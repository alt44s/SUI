using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Input = UnityEngine.Input;

namespace SimpleUICustomization
{
    public class SimpleUICustomizationMod : Mod
    {
        public static UISettings Settings;
        public static string ModRootDir;
        public static ModContentPack ContentPack;
        Vector2 scroll;
        float contentHeight = 800f;
        bool draggingFontSize;
        static readonly System.Collections.Generic.HashSet<string> expandedSections = new System.Collections.Generic.HashSet<string>();

        public SimpleUICustomizationMod(ModContentPack content) : base(content)
        {
            ContentPack = content;
            Settings = GetSettings<UISettings>();
            ModRootDir = content.RootDir;
            LongEventHandler.ExecuteWhenFinished(() => ThemeLoader.ApplyAll(Settings.live));
        }

        public override string SettingsCategory() => "SUIC_SettingsCategory".Translate();

        public override void WriteSettings()
        {
            Settings.ClearHistory();
            base.WriteSettings();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            const float historyH = 26f, historyW = 32f;
            Rect historyRow = new Rect(inRect.xMax - (historyW * 2f + 4f), inRect.y, historyW * 2f + 4f, historyH);
            GUI.enabled = Settings.CanUndo;
            if (Widgets.ButtonText(new Rect(historyRow.x, historyRow.y, historyW, historyH), "<<")) Settings.Undo();
            GUI.enabled = Settings.CanRedo;
            if (Widgets.ButtonText(new Rect(historyRow.xMax - historyW, historyRow.y, historyW, historyH), ">>")) Settings.Redo();
            GUI.enabled = true;
            TooltipHandler.TipRegion(new Rect(historyRow.x, historyRow.y, historyW, historyH), "SUIC_Undo".Translate());
            TooltipHandler.TipRegion(new Rect(historyRow.xMax - historyW, historyRow.y, historyW, historyH), "SUIC_Redo".Translate());

            Rect scrollArea = new Rect(inRect.x, inRect.y + historyH + 6f, inRect.width, inRect.height - historyH - 6f);
            float estimatedHeight = Mathf.Max(contentHeight + 40f, 2400f);
            Rect viewRect = new Rect(0, 0, scrollArea.width - 16f, estimatedHeight);
            Widgets.BeginScrollView(scrollArea, ref scroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.CheckboxLabeled("SUIC_EnableTheming".Translate(), ref Settings.enabled);
            listing.Label("SUIC_Active".Translate(Settings.CurrentLabel()));
            listing.Gap();

            listing.Label("SUIC_Presets".Translate());
            if (listing.ButtonText("SUIC_PickPreset".Translate()))
            {
                Find.WindowStack.Add(new Dialog_Picker(def => Settings.ApplyPreset(def), Settings.AllPresets().ToArray()));
            }
            if (listing.ButtonText("SUIC_ExportCurrentTheme".Translate()))
            {
                Find.WindowStack.Add(new Dialog_TextEntry("MyTheme", "Your Name Here", "A SimplyUI theme, exported for sharing.", DoExport));
            }
            listing.Gap(12f);

            listing.Label("SUIC_AccentColor".Translate());
            ColorButton(listing.GetRect(24f), () => Settings.live.accent, c => { Settings.live.accent = c; Settings.Notify_Edited(); }, Dialog_ColorPicker.PreviewKind.Accent);
            listing.Gap(6f);

            listing.Label("SUIC_BackgroundColor".Translate());
            ColorButton(listing.GetRect(24f), () => Settings.live.background, c => { Settings.live.background = c; Settings.Notify_Edited(); }, Dialog_ColorPicker.PreviewKind.WindowBG);
            listing.Gap(12f);

            listing.Label("SUIC_Font".Translate());
            Rect fontRow = listing.GetRect(28f);
            string fontLabel = Settings.live.fontName.NullOrEmpty() ? "SUIC_Vanilla".Translate().ToString() : Settings.live.fontName;
            if (Widgets.ButtonText(fontRow.LeftHalf(), "SUIC_PickLabel".Translate(fontLabel)))
            {
                var names = ThemeLoader.AvailableFontNames();
                Find.WindowStack.Add(new Dialog_Picker(name =>
                {
                    Settings.live.fontName = name;
                    Settings.Notify_Edited();
                    LongEventHandler.ExecuteWhenFinished(() => ThemeLoader.ApplyFont(name, Settings.live.fontSizeOffset));
                }, names, true));
            }
            if (Widgets.ButtonText(fontRow.RightHalf(), "SUIC_ResetToVanilla".Translate()))
            {
                Settings.live.fontName = "";
                Settings.live.fontSizeOffset = 0;
                Settings.Notify_Edited();
                LongEventHandler.ExecuteWhenFinished(() => ThemeLoader.ApplyFont("", 0));
            }

            listing.Label("SUIC_TextColor".Translate());
            Rect textColorRow = listing.GetRect(24f);
            ColorButton(textColorRow.LeftPartPixels(textColorRow.width - 70f), () => Settings.live.textColor ?? Color.white, c => { Settings.live.textColor = c; Settings.Notify_Edited(); }, Dialog_ColorPicker.PreviewKind.TextColor);
            if (Widgets.ButtonText(new Rect(textColorRow.xMax - 64f, textColorRow.y, 64f, textColorRow.height), "SUIC_Reset".Translate()))
            {
                Settings.live.textColor = null;
                Settings.Notify_Edited();
            }
            listing.Gap(6f);
            listing.Label("SUIC_FontSizeOffset".Translate(Settings.live.fontSizeOffset.ToString("+0;-0;0")));
            Rect sizeRow = listing.GetRect(24f);
            int prevOffset = Settings.live.fontSizeOffset;
            bool wasDragging = draggingFontSize;
            draggingFontSize = Input.GetMouseButton(0) && Mouse.IsOver(sizeRow);
            Settings.live.fontSizeOffset = Mathf.RoundToInt(Widgets.HorizontalSlider(sizeRow, Settings.live.fontSizeOffset, -8f, 8f, roundTo: 1f));
            if (Settings.live.fontSizeOffset != prevOffset) Settings.Notify_Edited();
            if (!draggingFontSize && (Settings.live.fontSizeOffset != prevOffset || wasDragging))
                LongEventHandler.ExecuteWhenFinished(() => ThemeLoader.ApplyFont(Settings.live.fontName, Settings.live.fontSizeOffset));
            listing.Gap(12f);

            listing.Label("SUIC_CustomCursor".Translate());
            Rect cursorRow = listing.GetRect(28f);
            string cursorLabel = Settings.live.cursorPath.NullOrEmpty() ? "NoneBrackets".Translate().ToString() : Path.GetFileName(Settings.live.cursorPath);
            if (Widgets.ButtonText(cursorRow.LeftHalf(), "SUIC_PickLabel".Translate(cursorLabel)))
            {
                Find.WindowStack.Add(new Dialog_Picker(path =>
                {
                    Settings.live.cursorPath = path;
                    Settings.Notify_Edited();
                    LongEventHandler.ExecuteWhenFinished(() => ThemeLoader.ApplyCursor(path));
                }, true, true, ".png", ".jpg", ".jpeg", ".cur", ".ani", ".ico"));
            }
            if (Widgets.ButtonText(cursorRow.RightHalf(), "SUIC_ResetCursor".Translate()))
            {
                Settings.live.cursorPath = "";
                Settings.Notify_Edited();
                LongEventHandler.ExecuteWhenFinished(() => ThemeLoader.ApplyCursor(""));
            }
            listing.Gap(12f);

            bool advOpen = Section(listing, "SUIC_AdvancedPerElementColors".Translate(), "advanced");

            if (advOpen)
            {
                if (Section(listing, "SUIC_GroupTints".Translate(), "tints"))
                {
                    AdvancedRow(listing, "SUIC_HoverTintColor".Translate(), ThemeValues.Elem.HoverClick, () => Settings.live.hoverColor, c => Settings.live.hoverColor = c, Dialog_ColorPicker.PreviewKind.Accent);
                    AdvancedRow(listing, "SUIC_ClickTintColor".Translate(), ThemeValues.Elem.HoverClick, () => Settings.live.clickColor, c => Settings.live.clickColor = c, Dialog_ColorPicker.PreviewKind.Accent);
                }

                if (Section(listing, "SUIC_GroupPanels".Translate(), "panels"))
                {
                    AdvancedRow(listing, "SUIC_WindowBackground".Translate(), ThemeValues.Elem.Window, () => Settings.live.windowBG, c => Settings.live.windowBG = c, Dialog_ColorPicker.PreviewKind.WindowBG, Settings.live.windowTex, v => Settings.live.windowTex = v);
                    if (!Settings.live.windowTex.NullOrEmpty())
                    {
                        bool prevSliced = Settings.live.windowBGSliced;
                        listing.CheckboxLabeled("SUIC_SliceTexture".Translate(), ref Settings.live.windowBGSliced);
                        if (Settings.live.windowBGSliced != prevSliced) Settings.Notify_Edited();
                    }
                    bool prevOutline = Settings.live.windowBGOutline;
                    listing.CheckboxLabeled("SUIC_WindowBGOutline".Translate(), ref Settings.live.windowBGOutline);
                    if (Settings.live.windowBGOutline != prevOutline) Settings.Notify_Edited();
                    AdvancedRow(listing, "SUIC_MenuSectionBackground".Translate(), ThemeValues.Elem.MenuSection, () => Settings.live.menuSectionBG, c => Settings.live.menuSectionBG = c, Dialog_ColorPicker.PreviewKind.MenuSectionBG, Settings.live.menuSectionTex, v => Settings.live.menuSectionTex = v);
                    AdvancedRow(listing, "SUIC_TabBackground".Translate(), ThemeValues.Elem.Tab, () => Settings.live.tabBG, c => Settings.live.tabBG = c, Dialog_ColorPicker.PreviewKind.TabBG, Settings.live.tabTex, v => Settings.live.tabTex = v);
                    AdvancedRow(listing, "SUIC_ButtonBackground".Translate(), ThemeValues.Elem.Button, () => Settings.live.buttonBG, c => Settings.live.buttonBG = c, Dialog_ColorPicker.PreviewKind.ButtonBG, Settings.live.buttonTex, v => Settings.live.buttonTex = v);
                    AdvancedRow(listing, "SUIC_BottomBarBackground".Translate(), ThemeValues.Elem.BottomBar, () => Settings.live.bottomBarBG, c => Settings.live.bottomBarBG = c, Dialog_ColorPicker.PreviewKind.BottomBarBG, Settings.live.bottomBarTex, v => Settings.live.bottomBarTex = v);
                    AdvancedRow(listing, "SUIC_GizmoBackground".Translate(), ThemeValues.Elem.Gizmo, () => Settings.live.gizmoBG, c => Settings.live.gizmoBG = c, Dialog_ColorPicker.PreviewKind.GizmoBG, Settings.live.gizmoTex, v => Settings.live.gizmoTex = v);
                    AdvancedRow(listing, "SUIC_Scrollbar".Translate(), ThemeValues.Elem.Scrollbar, () => Settings.live.scrollbarBG, c => Settings.live.scrollbarBG = c, Dialog_ColorPicker.PreviewKind.Scrollbar, Settings.live.scrollbarTex, v => Settings.live.scrollbarTex = v);
                }

                if (Section(listing, "SUIC_GroupControls".Translate(), "controls"))
                {
                    AdvancedRow(listing, "SUIC_CheckboxTint".Translate(), ThemeValues.Elem.Checkbox, () => Settings.live.checkboxBG, c => Settings.live.checkboxBG = c, Dialog_ColorPicker.PreviewKind.Checkbox);
                    CheckboxTexRow(listing);
                    AdvancedRow(listing, "SUIC_SliderTint".Translate(), ThemeValues.Elem.Slider, () => Settings.live.sliderBG, c => Settings.live.sliderBG = c, Dialog_ColorPicker.PreviewKind.Slider);
                    AdvancedRow(listing, "SUIC_TextFieldBackground".Translate(), ThemeValues.Elem.TextField, () => Settings.live.textFieldBG, c => Settings.live.textFieldBG = c, Dialog_ColorPicker.PreviewKind.MenuSectionBG, Settings.live.textFieldTex, v => Settings.live.textFieldTex = v);
                    AdvancedRow(listing, "SUIC_FillableBarTint".Translate(), ThemeValues.Elem.FillableBar, () => Settings.live.fillableBarBG, c => Settings.live.fillableBarBG = c, Dialog_ColorPicker.PreviewKind.FillableBar, Settings.live.fillableBarFillTex, v => Settings.live.fillableBarFillTex = v);
                    AdvancedRow(listing, "SUIC_FillableBarBackdropTint".Translate(), ThemeValues.Elem.FillableBar, () => Settings.live.fillableBarBackdropBG, c => Settings.live.fillableBarBackdropBG = c, Dialog_ColorPicker.PreviewKind.FillableBarBackdrop, Settings.live.fillableBarBGTex, v => Settings.live.fillableBarBGTex = v);
                }

                if (Section(listing, "SUIC_GroupMisc".Translate(), "misc"))
                    TextureOnlyRow(listing, "SUIC_MainMenuTitle".Translate(), ThemeValues.Elem.MainMenuTitle, Settings.live.titleTex, v => Settings.live.titleTex = v);
                listing.Gap(8f);
            }
            listing.Gap(12f);

            listing.Label("SUIC_MainMenuBackgrounds".Translate(Settings.live.mainMenuBackgrounds.Count));
            if (listing.ButtonText("SUIC_ManageBackgrounds".Translate()))
            {
                Find.WindowStack.Add(new Dialog_BackgroundManager(Settings.live.mainMenuBackgrounds, () => { Settings.Notify_Edited(); }));
            }
            bool prevCycle = Settings.live.bgCycle;
            listing.CheckboxLabeled("SUIC_CycleBackgrounds".Translate(), ref Settings.live.bgCycle);
            if (Settings.live.bgCycle != prevCycle) Settings.Notify_Edited();
            if (Settings.live.bgCycle)
            {
                listing.Label("SUIC_CycleInterval".Translate(FormatDuration(Settings.live.bgCycleSeconds)));
                Settings.live.bgCycleSeconds = Widgets.HorizontalSlider(listing.GetRect(24f), Settings.live.bgCycleSeconds, 5f, 600f, roundTo: 5f);

                listing.Label("SUIC_FadeDuration".Translate(Settings.live.bgFadeSeconds.ToString("F1")));
                Settings.live.bgFadeSeconds = Widgets.HorizontalSlider(listing.GetRect(24f), Settings.live.bgFadeSeconds, 0.2f, 5f, roundTo: 0.1f);
            }
            listing.Gap(12f);

            bool prevSoundsEnabled = Settings.live.soundsEnabled;
            listing.CheckboxLabeled("SUIC_EnableUISounds".Translate(), ref Settings.live.soundsEnabled);
            if (Settings.live.soundsEnabled != prevSoundsEnabled) Settings.Notify_Edited();

            if (Settings.live.soundsEnabled)
            {
                listing.Label("SUIC_SoundOverrideCount".Translate(Settings.live.soundOverrides.Count));
                if (listing.ButtonText("SUIC_AddSoundOverride".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_Picker(vanillaName =>
                    {
                        Find.WindowStack.Add(new Dialog_Picker(replacement =>
                        {
                            Settings.live.soundOverrides[vanillaName] = replacement;
                            Settings.Notify_Edited();
                        }, ThemeAudio.AllSoundDefNames(), true, name => ThemeAudio.Get(name)?.PlayOneShotOnCamera()));
                    }, ThemeAudio.UISoundNames, true, name => ThemeAudio.Get(name)?.PlayOneShotOnCamera()));
                }
                string removeKey = null;
                foreach (var kv in Settings.live.soundOverrides)
                {
                    if (SoundRow(listing, kv.Key, kv.Value)) removeKey = kv.Key;
                }
                if (removeKey != null)
                {
                    Settings.live.soundOverrides.Remove(removeKey);
                    Settings.Notify_Edited();
                }
            }

            contentHeight = listing.CurHeight + 20f;
            listing.End();
            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        static string FormatDuration(float seconds)
        {
            int total = Mathf.RoundToInt(seconds);
            if (total < 60) return total + "s";
            int mins = total / 60, secs = total % 60;
            return secs == 0 ? mins + "m" : $"{mins}m {secs}s";
        }

        void DoExport(string name, string author, string desc)
        {
            string safeName = ThemeExporter.SanitizeFileName(name);
            if (safeName.NullOrEmpty()) return;
            string existing = Path.Combine(ThemeExporter.ExportRoot, safeName);
            if (Directory.Exists(existing))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "SUIC_OverwriteConfirm".Translate(safeName, existing),
                    () => RunExport(safeName, author, desc), true));
            }
            else
            {
                RunExport(safeName, author, desc);
            }
        }

