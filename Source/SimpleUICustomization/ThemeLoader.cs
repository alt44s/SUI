using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
namespace SimpleUICustomization
{
    public static class ThemeLoader
    {
        static Font[] originalFonts;
        static AssetBundle mainBundle;
        static readonly int[] VanillaBaseSizes = { 11, 14, 18 };
        static readonly Dictionary<string, Font> loadedCustomFonts = new Dictionary<string, Font>();
        static readonly Dictionary<string, Texture2D> loadedCustomTextures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, CursorFrame[]> loadedCursorFrames = new Dictionary<string, CursorFrame[]>();
        static string[] cachedTexturePaths;
        static HashSet<string> ownBundleKeys;

        public const string ExpansionPrefix = "expansion:";

        public static (string key, string label, Texture2D tex)[] ExpansionBackgrounds()
        {
            var list = new List<(string, string, Texture2D)>();
            foreach (var exp in Verse.ModLister.AllExpansions)
            {
                var tex = exp.BackgroundImage;
                if (tex == null || tex.NullOrBad()) continue;
                list.Add((ExpansionPrefix + exp.defName, exp.label.NullOrEmpty() ? exp.defName : exp.label, tex));
            }
            return list.ToArray();
        }

        public static string[] AllTexturePaths()
        {
            if (cachedTexturePaths != null) return cachedTexturePaths;
            var set = new HashSet<string>();
            foreach (var mod in LoadedModManager.RunningModsListForReading)
                foreach (var key in mod.GetContentHolder<Texture2D>().contentList.Keys)
                    set.Add(key);
            foreach (var tex in Resources.LoadAll<Texture2D>(""))
            {
                if (tex == null || tex.name.NullOrEmpty()) continue;
                if (set.Add(tex.name)) loadedCustomTextures[tex.name] = tex;
            }
            foreach (var bundle in AllBundles())
            {
                foreach (var name in bundle.GetAllAssetNames())
                {
                    if (!name.EndsWith(".png") && !name.EndsWith(".jpg") && !name.EndsWith(".jpeg")) continue;
                    string key = Path.GetFileNameWithoutExtension(name);
                    if (set.Add(key)) loadedCustomTextures[key] = bundle.LoadAsset<Texture2D>(name);
                }
            }
            cachedTexturePaths = set.OrderBy(s => s).ToArray();
            return cachedTexturePaths;
        }

        public static bool IsOwnBundleAsset(string key)
        {
            if (key.NullOrEmpty()) return false;
            if (ownBundleKeys == null)
            {
                ownBundleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var bundle = GetMainBundle();
                if (bundle != null)
                    foreach (var name in bundle.GetAllAssetNames())
                        ownBundleKeys.Add(Path.GetFileNameWithoutExtension(name));
            }
            return ownBundleKeys.Contains(key);
        }

        public static Texture2D GetTexture(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (path.StartsWith(ExpansionPrefix))
            {
                var exp = Verse.ModLister.AllExpansions.FirstOrDefault(e => e.defName == path.Substring(ExpansionPrefix.Length));
                return exp != null && !exp.BackgroundImage.NullOrBad() ? exp.BackgroundImage : null;
            }
            if (path.StartsWith(Dialog_Picker.GamePrefix)) path = path.Substring(Dialog_Picker.GamePrefix.Length);
            else if (path.StartsWith(Dialog_Picker.FilePrefix)) return LoadFromDisk(path.Substring(Dialog_Picker.FilePrefix.Length));
            else if (Path.IsPathRooted(path)) return LoadFromDisk(path);
            if (loadedCustomTextures.TryGetValue(path, out var tex) && tex != null) return tex;
            var bundle = FindBundleWithAsset(path) ?? FindBundleWithAsset(Path.GetFileName(path));
            tex = bundle != null ? bundle.LoadAsset<Texture2D>(path) : null;
            if (tex == null && bundle != null) tex = bundle.LoadAsset<Texture2D>(Path.GetFileName(path));
            if (tex == null) tex = ContentFinder<Texture2D>.Get(path, false);
            if (tex == null)
            {
                Log.Warning("[SimpleUICustomization] Texture not found in bundle or content: " + path);
                return null;
            }
            tex.filterMode = FilterMode.Bilinear;
            loadedCustomTextures[path] = tex;
            return tex;
        }

