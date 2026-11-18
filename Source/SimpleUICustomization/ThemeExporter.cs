using System;
using System.IO;
using System.Text;
using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    public static class ThemeExporter
    {
        public static string ExportRoot => Path.Combine(SimpleUICustomizationMod.ModRootDir, "ExportedThemes");

        public class ExportResult
        {
            public bool success;
            public string folder;
            public string error;
            public bool overwrote;
        }

        public static ExportResult Export(string themeName, ThemeValues v, string author, string description)
        {
            var result = new ExportResult();
            string safeName = SanitizeFileName(themeName);
            string defName = SanitizeDefName(themeName);
            if (safeName.NullOrEmpty() || defName.NullOrEmpty())
            {
                result.error = "SUIC_PleaseEnterThemeName".Translate();
                return result;
            }

            v.RecomputeTextures();

            try
            {
                string folder = Path.Combine(ExportRoot, safeName);
                result.overwrote = Directory.Exists(folder);
                if (result.overwrote)
                {
                    SafeDeleteDir(Path.Combine(folder, "Textures"));
                    SafeDeleteDir(Path.Combine(folder, ThemeLoader.CursorSubfolder));
                }
                Directory.CreateDirectory(folder);
                Directory.CreateDirectory(Path.Combine(folder, "About"));

                string texFolder = Path.Combine(folder, "Textures");
                string windowTex = BundleAsset(v.windowTex, v.ResolvedWindowTex, texFolder);
                string menuSectionTex = BundleAsset(v.menuSectionTex, v.ResolvedMenuSectionTex, texFolder);
                string tabTex = BundleAsset(v.tabTex, v.ResolvedTabTex, texFolder);
                string buttonTex = BundleAsset(v.buttonTex, v.ResolvedButtonTex, texFolder);
                string bottomBarTex = BundleAsset(v.bottomBarTex, v.ResolvedBottomBarTex, texFolder);
                string gizmoTex = BundleAsset(v.gizmoTex, v.ResolvedGizmoTex, texFolder);
                string titleTex = BundleAsset(v.titleTex, v.ResolvedTitleTex, texFolder);
                string scrollbarTex = BundleAsset(v.scrollbarTex, v.ResolvedScrollbarTex, texFolder);
                string checkboxOnTex = BundleAsset(v.checkboxOnTex, v.ResolvedCheckboxOnTex, texFolder);
                string checkboxOffTex = BundleAsset(v.checkboxOffTex, v.ResolvedCheckboxOffTex, texFolder);
                string checkboxPartialTex = BundleAsset(v.checkboxPartialTex, v.ResolvedCheckboxPartialTex, texFolder);
                string textFieldTex = BundleAsset(v.textFieldTex, v.ResolvedTextFieldTex, texFolder);
                string fillableBarFillTex = BundleAsset(v.fillableBarFillTex, v.ResolvedFillableBarFillTex, texFolder);
                string fillableBarBGTex = BundleAsset(v.fillableBarBGTex, v.ResolvedFillableBarBGTex, texFolder);

                var bgTexNames = new System.Collections.Generic.List<string>();
                for (int i = 0; i < v.mainMenuBackgrounds.Count; i++)
                {
                    var resolved = i < v.ResolvedBackgrounds.Count ? v.ResolvedBackgrounds[i] : null;
                    string bundled = BundleAsset(v.mainMenuBackgrounds[i], resolved, texFolder);
                    if (!bundled.NullOrEmpty()) bgTexNames.Add(bundled);
                }

                string cursorPath = BundleCursor(v.cursorPath, v.cursorModRoot, Path.Combine(folder, ThemeLoader.CursorSubfolder));

                string xml = BuildXml(safeName, defName, v, windowTex, menuSectionTex, tabTex, buttonTex, bottomBarTex, gizmoTex, titleTex, cursorPath, bgTexNames, scrollbarTex, checkboxOnTex, checkboxOffTex, checkboxPartialTex, textFieldTex, fillableBarFillTex, fillableBarBGTex);
                Directory.CreateDirectory(Path.Combine(folder, "Defs"));
                File.WriteAllText(Path.Combine(folder, "Defs", "Themes.xml"), xml);
                File.WriteAllText(Path.Combine(folder, "About", "About.xml"), BuildAboutXml(safeName, author, description));
                CapturePreview(safeName, Path.Combine(folder, "About", "Preview.png"), v);

                result.success = true;
                result.folder = folder;
                return result;
            }
            catch (Exception e)
            {
                Log.Error("[SimpleUICustomization] Theme export failed: " + e);
                result.error = "SUIC_ExportFailed".Translate(e.Message);
                return result;
            }
        }

        static void SafeDeleteDir(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch (Exception e) { Log.Warning("[SimpleUICustomization] Failed to clear old export folder '" + path + "': " + e.Message); }
        }

        static void CapturePreview(string themeName, string path, ThemeValues v)
        {
            Find.WindowStack.Add(new Dialog_ThemePreviewCapture(themeName, v, tex =>
            {
                try
                {
                    File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
                }
                catch (Exception e)
                {
                    Log.Warning("[SimpleUICustomization] Failed to write preview image: " + e.Message);
                }
                finally
                {
                    UnityEngine.Object.Destroy(tex);
                }
            }));
        }

        static bool IsDiskPath(string path) =>
            !path.NullOrEmpty() && (path.StartsWith(Dialog_Picker.FilePrefix) || Path.IsPathRooted(path));

        static string StripPrefix(string path) =>
            path.StartsWith(Dialog_Picker.FilePrefix) ? path.Substring(Dialog_Picker.FilePrefix.Length) : path;

        static string CopyDeduped(string srcPath, string destFolder, string baseName, string ext)
        {
            Directory.CreateDirectory(destFolder);
            string dest = Path.Combine(destFolder, baseName + ext);
            int i = 1;
            while (File.Exists(dest) && !FilesIdentical(srcPath, dest))
                dest = Path.Combine(destFolder, baseName + "_" + i++ + ext);
            File.Copy(srcPath, dest, true);
            return dest;
        }

        static string BundleAsset(string path, Texture2D resolved, string texFolder)
        {
            if (path.NullOrEmpty()) return "";
            if (path.StartsWith(ThemeLoader.ExpansionPrefix)) return path;
            if (IsDiskPath(path))
            {
                string diskPath = StripPrefix(path);
                if (!File.Exists(diskPath))
                {
                    Log.Warning("[SimpleUICustomization] Texture file missing during export, skipped: " + diskPath);
                    return "";
                }
                string dest = CopyDeduped(diskPath, texFolder, Path.GetFileNameWithoutExtension(diskPath), Path.GetExtension(diskPath));
                return Path.GetFileNameWithoutExtension(dest);
            }

            string keyName = path.StartsWith(Dialog_Picker.GamePrefix) ? path.Substring(Dialog_Picker.GamePrefix.Length) : path;
            keyName = Path.GetFileNameWithoutExtension(keyName);
            if (ThemeLoader.IsOwnBundleAsset(keyName)) return keyName;
            keyName = SanitizeFileName(keyName);
            if (keyName.NullOrEmpty())
            {
                Log.Warning("[SimpleUICustomization] Texture '" + path + "' has no usable file name, skipped.");
                return "";
            }

            if (resolved == null || !resolved)
            {
                Log.Warning("[SimpleUICustomization] Texture '" + path + "' could not be resolved during export, skipped.");
                return "";
            }
            try
            {
                Directory.CreateDirectory(texFolder);
                string dest = Path.Combine(texFolder, keyName + ".png");
                File.WriteAllBytes(dest, EncodeReadable(resolved));
                return keyName;
            }
            catch (Exception e)
            {
                Log.Warning("[SimpleUICustomization] Failed to bundle texture '" + path + "': " + e.Message);
                return "";
            }
        }

        static byte[] EncodeReadable(Texture2D tex)
        {
            if (tex.isReadable) return ImageConversion.EncodeToPNG(tex);
            var readable = ThemeLoader.MakeReadable(tex);
            var bytes = ImageConversion.EncodeToPNG(readable);
            UnityEngine.Object.Destroy(readable);
            return bytes;
        }

        static string BundleCursor(string path, string modRoot, string cursorFolder)
        {
            if (path.NullOrEmpty()) return "";
            string diskPath;
            if (IsDiskPath(path)) diskPath = StripPrefix(path);
            else if (!modRoot.NullOrEmpty()) diskPath = Path.Combine(modRoot, ThemeLoader.CursorSubfolder, path);
            else return path;
            if (!File.Exists(diskPath))
            {
                Log.Warning("[SimpleUICustomization] Cursor file missing during export, skipped: " + diskPath);
                return "";
            }
            string dest = CopyDeduped(diskPath, cursorFolder, Path.GetFileNameWithoutExtension(diskPath), Path.GetExtension(diskPath));
            return Path.GetFileName(dest);
        }

        static bool FilesIdentical(string a, string b)
        {
            try
            {
                var fa = new FileInfo(a);
                var fb = new FileInfo(b);
                if (fa.Length != fb.Length) return false;
                using (var sa = fa.OpenRead())
                using (var sb = fb.OpenRead())
                {
                    var bufA = new byte[8192];
                    var bufB = new byte[8192];
                    int readA;
                    while ((readA = sa.Read(bufA, 0, bufA.Length)) > 0)
                    {
                        int readB = sb.Read(bufB, 0, readA);
                        if (readB != readA) return false;
                        for (int i = 0; i < readA; i++)
                            if (bufA[i] != bufB[i]) return false;
                    }
                    return true;
                }
            }
            catch { return false; }
        }

        public static string SanitizeFileName(string name)
        {
            if (name.NullOrEmpty()) return "";
            var sb = new StringBuilder();
            foreach (char c in name)
                sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
            return sb.ToString().Trim();
        }

        static string BuildAboutXml(string safeName, string author, string description)
        {
            string depId = SimpleUICustomizationMod.ContentPack.PackageId;
            string packageId = "suitheme." + ToPackageIdSafe(safeName);
            const string workshopUrl = "https://steamcommunity.com/id/alt4ss/myworkshopfiles?appid=294100";
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<ModMetaData>");
            sb.AppendLine($"  <packageId>{Escape(packageId)}</packageId>");
            sb.AppendLine($"  <name>{Escape(safeName)} (SUI Theme)</name>");
            sb.AppendLine($"  <author>{Escape(author)}</author>");
            sb.AppendLine($"  <description>{Escape(description)}</description>");
            sb.AppendLine("  <supportedVersions><li>1.6</li></supportedVersions>");
            sb.AppendLine("  <modDependencies>");
            sb.AppendLine("    <li>");
            sb.AppendLine($"      <packageId>{Escape(depId)}</packageId>");
            sb.AppendLine("      <displayName>SimplyUI - UI Customization</displayName>");
            sb.AppendLine($"      <downloadUrl>{Escape(workshopUrl)}</downloadUrl>");
            sb.AppendLine($"      <steamWorkshopUrl>{Escape(workshopUrl)}</steamWorkshopUrl>");
            sb.AppendLine("    </li>");
            sb.AppendLine("  </modDependencies>");
            sb.AppendLine("  <loadAfter>");
            sb.AppendLine($"    <li>{Escape(depId)}</li>");
            sb.AppendLine("  </loadAfter>");
            sb.AppendLine("</ModMetaData>");
            return sb.ToString();
        }

        static string BuildXml(string safeName, string defName, ThemeValues v, string windowTex, string menuSectionTex, string tabTex, string buttonTex, string bottomBarTex, string gizmoTex, string titleTex, string cursorPath, System.Collections.Generic.List<string> bgTexNames, string scrollbarTex, string checkboxOnTex, string checkboxOffTex, string checkboxPartialTex, string textFieldTex, string fillableBarFillTex, string fillableBarBGTex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<Defs>");
            sb.AppendLine();
            sb.AppendLine("  <SimpleUICustomization.ThemeDef>");
            sb.AppendLine($"    <defName>SUI_{defName}</defName>");
            sb.AppendLine($"    <label>{Escape(safeName)}</label>");
            sb.AppendLine($"    <accentColor>{ColorXml(v.accent)}</accentColor>");
            sb.AppendLine($"    <backgroundColor>{ColorXml(v.background)}</backgroundColor>");
            if (!v.fontName.NullOrEmpty()) sb.AppendLine($"    <fontName>{Escape(v.fontName)}</fontName>");
            if (v.fontSizeOffset != 0) sb.AppendLine($"    <fontSizeOffset>{v.fontSizeOffset}</fontSizeOffset>");
            if (!cursorPath.NullOrEmpty()) sb.AppendLine($"    <cursorPath>{Escape(cursorPath)}</cursorPath>");

            if (v.disabledElements.Count > 0)
            {
                sb.AppendLine("    <disabledElements>");
                foreach (var key in v.disabledElements) sb.AppendLine($"      <li>{Escape(key)}</li>");
                sb.AppendLine("    </disabledElements>");
            }
            if (v.hoverColor.HasValue) sb.AppendLine($"    <hoverColor>{ColorXml(v.hoverColor.Value)}</hoverColor>");
            if (v.clickColor.HasValue) sb.AppendLine($"    <clickColor>{ColorXml(v.clickColor.Value)}</clickColor>");
            if (v.textColor.HasValue) sb.AppendLine($"    <textColor>{ColorXml(v.textColor.Value)}</textColor>");
            if (v.windowBG.HasValue) sb.AppendLine($"    <windowBGColor>{ColorXml(v.windowBG.Value)}</windowBGColor>");
            if (v.menuSectionBG.HasValue) sb.AppendLine($"    <menuSectionBGColor>{ColorXml(v.menuSectionBG.Value)}</menuSectionBGColor>");
            if (v.tabBG.HasValue) sb.AppendLine($"    <tabBGColor>{ColorXml(v.tabBG.Value)}</tabBGColor>");
            if (v.buttonBG.HasValue) sb.AppendLine($"    <buttonBGColor>{ColorXml(v.buttonBG.Value)}</buttonBGColor>");
            if (v.bottomBarBG.HasValue) sb.AppendLine($"    <bottomBarBGColor>{ColorXml(v.bottomBarBG.Value)}</bottomBarBGColor>");
            if (v.gizmoBG.HasValue) sb.AppendLine($"    <gizmoBGColor>{ColorXml(v.gizmoBG.Value)}</gizmoBGColor>");
            if (v.scrollbarBG.HasValue) sb.AppendLine($"    <scrollbarBGColor>{ColorXml(v.scrollbarBG.Value)}</scrollbarBGColor>");
            if (v.checkboxBG.HasValue) sb.AppendLine($"    <checkboxBGColor>{ColorXml(v.checkboxBG.Value)}</checkboxBGColor>");
            if (v.sliderBG.HasValue) sb.AppendLine($"    <sliderBGColor>{ColorXml(v.sliderBG.Value)}</sliderBGColor>");
            if (v.textFieldBG.HasValue) sb.AppendLine($"    <textFieldBGColor>{ColorXml(v.textFieldBG.Value)}</textFieldBGColor>");
            if (v.fillableBarBG.HasValue) sb.AppendLine($"    <fillableBarBGColor>{ColorXml(v.fillableBarBG.Value)}</fillableBarBGColor>");
            if (v.fillableBarBackdropBG.HasValue) sb.AppendLine($"    <fillableBarBackdropBGColor>{ColorXml(v.fillableBarBackdropBG.Value)}</fillableBarBackdropBGColor>");
            if (v.colonistBarHealthBG.HasValue) sb.AppendLine($"    <colonistBarHealthBGColor>{ColorXml(v.colonistBarHealthBG.Value)}</colonistBarHealthBGColor>");
            if (v.colonistBarHealthBackdropBG.HasValue) sb.AppendLine($"    <colonistBarHealthBackdropBGColor>{ColorXml(v.colonistBarHealthBackdropBG.Value)}</colonistBarHealthBackdropBGColor>");
            if (!windowTex.NullOrEmpty()) sb.AppendLine($"    <windowTex>{Escape(windowTex)}</windowTex>");
            if (!v.windowBGSliced) sb.AppendLine("    <windowBGSliced>false</windowBGSliced>");
            if (!v.windowBGOutline) sb.AppendLine("    <windowBGOutline>false</windowBGOutline>");
            if (!menuSectionTex.NullOrEmpty()) sb.AppendLine($"    <menuSectionTex>{Escape(menuSectionTex)}</menuSectionTex>");
            if (!tabTex.NullOrEmpty()) sb.AppendLine($"    <tabTex>{Escape(tabTex)}</tabTex>");
            if (!buttonTex.NullOrEmpty()) sb.AppendLine($"    <buttonTex>{Escape(buttonTex)}</buttonTex>");
            if (!bottomBarTex.NullOrEmpty()) sb.AppendLine($"    <bottomBarTex>{Escape(bottomBarTex)}</bottomBarTex>");
            if (!gizmoTex.NullOrEmpty()) sb.AppendLine($"    <gizmoTex>{Escape(gizmoTex)}</gizmoTex>");
            if (!titleTex.NullOrEmpty()) sb.AppendLine($"    <titleTex>{Escape(titleTex)}</titleTex>");
            if (!scrollbarTex.NullOrEmpty()) sb.AppendLine($"    <scrollbarTex>{Escape(scrollbarTex)}</scrollbarTex>");
            if (!checkboxOnTex.NullOrEmpty()) sb.AppendLine($"    <checkboxOnTex>{Escape(checkboxOnTex)}</checkboxOnTex>");
            if (!checkboxOffTex.NullOrEmpty()) sb.AppendLine($"    <checkboxOffTex>{Escape(checkboxOffTex)}</checkboxOffTex>");
            if (!checkboxPartialTex.NullOrEmpty()) sb.AppendLine($"    <checkboxPartialTex>{Escape(checkboxPartialTex)}</checkboxPartialTex>");
            if (!textFieldTex.NullOrEmpty()) sb.AppendLine($"    <textFieldTex>{Escape(textFieldTex)}</textFieldTex>");
            if (!fillableBarFillTex.NullOrEmpty()) sb.AppendLine($"    <fillableBarFillTex>{Escape(fillableBarFillTex)}</fillableBarFillTex>");
            if (!fillableBarBGTex.NullOrEmpty()) sb.AppendLine($"    <fillableBarBGTex>{Escape(fillableBarBGTex)}</fillableBarBGTex>");
            if (bgTexNames.Count > 0)
            {
                sb.AppendLine("    <mainMenuBackgrounds>");
                foreach (var name in bgTexNames) sb.AppendLine($"      <li>{Escape(name)}</li>");
                sb.AppendLine("    </mainMenuBackgrounds>");
                if (v.bgCycle) sb.AppendLine("    <bgCycle>true</bgCycle>");
                if (v.bgCycleSeconds != 30f) sb.AppendLine($"    <bgCycleSeconds>{F(v.bgCycleSeconds)}</bgCycleSeconds>");
                if (v.bgFadeSeconds != 1.5f) sb.AppendLine($"    <bgFadeSeconds>{F(v.bgFadeSeconds)}</bgFadeSeconds>");
            }

            if (v.soundsEnabled && v.soundOverrides.Count > 0)
            {
                sb.AppendLine("    <soundsEnabled>true</soundsEnabled>");
                sb.AppendLine("    <soundOverrides>");
                foreach (var kv in v.soundOverrides)
                    if (!kv.Value.NullOrEmpty()) sb.AppendLine($"      <li>{Escape(kv.Key)}={Escape(kv.Value)}</li>");
                sb.AppendLine("    </soundOverrides>");
            }

            sb.AppendLine("  </SimpleUICustomization.ThemeDef>");
            sb.AppendLine();
            sb.AppendLine("</Defs>");
            return sb.ToString();
        }

        static string ColorXml(Color c) =>
            $"({F(c.r)}, {F(c.g)}, {F(c.b)}, {F(c.a)})";

        static string F(float f) => f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        static string Escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        public static string SanitizeDefName(string name)
        {
            if (name.NullOrEmpty()) return "";
            var sb = new StringBuilder();
            foreach (char c in name)
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-') sb.Append(c);
            string result = sb.ToString();
            return result.Length > 0 && char.IsDigit(result[0]) ? "_" + result : result;
        }

        static string ToPackageIdSafe(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in name.ToLowerInvariant())
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            string result = sb.ToString();
            return result.NullOrEmpty() ? "theme" : result;
        }
    }
}