        void RunExport(string safeName, string author, string desc)
        {
            var result = ThemeExporter.Export(safeName, Settings.live, author, desc);
            if (!result.success)
            {
                Messages.Message(result.error, MessageTypeDefOf.RejectInput, false);
                return;
            }
            Find.WindowStack.Add(new Dialog_TextEntry("SUIC_ExportedToLabel".Translate(), result.folder, "SUIC_ExportedToNote".Translate()));
        }

        static bool Section(Listing_Standard listing, string label, string key)
        {
            bool open = expandedSections.Contains(key);
            Rect row = listing.GetRect(26f);
            Widgets.DrawBoxSolid(row, new Color(1f, 1f, 1f, 0.05f));
            Widgets.DrawHighlightIfMouseover(row);
            Widgets.Label(new Rect(row.x + 8f, row.y, row.width - 16f, row.height), (open ? "▼ " : "▶ ") + label);
            if (Widgets.ButtonInvisible(row)) { if (open) expandedSections.Remove(key); else expandedSections.Add(key); }
            listing.Gap(open ? 4f : 2f);
            return open;
        }

        void AdvancedRow(Listing_Standard listing, string label, string elemKey, System.Func<Color?> getColor, System.Action<Color?> setColor, Dialog_ColorPicker.PreviewKind previewKind, string texValue = null, System.Action<string> setTex = null)
        {
            Color? current = getColor();
            Rect labelRow = listing.GetRect(24f);
            bool enabled = !Settings.live.disabledElements.Contains(elemKey);
            bool prevEnabled = enabled;
            Widgets.CheckboxLabeled(labelRow.LeftPartPixels(labelRow.width - 30f),
                label + (current.HasValue ? "" : "SUIC_Inherited".Translate().ToString()) + (texValue.NullOrEmpty() ? "" : "SUIC_CustomTexture".Translate().ToString()),
                ref enabled);
            if (enabled != prevEnabled)
            {
                if (enabled) Settings.live.disabledElements.Remove(elemKey);
                else Settings.live.disabledElements.Add(elemKey);
                Settings.Notify_Edited();
            }

            GUI.enabled = enabled;
            Rect row = listing.GetRect(24f);
            Texture2D previewTex = texValue.NullOrEmpty() ? null : ThemeLoader.GetTexture(texValue);
            ColorButton(row.LeftPartPixels(row.width - 70f), () => getColor() ?? Settings.live.accent, c => { setColor(c); Settings.Notify_Edited(); }, previewKind, previewTex);
            if (Widgets.ButtonText(new Rect(row.xMax - 64f, row.y, 64f, row.height), "SUIC_Reset".Translate()))
            {
                setColor(null);
                Settings.Notify_Edited();
            }

            if (setTex != null) TexturePickerRow(listing.GetRect(24f), texValue, setTex);
            GUI.enabled = true;
            listing.Gap(4f);
        }