        static Texture2D LoadFromDisk(string diskPath)
        {
            if (loadedCustomTextures.TryGetValue(diskPath, out var tex) && tex != null) return tex;
            try
            {
                if (!File.Exists(diskPath)) return null;
                tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(diskPath))) return null;
                tex.filterMode = FilterMode.Bilinear;
                loadedCustomTextures[diskPath] = tex;
                return tex;
            }
            catch (System.Exception e)
            {
                Log.Warning("[SimpleUICustomization] Failed to read texture from disk: " + diskPath + " - " + e.Message);
                return null;
            }
        }

        static void CacheOriginals()
        {
            if (originalFonts != null) return;
            originalFonts = new Font[3];
            for (int i = 0; i < 3; i++)
                originalFonts[i] = Text.fontStyles[i].font;
        }
        static AssetBundle GetMainBundle()
        {
            if (mainBundle != null && mainBundle) return mainBundle;
            mainBundle = SimpleUICustomizationMod.ContentPack.assetBundles.loadedAssetBundles
                .FirstOrDefault(b => b.name == "suic_bundle");
            if (mainBundle == null)
                Log.Warning("[SimpleUICustomization] suic_bundle not found among loaded bundles.");
            return mainBundle;
        }

        static IEnumerable<AssetBundle> AllBundles()
        {
            var main = GetMainBundle();
            if (main != null) yield return main;
            foreach (var mod in LoadedModManager.RunningModsListForReading)
            {
                if (mod == SimpleUICustomizationMod.ContentPack) continue;
                foreach (var b in mod.assetBundles.loadedAssetBundles)
                    if (b != main) yield return b;
            }
        }

        static AssetBundle FindBundleWithAsset(string name)
        {
            foreach (var b in AllBundles())
                if (b.Contains(name)) return b;
            return null;
        }

        public static string[] AvailableFontNames()
        {
            var names = new HashSet<string>();
            foreach (var bundle in AllBundles())
                foreach (var n in bundle.GetAllAssetNames())
                    if (n.EndsWith(".ttf") || n.EndsWith(".otf"))
                        names.Add(Path.GetFileNameWithoutExtension(n));
            return names.OrderBy(n => n).ToArray();
        }
        public static void ApplyAll(ThemeValues v)
        {
            AllTexturePaths();
            v.RecomputeColors();
            v.RecomputeTextures();
            ApplyFont(v.fontName, v.fontSizeOffset);
            ApplyCursor(v.cursorPath, v.cursorModRoot);
        }
        public static void ApplyFont(string fontName, int sizeOffset = 0)
        {
            CacheOriginals();
            Font font = null;
            if (!string.IsNullOrEmpty(fontName))
            {
                if (!loadedCustomFonts.TryGetValue(fontName, out font) || font == null)
                {
                    var bundle = FindBundleWithAsset(fontName);
                    font = bundle?.LoadAsset<Font>(fontName);
                    if (font == null)
                        Log.Warning($"[SimpleUICustomization] Font '{fontName}' not found in any loaded bundle.");
                    else
                        loadedCustomFonts[fontName] = font;
                }
            }
            bool needsExplicitSize = sizeOffset != 0 || font != null;
            for (int i = 0; i < 3; i++)
            {
                var f = font ?? originalFonts[i];
                Text.fontStyles[i].font = f;
                Text.fontStyles[i].fontSize = needsExplicitSize ? Mathf.Max(1, VanillaBaseSizes[i] + sizeOffset) : 0;
                Text.textFieldStyles[i].font = f;
                Text.textAreaStyles[i].font = f;
            }
        }
        static CursorFrame[] activeCursorFrames;
        static int activeCursorFrame;
        static float nextFrameTime;

        public const string CursorSubfolder = "Cursors";

        static bool HasPrefix(string path) =>
            path.StartsWith(Dialog_Picker.FilePrefix) || path.StartsWith(Dialog_Picker.GamePrefix) || Path.IsPathRooted(path);

        public static void ApplyCursor(string path, string modRoot = "")
        {
            activeCursorFrames = null;
            if (string.IsNullOrEmpty(path))
            {
                CustomCursor.Activate();
                return;
            }

            string resolved = path.StartsWith(Dialog_Picker.FilePrefix) ? path.Substring(Dialog_Picker.FilePrefix.Length) : path;
            if (!modRoot.NullOrEmpty() && !HasPrefix(path))
            {
                string candidate = Path.GetFullPath(Path.Combine(modRoot, CursorSubfolder, path));
                string modRootFull = Path.GetFullPath(modRoot);
                if (!candidate.StartsWith(modRootFull, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning("[SimpleUICustomization] Rejected cursor path outside mod folder: " + path);
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    return;
                }
                resolved = candidate;
            }

            if (resolved.EndsWith(".cur", StringComparison.OrdinalIgnoreCase) || resolved.EndsWith(".ani", StringComparison.OrdinalIgnoreCase) || resolved.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                if (!loadedCursorFrames.TryGetValue(resolved, out var frames))
                {
                    frames = CursorFormats.Load(resolved);
                    loadedCursorFrames[resolved] = frames;
                }
                if (frames == null || frames.Length == 0)
                {
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    return;
                }
                if (frames.Length == 1)
                {
                    Cursor.SetCursor(frames[0].tex, frames[0].hotspot, CursorMode.Auto);
                }
                else
                {
                    activeCursorFrames = frames;
                    activeCursorFrame = 0;
                    nextFrameTime = 0f;
                    Cursor.SetCursor(frames[0].tex, frames[0].hotspot, CursorMode.Auto);
                }
                return;
            }
            var tex = GetTexture(path);
            if (tex == null)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Log.Warning("[SimpleUICustomization] Failed to load cursor image: " + path);
                return;
            }
            try
            {
                if (!tex.isReadable) tex = MakeReadable(tex);
                Cursor.SetCursor(tex, new Vector2(tex.width / 2f, tex.height / 2f), CursorMode.Auto);
            }
            catch (System.Exception e)
            {
                Log.Warning("[SimpleUICustomization] Failed to apply cursor: " + path + " - " + e.Message);
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        public static void TickCursorAnimation()
        {
            if (activeCursorFrames == null) return;
            if (Time.unscaledTime < nextFrameTime) return;
            activeCursorFrame = (activeCursorFrame + 1) % activeCursorFrames.Length;
            var f = activeCursorFrames[activeCursorFrame];
            Cursor.SetCursor(f.tex, f.hotspot, CursorMode.Auto);
            nextFrameTime = Time.unscaledTime + Mathf.Max(0.016f, f.delaySeconds);
        }

        public static Texture2D MakeReadable(Texture2D src)
        {
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;
            try
            {
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;
                var readable = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false);
                readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
                readable.Apply();
                return readable;
            }
            finally
            {
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}