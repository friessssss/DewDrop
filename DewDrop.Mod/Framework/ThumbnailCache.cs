using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using DewDrop.Core.Services;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using StardewValley;

namespace DewDrop.Framework
{
    /// <summary>
    /// Downloads Nexus images in the background, caches them on disk, decodes them off the main thread and turns
    /// a few of them into textures each tick. Nexus's CDN serves every picture as WebP whatever the URL says, which
    /// the game's Texture2D.FromStream can't read, so decoding goes through the SkiaSharp the game ships with.
    /// </summary>
    internal class ThumbnailCache
    {
        /// <summary>Wider pictures are scaled down when decoded; the menu never draws one wider than this.</summary>
        private const int MaxWidth = 1024;

        /// <summary>Roughly how much decoded picture data stays in video memory before the least recently drawn goes.</summary>
        private const long MaxBytes = 64L * 1024 * 1024;

        /// <summary>Texture uploads per tick, so a page of downloads finishing together can't stall one frame.</summary>
        private const int UploadsPerTick = 4;

        /// <summary>How long a picture whose download failed waits before it's tried again.</summary>
        private const int RetryMs = 30_000;

        private readonly NexusApi _api;
        private readonly string _folder;
        private readonly Dictionary<string, Entry> _textures = new();
        private readonly HashSet<string> _pending = new();
        private readonly ConcurrentQueue<Loaded> _loaded = new();
        private readonly SemaphoreSlim _decodeGate = new(2, 2);

        /// <summary>URLs that couldn't be shown, and when (Environment.TickCount64) to try them again.</summary>
        private readonly Dictionary<string, long> _failed = new();
        private long _bytes;
        private int _generation;

        public ThumbnailCache(NexusApi api, string folder)
        {
            _api = api;
            _folder = folder;
            Directory.CreateDirectory(folder);
        }

        /// <summary>Whether the image at this URL couldn't be downloaded or decoded, so callers can stop showing a placeholder.</summary>
        public bool IsFailed(string? url) => url is not null && _failed.ContainsKey(url);

        /// <summary>The texture if it's ready; otherwise starts loading it and returns null.</summary>
        public Texture2D? Get(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            if (_textures.TryGetValue(url, out var entry))
            {
                entry.LastUsed = Environment.TickCount64;
                return entry.Texture;
            }

            if (_failed.TryGetValue(url, out var retryAt))
            {
                if (Environment.TickCount64 < retryAt)
                {
                    return null;
                }
                _failed.Remove(url);
            }

            if (_pending.Add(url))
            {
                var generation = _generation;
                Task.Run(() => Load(url, generation));
            }

            return null;
        }

        /// <summary>Turns a few finished loads into textures. Runs on the main thread once per tick.</summary>
        public void Upload()
        {
            var uploads = 0;
            while (uploads < UploadsPerTick && _loaded.TryDequeue(out var loaded))
            {
                // Loads started before Clear() belong to textures that are gone
                if (loaded.Generation != _generation)
                {
                    continue;
                }

                _pending.Remove(loaded.Url);
                var texture = CreateTexture(loaded);
                if (loaded.Image is not null || loaded.Encoded is not null)
                {
                    uploads++;
                }

                if (texture is null)
                {
                    _failed[loaded.Url] = loaded.Retry ? Environment.TickCount64 + RetryMs : long.MaxValue;
                    continue;
                }

                var entry = new Entry(texture, (long)texture.Width * texture.Height * 4) { LastUsed = Environment.TickCount64 };
                _textures[loaded.Url] = entry;
                _bytes += entry.Bytes;
                if (_bytes > MaxBytes)
                {
                    Evict();
                }
            }
        }

        /// <summary>Frees every texture, e.g. once the menu closes; pictures load again from the disk cache when next drawn.</summary>
        public void Clear()
        {
            foreach (var entry in _textures.Values)
            {
                entry.Texture.Dispose();
            }

            _textures.Clear();
            _pending.Clear();
            _failed.Clear();
            _bytes = 0;
            _generation++;
        }

        private Texture2D? CreateTexture(Loaded loaded)
        {
            Texture2D? texture = null;
            try
            {
                if (loaded.Image is not null)
                {
                    texture = new Texture2D(Game1.graphics.GraphicsDevice, loaded.Image.Width, loaded.Image.Height);
                    texture.SetData(loaded.Image.Pixels);
                    return texture;
                }

                if (loaded.Encoded is not null)
                {
                    // Only for something SkiaSharp couldn't read; the game's decoder runs here on the main thread
                    using var stream = new MemoryStream(loaded.Encoded);
                    texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                    if (!loaded.FromDisk)
                    {
                        var bytes = loaded.Encoded;
                        Task.Run(() => SaveAsync(loaded.Path, bytes));
                    }
                    return texture;
                }
            }
            catch (Exception ex)
            {
                texture?.Dispose();
                ModEntry.Log.Trace($"Couldn't decode {loaded.Url}: {ex.Message}");
                if (loaded.Encoded is not null && loaded.FromDisk)
                {
                    // A bad cached copy shouldn't stick: drop it and download again later
                    TryDelete(loaded.Path);
                    loaded.Retry = true;
                }
            }

            return null;
        }