        void TexturePickerRow(Rect row, string texValue, System.Action<string> setTex, string pickLabelKey = "SUIC_PickTexture")
        {
            string texLabel = texValue.NullOrEmpty() ? pickLabelKey.Translate().ToString() : texValue;
            if (Widgets.ButtonText(row.LeftPartPixels(row.width - 70f), texLabel))
            {
                Find.WindowStack.Add(new Dialog_Picker(path =>
                {
                    setTex(path);
                    Settings.Notify_Edited();
                }, true));
            }
            if (Widgets.ButtonText(new Rect(row.xMax - 64f, row.y, 64f, row.height), "SUIC_Clear".Translate()))
            {
                setTex("");
                Settings.Notify_Edited();
            }
        }

        void CheckboxTexRow(Listing_Standard listing)
        {
            GUI.enabled = !Settings.live.disabledElements.Contains(ThemeValues.Elem.Checkbox);
            Rect row = listing.GetRect(24f);
            float w = (row.width - 16f) / 3f;
            Rect onRow = new Rect(row.x, row.y, w, row.height);
            Rect offRow = new Rect(row.x + w + 8f, row.y, w, row.height);
            Rect partialRow = new Rect(row.x + (w + 8f) * 2f, row.y, w, row.height);
            TexturePickerRow(onRow, Settings.live.checkboxOnTex, v => Settings.live.checkboxOnTex = v, "SUIC_PickOnTexture");
            TexturePickerRow(offRow, Settings.live.checkboxOffTex, v => Settings.live.checkboxOffTex = v, "SUIC_PickOffTexture");
            TexturePickerRow(partialRow, Settings.live.checkboxPartialTex, v => Settings.live.checkboxPartialTex = v, "SUIC_PickPartialTexture");
            GUI.enabled = true;
            listing.Gap(4f);
        }

