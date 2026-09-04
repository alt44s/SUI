using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    public struct CursorFrame
    {
        public Texture2D tex;
        public Vector2 hotspot;
        public float delaySeconds;
    }

    public static class CursorFormats
    {
        public static CursorFrame[] Load(string path)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (path.EndsWith(".ani", StringComparison.OrdinalIgnoreCase)) return ParseAni(bytes);
                bool isIco = path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase);
                return new[] { ParseCur(bytes, 0, isIco) };
            }
            catch (Exception e)
            {
                Log.Warning("[SimpleUICustomization] Failed to load cursor file: " + path + " - " + e.Message);
                return null;
            }
        }

        static CursorFrame ParseCur(byte[] d, int baseOffset, bool isIco = false)
        {
            int count = BitConverter.ToUInt16(d, baseOffset + 4);
            if (count < 1) throw new Exception("no images in .cur/.ico");
            int entryOff = baseOffset + 6;
            int hotX = d[entryOff + 4] | (d[entryOff + 5] << 8);
            int hotY = d[entryOff + 6] | (d[entryOff + 7] << 8);
            int imgSize = BitConverter.ToInt32(d, entryOff + 8);
            int imgOff = BitConverter.ToInt32(d, entryOff + 12) + baseOffset;

            var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (d[imgOff] == 0x89 && d[imgOff + 1] == 0x50)
            {
                var png = new byte[imgSize];
                Array.Copy(d, imgOff, png, 0, imgSize);
                if (!ImageConversion.LoadImage(tex, png)) throw new Exception("bad png frame");
            }
            else
            {
                tex = DecodeDib(d, imgOff);
            }
            var hotspot = isIco ? new Vector2(tex.width / 2f, tex.height / 2f) : new Vector2(hotX, hotY);
            return new CursorFrame { tex = tex, hotspot = hotspot };
        }

        static Texture2D DecodeDib(byte[] d, int off)
        {
            int headerSize = BitConverter.ToInt32(d, off);
            int w = BitConverter.ToInt32(d, off + 4);
            int h = BitConverter.ToInt32(d, off + 8) / 2;
            int bpp = BitConverter.ToUInt16(d, off + 14);
            if (bpp != 32 && bpp != 24 && bpp != 8 && bpp != 4 && bpp != 1) throw new Exception("unsupported cursor bit depth: " + bpp);

            var pixels = new Color32[w * h];
            int pixOff = off + headerSize;

            if (bpp <= 8)
            {
                int paletteCount = 1 << bpp;
                var palette = new Color32[paletteCount];
                int palOff = off + headerSize;
                for (int i = 0; i < paletteCount; i++)
                {
                    int p = palOff + i * 4;
                    palette[i] = new Color32(d[p + 2], d[p + 1], d[p], 255);
                }
                int rowBytes = ((w * bpp + 31) / 32) * 4;
                int dataOff = palOff + paletteCount * 4;
                int pxPerByte = 8 / bpp;
                for (int y = 0; y < h; y++)
                {
                    int srcRow = dataOff + y * rowBytes;
                    int dstRow = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int b = d[srcRow + x / pxPerByte];
                        int shift = (pxPerByte - 1 - x % pxPerByte) * bpp;
                        int idx = (b >> shift) & ((1 << bpp) - 1);
                        pixels[dstRow + x] = palette[idx];
                    }
                }
                ApplyAndMask(d, pixOff + rowBytes * h, w, h, pixels);
                var texP = new Texture2D(w, h, TextureFormat.RGBA32, false);
                texP.SetPixels32(pixels);
                texP.Apply();
                return texP;
            }

            int bytesPerPixel = bpp / 8;
            int rowSize = ((w * bytesPerPixel + 3) / 4) * 4;
            for (int y = 0; y < h; y++)
            {
                int srcRow = pixOff + y * rowSize;
                int dstRow = y * w;
                for (int x = 0; x < w; x++)
                {
                    int si = srcRow + x * bytesPerPixel;
                    byte a = bpp == 32 ? d[si + 3] : (byte)255;
                    pixels[dstRow + x] = new Color32(d[si + 2], d[si + 1], d[si], a);
                }
            }
            if (bpp != 32) ApplyAndMask(d, pixOff + rowSize * h, w, h, pixels);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        static void ApplyAndMask(byte[] d, int maskOff, int w, int h, Color32[] pixels)
        {
            int maskRowBytes = ((w + 31) / 32) * 4;
            for (int y = 0; y < h; y++)
            {
                int rowOff = maskOff + y * maskRowBytes;
                int dstRow = y * w;
                for (int x = 0; x < w; x++)
                {
                    bool transparent = (d[rowOff + x / 8] & (0x80 >> (x % 8))) != 0;
                    if (transparent) pixels[dstRow + x].a = 0;
                }
            }
        }

        static CursorFrame[] ParseAni(byte[] d)
        {
            if (d[0] != 'R' || d[1] != 'I' || d[2] != 'F' || d[3] != 'F') throw new Exception("not a RIFF file");
            int pos = 12;
            int defaultRateJiffies = 6;
            var rates = new List<int>();
            var seq = new List<int>();
            var frameChunks = new List<int>();

            while (pos + 8 <= d.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(d, pos, 4);
                int size = BitConverter.ToInt32(d, pos + 4);
                int dataOff = pos + 8;

                if (id == "LIST")
                {
                    string listType = System.Text.Encoding.ASCII.GetString(d, dataOff, 4);
                    if (listType == "fram")
                    {
                        int p = dataOff + 4;
                        int end = dataOff + size - 4;
                        while (p + 8 <= end)
                        {
                            string subId = System.Text.Encoding.ASCII.GetString(d, p, 4);
                            int subSize = BitConverter.ToInt32(d, p + 4);
                            if (subId == "icon") frameChunks.Add(p + 8);
                            p += 8 + subSize + (subSize & 1);
                        }
                    }
                }
                else if (id == "anih")
                {
                    defaultRateJiffies = BitConverter.ToInt32(d, dataOff + 28);
                }
                else if (id == "rate")
                {
                    int n = size / 4;
                    for (int i = 0; i < n; i++) rates.Add(BitConverter.ToInt32(d, dataOff + i * 4));
                }
                else if (id == "seq ")
                {
                    int n = size / 4;
                    for (int i = 0; i < n; i++) seq.Add(BitConverter.ToInt32(d, dataOff + i * 4));
                }

                pos = dataOff + size + (size & 1);
            }

            if (frameChunks.Count == 0) throw new Exception("no icon frames in .ani");
            var decoded = new CursorFrame[frameChunks.Count];
            for (int i = 0; i < frameChunks.Count; i++)
                decoded[i] = ParseCur(d, frameChunks[i]);

            int steps = seq.Count > 0 ? seq.Count : frameChunks.Count;
            var result = new CursorFrame[steps];
            for (int i = 0; i < steps; i++)
            {
                int frameIdx = seq.Count > 0 ? seq[i] : i;
                var f = decoded[frameIdx];
                int jiffies = i < rates.Count ? rates[i] : defaultRateJiffies;
                f.delaySeconds = jiffies / 60f;
                result[i] = f;
            }
            return result;
        }
    }
}