        /// <summary>Frees the least recently drawn textures, keeping anything drawn in the last couple of seconds.</summary>
        private void Evict()
        {
            var cutoff = Environment.TickCount64 - 2000;
            foreach (var (url, entry) in _textures.Where(p => p.Value.LastUsed < cutoff).OrderBy(p => p.Value.LastUsed).ToList())
            {
                if (_bytes <= MaxBytes * 3 / 4)
                {
                    break;
                }

                _textures.Remove(url);
                entry.Texture.Dispose();
                _bytes -= entry.Bytes;
            }
        }

        private async Task Load(string url, int generation)
        {
            var loaded = new Loaded(url, generation, Path.Combine(_folder, Hash(url) + SafeExtension(url)));
            try
            {
                loaded.FromDisk = File.Exists(loaded.Path);
                var bytes = loaded.FromDisk ? await File.ReadAllBytesAsync(loaded.Path) : await _api.GetBytesAsync(url);
                if (bytes is null)
                {
                    // Most likely a network problem, so worth another go later
                    loaded.Retry = true;
                }
                else
                {
                    loaded.Image = await DecodeAsync(url, bytes);
                    if (loaded.Image is not null)
                    {
                        // Only bytes that decode are worth keeping
                        if (!loaded.FromDisk)
                        {
                            await SaveAsync(loaded.Path, bytes);
                        }
                    }
                    else if (!IsWebP(bytes))
                    {
                        loaded.Encoded = bytes;
                    }
                    else if (loaded.FromDisk)
                    {
                        // A cached copy cut short (say by a crash): drop it so the retry downloads it again
                        TryDelete(loaded.Path);
                        loaded.Retry = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.Log.Trace($"Thumbnail {url} failed: {ex.Message}");
                loaded.Retry = true;
            }

            _loaded.Enqueue(loaded);
        }

        private async Task<Decoded?> DecodeAsync(string url, byte[] bytes)
        {
            await _decodeGate.WaitAsync();
            try
            {
                return DecodeWithSkia(bytes);
            }
            catch (Exception ex)
            {
                ModEntry.Log.Trace($"Couldn't decode {url}: {ex.Message}");
                return null;
            }
            finally
            {
                _decodeGate.Release();
            }
        }

        /// <summary>
        /// Decodes any format SkiaSharp knows (WebP including animated, PNG, JPEG, GIF...) to premultiplied RGBA,
        /// at most MaxWidth wide; null if it can't. Kept apart so a missing SkiaSharp fails here, inside a catch.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Decoded? DecodeWithSkia(byte[] bytes)
        {
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec is null)
            {
                return null;
            }

            // WebP and JPEG can decode straight to a smaller size; animations give their first frame
            var size = codec.Info.Width > MaxWidth ? codec.GetScaledDimensions((float)MaxWidth / codec.Info.Width) : codec.Info.Size;
            var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var bitmap = new SKBitmap(info);
            if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
            {
                return null;
            }

            if (bitmap.Width <= MaxWidth)
            {
                return new Decoded(bitmap.Width, bitmap.Height, bitmap.Bytes);
            }

            // PNG and GIF only decode at full size
            var height = Math.Max(1, (int)((long)bitmap.Height * MaxWidth / bitmap.Width));
            using var scaled = bitmap.Resize(new SKImageInfo(MaxWidth, height, SKColorType.Rgba8888, SKAlphaType.Premul), SKFilterQuality.Medium);
            return scaled is null ? null : new Decoded(scaled.Width, scaled.Height, scaled.Bytes);
        }

        private static bool IsWebP(byte[] bytes)
        {
            return bytes.Length >= 12
                && bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F'
                && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P';
        }

        /// <summary>Writes beside the final name and moves it into place, so a crash never leaves half a file.</summary>
        private static async Task SaveAsync(string path, byte[] bytes)
        {
            var temp = path + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temp, bytes);
                File.Move(temp, path, overwrite: true);
            }
            catch (Exception ex)
            {
                ModEntry.Log.Trace($"Couldn't cache {path}: {ex.Message}");
                TryDelete(temp);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // A stale cache file is harmless
            }
        }

        private static string SafeExtension(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? Path.GetExtension(uri.LocalPath) : "";
        }

        private static string Hash(string url)
        {
            return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(url)))[..16];
        }

        private record Decoded(int Width, int Height, byte[] Pixels);

        /// <summary>A finished background load, handed to the main thread.</summary>
        private class Loaded
        {
            public string Url { get; }
            public int Generation { get; }
            public string Path { get; }
            public bool FromDisk { get; set; }

            /// <summary>Decoded pixels, ready to upload.</summary>
            public Decoded? Image { get; set; }

            /// <summary>Bytes SkiaSharp couldn't read, left for the game's own decoder.</summary>
            public byte[]? Encoded { get; set; }

            /// <summary>Whether a failure might go away (network trouble, a bad cached copy), so it's tried again later.</summary>
            public bool Retry { get; set; }

            public Loaded(string url, int generation, string path)
            {
                Url = url;
                Generation = generation;
                Path = path;
            }
        }

        private class Entry
        {
            public Texture2D Texture { get; }
            public long Bytes { get; }
            public long LastUsed { get; set; }

            public Entry(Texture2D texture, long bytes)
            {
                Texture = texture;
                Bytes = bytes;
            }
        }
    }
}