        void TextureOnlyRow(Listing_Standard listing, string label, string elemKey, string texValue, System.Action<string> setTex)
        {
            Rect labelRow = listing.GetRect(24f);
            bool enabled = !Settings.live.disabledElements.Contains(elemKey);
            bool prevEnabled = enabled;
            Widgets.CheckboxLabeled(labelRow, label + (texValue.NullOrEmpty() ? "" : "SUIC_CustomTexture".Translate().ToString()), ref enabled);
            if (enabled != prevEnabled)
            {
                if (enabled) Settings.live.disabledElements.Remove(elemKey);
                else Settings.live.disabledElements.Add(elemKey);
                Settings.Notify_Edited();
            }
            GUI.enabled = enabled;
            TexturePickerRow(listing.GetRect(24f), texValue, setTex);
            GUI.enabled = true;
            listing.Gap(4f);
        }

        bool SoundRow(Listing_Standard listing, string vanillaName, string replacementName)
        {
            Rect row = listing.GetRect(24f);
            Rect playBtn = new Rect(row.x, row.y, 24f, row.height);
            Rect clearBtn = new Rect(row.xMax - 64f, row.y, 64f, row.height);
            Rect labelRect = new Rect(playBtn.xMax + 4f, row.y, clearBtn.x - playBtn.xMax - 8f, row.height);

            if (Widgets.ButtonText(playBtn, "▶"))
                ThemeAudio.Get(replacementName)?.PlayOneShotOnCamera();
            Widgets.Label(labelRect, vanillaName + ": " + replacementName);
            bool remove = Widgets.ButtonText(clearBtn, "SUIC_Clear".Translate());
            listing.Gap(4f);
            return remove;
        }

        void ColorButton(Rect rect, System.Func<Color> getColor, System.Action<Color> setColor, Dialog_ColorPicker.PreviewKind previewKind = Dialog_ColorPicker.PreviewKind.None, Texture2D previewTex = null)
        {
            Color c = getColor();
            Rect swatch = rect.LeftPartPixels(24f);
            Widgets.DrawBoxSolid(swatch, c);
            Widgets.DrawBox(swatch);
            if (Widgets.ButtonText(new Rect(swatch.xMax + 6f, rect.y, rect.width - 30f, rect.height), "SUIC_PickColor".Translate()))
                Find.WindowStack.Add(new Dialog_ColorPicker(c, setColor, previewKind, previewTex));
        }
    }
}