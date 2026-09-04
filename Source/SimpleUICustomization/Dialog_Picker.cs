using System;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    public class Dialog_Picker : Window
    {
        string currentDir;
        string manualPath = "";
        Vector2 scroll;
        readonly Action<string> onSelect;
        readonly Action<ThemeDef> onSelectPreset;
        readonly Action<string> onPreview;
        readonly string[] extensions;
        readonly string[] plainList;
        readonly ThemeDef[] presetList;
        readonly bool isTextureMode;
        readonly bool mergedPicker;
        readonly string[] drives;
        bool showingGameTextures;
        string searchQuery = "";
        string[] filteredTextures = new string[0];
        const int MaxResults = 200;
        public const string GamePrefix = "game:";
        public const string FilePrefix = "file:";

        public Dialog_Picker(Action<string> onSelect, string[] plainList, bool isPlainList, Action<string> onPreview = null)
        {
            this.onSelect = onSelect;
            this.plainList = plainList;
            this.onPreview = onPreview;
            doCloseButton = true;
            doCloseX = true;
            forcePause = true;
        }

        public Dialog_Picker(Action<ThemeDef> onSelectPreset, ThemeDef[] presetList)
        {
            this.presetList = presetList;
            this.onSelectPreset = onSelectPreset;
            doCloseButton = true;
            doCloseX = true;
            forcePause = true;
        }

        public Dialog_Picker(Action<string> onSelect, bool isTextureMode, bool merged = true, params string[] diskExtensions)
        {
            this.onSelect = onSelect;
            this.isTextureMode = isTextureMode;
            this.mergedPicker = merged;
            extensions = diskExtensions.Length > 0 ? diskExtensions : new[] { ".png", ".jpg", ".jpeg" };
            showingGameTextures = true;
            doCloseButton = true;
            doCloseX = true;
            forcePause = true;
            drives = Directory.GetLogicalDrives();
            currentDir = drives.FirstOrDefault() ?? "/";
            manualPath = currentDir;
            RefreshTextureFilter();
        }

        public override Vector2 InitialSize => new Vector2(560f, 640f);

        public override void DoWindowContents(Rect inRect)
        {
            inRect.height -= CloseButSize.y + 10f;

            if (isTextureMode)
            {
                if (mergedPicker)
                {
                    Rect toggleRow = new Rect(inRect.x, inRect.y, inRect.width, 28f);
                    if (Widgets.ButtonText(toggleRow.LeftHalf(), showingGameTextures ? "SUIC_InGameTextureChecked".Translate() : "SUIC_InGameTexture".Translate()))
                        showingGameTextures = true;
                    if (Widgets.ButtonText(toggleRow.RightHalf(), !showingGameTextures ? "SUIC_FromDiskChecked".Translate() : "SUIC_FromDisk".Translate()))
                        showingGameTextures = false;

                    Rect body = new Rect(inRect.x, inRect.y + 34f, inRect.width, inRect.height - 34f);
                    if (showingGameTextures) DrawTextureSearch(body);
                    else DrawDiskBrowser(body);
                    return;
                }
                DrawTextureSearch(inRect);
                return;
            }

            if (presetList != null)
            {
                DrawPresetList(inRect);
                return;
            }

            if (plainList != null)
            {
                DrawPlainList(inRect);
                return;
            }

            DrawDiskBrowser(inRect);
        }

        void DrawList(Rect rect, int count, float rowH, Action<Rect, int> drawRow)
        {
            Rect viewRect = new Rect(0, 0, rect.width - 16f, count * (rowH + 4f));
            Widgets.BeginScrollView(rect, ref scroll, viewRect);
            for (int i = 0; i < count; i++)
                drawRow(new Rect(0, i * (rowH + 4f), viewRect.width, rowH), i);
            Widgets.EndScrollView();
        }

        void DrawDiskBrowser(Rect inRect)
        {
            float driveRowH = drives != null && drives.Length > 1 ? 28f : 0f;
            if (driveRowH > 0f)
                DrawDriveRow(new Rect(inRect.x, inRect.y, inRect.width, driveRowH));

            Rect pathRow = new Rect(inRect.x, inRect.y + driveRowH + (driveRowH > 0f ? 6f : 0f), inRect.width, 28f);
            DrawPathEntry(pathRow);

            Rect listRect = new Rect(inRect.x, pathRow.yMax + 6f, inRect.width, inRect.yMax - pathRow.yMax - 6f);
            DrawFileList(listRect);
        }

        void DrawDriveRow(Rect rect)
        {
            float btnW = rect.width / drives.Length;
            for (int i = 0; i < drives.Length; i++)
            {
                Rect btn = new Rect(rect.x + i * btnW, rect.y, btnW - 4f, rect.height);
                bool active = currentDir.StartsWith(drives[i], StringComparison.OrdinalIgnoreCase);
                if (active) GUI.color = Widgets.NormalOptionColor;
                if (Widgets.ButtonText(btn, drives[i]))
                {
                    currentDir = drives[i];
                    manualPath = currentDir;
                }
                GUI.color = Color.white;
            }
        }

        void RefreshTextureFilter()
        {
            var all = ThemeLoader.AllTexturePaths();
            if (searchQuery.NullOrEmpty())
            {
                filteredTextures = all.Take(MaxResults).ToArray();
            }
            else
            {
                filteredTextures = all.Where(p => p.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Take(MaxResults).ToArray();
            }
        }

        void DrawTextureSearch(Rect rect)
        {
            Rect searchRow = new Rect(rect.x, rect.y, rect.width, 28f);
            string prevQuery = searchQuery;
            searchQuery = Widgets.TextField(searchRow, searchQuery);
            if (searchQuery != prevQuery) RefreshTextureFilter();

            var expansions = searchQuery.NullOrEmpty() ? ThemeLoader.ExpansionBackgrounds() : new (string, string, Texture2D)[0];

            Rect countRow = new Rect(rect.x, rect.y + 32f, rect.width, 20f);
            Widgets.Label(countRow, $"{filteredTextures.Length} result(s){(filteredTextures.Length == MaxResults ? " (capped, refine search)" : "")}");

            Rect listRect = new Rect(rect.x, rect.y + 56f, rect.width, rect.height - 56f);
            float rowH = 32f;
            DrawList(listRect, expansions.Length + filteredTextures.Length, rowH, (rowRect, i) =>
            {
                if (i < expansions.Length)
                {
                    var (key, label, tex) = expansions[i];
                    Widgets.DrawTextureFitted(new Rect(rowRect.x, rowRect.y, rowH, rowH), tex, 1f);
                    if (Widgets.ButtonText(new Rect(rowRect.x + rowH + 4f, rowRect.y, rowRect.width - rowH - 4f, rowH), "DLC: " + label))
                    {
                        onSelect?.Invoke(key);
                        Close();
                    }
                    return;
                }
                var path = filteredTextures[i - expansions.Length];
                var ptex = ThemeLoader.GetTexture(path);
                if (ptex != null)
                    Widgets.DrawTextureFitted(new Rect(rowRect.x, rowRect.y, rowH, rowH), ptex, 1f);
                if (Widgets.ButtonText(new Rect(rowRect.x + rowH + 4f, rowRect.y, rowRect.width - rowH - 4f, rowH), path))
                {
                    onSelect?.Invoke(GamePrefix + path);
                    Close();
                }
            });
        }

        void DrawPathEntry(Rect rect)
        {
            Rect field = new Rect(rect.x, rect.y, rect.width - 60f, rect.height);
            Rect goBtn = new Rect(rect.xMax - 56f, rect.y, 56f, rect.height);
            manualPath = Widgets.TextField(field, manualPath);
            if (!Widgets.ButtonText(goBtn, "SUIC_Go".Translate())) return;
            try
            {
                if (Directory.Exists(manualPath))
                {
                    currentDir = manualPath;
                }
                else if (File.Exists(manualPath) && extensions.Any(e => manualPath.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                {
                    onSelect?.Invoke(isTextureMode ? FilePrefix + manualPath : manualPath);
                    Close();
                }
                else
                {
                    Messages.Message("SUIC_PathNotFound".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
            catch (Exception e)
            {
                Messages.Message("SUIC_InvalidPath".Translate(e.Message), MessageTypeDefOf.RejectInput, false);
            }
        }

        string cachedListDir;
        string[] cachedDirs;
        string[] cachedFiles;

        static bool IsAccessible(Func<bool> check)
        {
            try { return check(); }
            catch { return false; }
        }

        void DrawFileList(Rect rect)
        {
            if (cachedListDir != currentDir)
            {
                try
                {
                    cachedDirs = Directory.GetDirectories(currentDir)
                        .Where(d => IsAccessible(() => (new DirectoryInfo(d).Attributes & FileAttributes.Hidden) == 0))
                        .OrderBy(d => d).ToArray();
                    cachedFiles = Directory.GetFiles(currentDir)
                        .Where(f => IsAccessible(() => (new FileInfo(f).Attributes & FileAttributes.Hidden) == 0))
                        .Where(f => extensions.Any(e => f.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(f => f).ToArray();
                    cachedListDir = currentDir;
                }
                catch (Exception e)
                {
                    Widgets.Label(rect, "SUIC_CantAccessFolder".Translate(e.Message));
                    return;
                }
            }
            var dirs = cachedDirs;
            var files = cachedFiles;

            var parent = Directory.GetParent(currentDir);
            int upRow = parent != null ? 1 : 0;
            int rowCount = upRow + dirs.Length + files.Length;
            DrawList(rect, rowCount, 28f, (rowRect, i) =>
            {
                if (upRow == 1 && i == 0)
                {
                    if (Widgets.ButtonText(rowRect, ".. (up)"))
                    {
                        currentDir = parent.FullName;
                        manualPath = currentDir;
                    }
                    return;
                }
                int di = i - upRow;
                if (di < dirs.Length)
                {
                    var d = dirs[di];
                    if (Widgets.ButtonText(rowRect, Path.GetFileName(d) + "/"))
                    {
                        currentDir = d;
                        manualPath = currentDir;
                    }
                    return;
                }
                var f = files[di - dirs.Length];
                if (Widgets.ButtonText(rowRect, Path.GetFileName(f)))
                {
                    onSelect?.Invoke(isTextureMode ? FilePrefix + f : f);
                    Close();
                }
            });
        }

        string cachedPresetQuery;
        ThemeDef[] cachedPresetFiltered;

        void DrawPresetList(Rect rect)
        {
            if (presetList.Length == 0)
            {
                Widgets.Label(rect, "SUIC_NoPresetsFound".Translate());
                return;
            }

            Rect searchRow = new Rect(rect.x, rect.y, rect.width, 28f);
            searchQuery = Widgets.TextField(searchRow, searchQuery);
            if (cachedPresetFiltered == null || cachedPresetQuery != searchQuery)
            {
                cachedPresetFiltered = searchQuery.NullOrEmpty()
                    ? presetList
                    : presetList.Where(d => d.label.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                cachedPresetQuery = searchQuery;
            }
            var filtered = cachedPresetFiltered;

            if (filtered.Length == 0)
            {
                Widgets.Label(new Rect(rect.x, rect.y + 34f, rect.width, 24f), "SUIC_NoEntriesFound".Translate());
                return;
            }

            Rect listRect = new Rect(rect.x, rect.y + 34f, rect.width, rect.height - 34f);
            DrawList(listRect, filtered.Length, 36f, (rowRect, i) =>
            {
                var def = filtered[i];
                Widgets.DrawBoxSolid(new Rect(rowRect.xMax - 68f, rowRect.y + 4f, 28f, 28f), def.accentColor);
                Widgets.DrawBoxSolid(new Rect(rowRect.xMax - 36f, rowRect.y + 4f, 28f, 28f), def.backgroundColor);
                if (Widgets.ButtonText(new Rect(rowRect.x, rowRect.y, rowRect.width - 76f, rowRect.height), def.label))
                {
                    onSelectPreset?.Invoke(def);
                    Close();
                }
            });
        }

        string cachedPlainQuery;
        string[] cachedPlainFiltered;

        void DrawPlainList(Rect rect)
        {
            if (plainList.Length == 0)
            {
                Widgets.Label(rect, "SUIC_NoEntriesFound".Translate());
                return;
            }

            Rect searchRow = new Rect(rect.x, rect.y, rect.width, 28f);
            searchQuery = Widgets.TextField(searchRow, searchQuery);
            if (cachedPlainFiltered == null || cachedPlainQuery != searchQuery)
            {
                cachedPlainFiltered = searchQuery.NullOrEmpty()
                    ? plainList
                    : plainList.Where(n => n.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                cachedPlainQuery = searchQuery;
            }
            var filtered = cachedPlainFiltered;

            Rect listRect = new Rect(rect.x, rect.y + 34f, rect.width, rect.height - 34f);
            DrawList(listRect, filtered.Length, 28f, (rowRect, i) =>
            {
                Rect textRect = rowRect;
                if (onPreview != null)
                {
                    Rect playBtn = rowRect.LeftPartPixels(28f);
                    textRect = new Rect(playBtn.xMax + 4f, rowRect.y, rowRect.width - playBtn.width - 4f, rowRect.height);
                    if (Widgets.ButtonText(playBtn, "▶")) onPreview(filtered[i]);
                }
                if (Widgets.ButtonText(textRect, filtered[i]))
                {
                    onSelect?.Invoke(filtered[i]);
                    Close();
                }
            });
        }
    }
}