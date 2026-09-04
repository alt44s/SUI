using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    public class UISettings : ModSettings
    {
        public bool enabled = false;
        public string activeThemeDefName = "";
        public ThemeValues live = new ThemeValues();
        public int editVersion;
        public int settingsVersion = 1;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref settingsVersion, "settingsVersion", 0);
            Scribe_Values.Look(ref enabled, "enabled", false);
            Scribe_Values.Look(ref activeThemeDefName, "activeThemeDefName", "");
            Scribe_Values.Look(ref live.accent, "accent", new Color(0.4f, 0.7f, 1f));
            Scribe_Values.Look(ref live.background, "background", new Color(0.15f, 0.15f, 0.15f));
            Scribe_Values.Look(ref live.hoverColor, "hoverColor", null);
            Scribe_Values.Look(ref live.clickColor, "clickColor", null);
            Scribe_Values.Look(ref live.textColor, "textColor", null);
            Scribe_Values.Look(ref live.fontName, "fontName", "");
            Scribe_Values.Look(ref live.fontSizeOffset, "fontSizeOffset", 0);
            Scribe_Values.Look(ref live.cursorPath, "cursorPath", "");
            Scribe_Values.Look(ref live.cursorModRoot, "cursorModRoot", "");
            Scribe_Collections.Look(ref live.disabledElements, "disabledElements", LookMode.Value);
            if (live.disabledElements == null) live.disabledElements = new HashSet<string>();
            Scribe_Values.Look(ref live.windowBG, "windowBG", null);
            Scribe_Values.Look(ref live.menuSectionBG, "menuSectionBG", null);
            Scribe_Values.Look(ref live.tabBG, "tabBG", null);
            Scribe_Values.Look(ref live.buttonBG, "buttonBG", null);
            Scribe_Values.Look(ref live.bottomBarBG, "bottomBarBG", null);
            Scribe_Values.Look(ref live.gizmoBG, "gizmoBG", null);
            Scribe_Values.Look(ref live.scrollbarBG, "scrollbarBG", null);
            Scribe_Values.Look(ref live.checkboxBG, "checkboxBG", null);
            Scribe_Values.Look(ref live.sliderBG, "sliderBG", null);
            Scribe_Values.Look(ref live.textFieldBG, "textFieldBG", null);
            Scribe_Values.Look(ref live.fillableBarBG, "fillableBarBG", null);
            Scribe_Values.Look(ref live.fillableBarBackdropBG, "fillableBarBackdropBG", null);
            Scribe_Values.Look(ref live.windowTex, "windowTex", "");
            Scribe_Values.Look(ref live.windowBGSliced, "windowBGSliced", true);
            Scribe_Values.Look(ref live.windowBGOutline, "windowBGOutline", true);
            Scribe_Values.Look(ref live.menuSectionTex, "menuSectionTex", "");
            Scribe_Values.Look(ref live.tabTex, "tabTex", "");
            Scribe_Values.Look(ref live.buttonTex, "buttonTex", "");
            Scribe_Values.Look(ref live.bottomBarTex, "bottomBarTex", "");
            Scribe_Values.Look(ref live.gizmoTex, "gizmoTex", "");
            Scribe_Values.Look(ref live.scrollbarTex, "scrollbarTex", "");
            Scribe_Values.Look(ref live.titleTex, "titleTex", "");
            Scribe_Values.Look(ref live.checkboxOnTex, "checkboxOnTex", "");
            Scribe_Values.Look(ref live.checkboxOffTex, "checkboxOffTex", "");
            Scribe_Values.Look(ref live.checkboxPartialTex, "checkboxPartialTex", "");
            Scribe_Values.Look(ref live.textFieldTex, "textFieldTex", "");
            Scribe_Values.Look(ref live.fillableBarFillTex, "fillableBarFillTex", "");
            Scribe_Values.Look(ref live.fillableBarBGTex, "fillableBarBGTex", "");
            Scribe_Collections.Look(ref live.mainMenuBackgrounds, "mainMenuBackgrounds", LookMode.Value);
            if (live.mainMenuBackgrounds == null) live.mainMenuBackgrounds = new System.Collections.Generic.List<string>();
            Scribe_Values.Look(ref live.bgCycle, "bgCycle", false);
            Scribe_Values.Look(ref live.bgCycleSeconds, "bgCycleSeconds", 30f);
            Scribe_Values.Look(ref live.bgFadeSeconds, "bgFadeSeconds", 1.5f);
            Scribe_Values.Look(ref live.soundsEnabled, "soundsEnabled", false);
            Scribe_Collections.Look(ref live.soundOverrides, "soundOverrides", LookMode.Value, LookMode.Value);
            if (live.soundOverrides == null) live.soundOverrides = new Dictionary<string, string>();
            base.ExposeData();
            live.RecomputeColors();
            settingsVersion = 1;
            lastSnapshot = live.Clone();
        }

        string cachedLabel;
        int cachedLabelVersion = -1;

        public string CurrentLabel()
        {
            if (cachedLabelVersion == editVersion) return cachedLabel;
            cachedLabelVersion = editVersion;
            cachedLabel = ComputeCurrentLabel();
            return cachedLabel;
        }

        string ComputeCurrentLabel()
        {
            if (activeThemeDefName.NullOrEmpty()) return "Custom";
            var def = DefDatabase<ThemeDef>.GetNamedSilentFail(activeThemeDefName);
            if (def == null) return "Custom";
            return live.accent == def.accentColor && live.background == def.backgroundColor
                && live.hoverColor == def.hoverColor && live.clickColor == def.clickColor && live.textColor == def.textColor
                && live.fontName == def.fontName && live.fontSizeOffset == def.fontSizeOffset && live.cursorPath == def.cursorPath
                && live.disabledElements.SetEquals(def.disabledElements) && live.windowBG == def.windowBGColor
                && live.menuSectionBG == def.menuSectionBGColor && live.tabBG == def.tabBGColor
                && live.buttonBG == def.buttonBGColor && live.bottomBarBG == def.bottomBarBGColor && live.gizmoBG == def.gizmoBGColor
                && live.scrollbarBG == def.scrollbarBGColor
                && live.checkboxBG == def.checkboxBGColor && live.sliderBG == def.sliderBGColor
                && live.textFieldBG == def.textFieldBGColor && live.fillableBarBG == def.fillableBarBGColor && live.fillableBarBackdropBG == def.fillableBarBackdropBGColor
                && live.windowTex == def.windowTex && live.menuSectionTex == def.menuSectionTex
                && live.windowBGSliced == def.windowBGSliced && live.windowBGOutline == def.windowBGOutline
                && live.tabTex == def.tabTex && live.buttonTex == def.buttonTex
                && live.bottomBarTex == def.bottomBarTex && live.gizmoTex == def.gizmoTex && live.scrollbarTex == def.scrollbarTex
                && live.titleTex == def.titleTex
                && live.checkboxOnTex == def.checkboxOnTex && live.checkboxOffTex == def.checkboxOffTex && live.checkboxPartialTex == def.checkboxPartialTex

                && live.textFieldTex == def.textFieldTex
                && live.fillableBarFillTex == def.fillableBarFillTex && live.fillableBarBGTex == def.fillableBarBGTex
                && live.mainMenuBackgrounds.SequenceEqual(def.mainMenuBackgrounds)
                && live.bgCycle == def.bgCycle && live.bgCycleSeconds == def.bgCycleSeconds && live.bgFadeSeconds == def.bgFadeSeconds
                && live.soundsEnabled == def.soundsEnabled && SoundOverridesEqual(live.soundOverrides, def.SoundOverridesDict())
                ? def.label : "Custom";
        }

        static bool SoundOverridesEqual(Dictionary<string, string> a, Dictionary<string, string> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kv in a)
                if (!b.TryGetValue(kv.Key, out string v) || v != kv.Value) return false;
            return true;
        }

        const int MaxHistory = 3;
        readonly List<ThemeValues> undoStack = new List<ThemeValues>();
        readonly List<ThemeValues> redoStack = new List<ThemeValues>();
        ThemeValues lastSnapshot;

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;

        public void Undo()
        {
            if (undoStack.Count == 0) return;
            redoStack.Add(live);
            if (redoStack.Count > MaxHistory) redoStack.RemoveAt(0);
            live = undoStack[undoStack.Count - 1];
            undoStack.RemoveAt(undoStack.Count - 1);
            FinishHistoryJump();
        }

        public void Redo()
        {
            if (redoStack.Count == 0) return;
            undoStack.Add(live);
            if (undoStack.Count > MaxHistory) undoStack.RemoveAt(0);
            live = redoStack[redoStack.Count - 1];
            redoStack.RemoveAt(redoStack.Count - 1);
            FinishHistoryJump();
        }

        void FinishHistoryJump()
        {
            activeThemeDefName = "";
            editVersion++;
            live.RecomputeColors();
            lastSnapshot = live.Clone();
            var snapshot = live;
            LongEventHandler.ExecuteWhenFinished(() => ThemeLoader.ApplyAll(snapshot));
        }

        public void ApplyPreset(ThemeDef def)
        {
            if (def == null) return;
            var preSwitch = live;
            activeThemeDefName = def.defName;
            live = def.ToValues();
            live.RecomputeColors();
            editVersion++;
            lastSnapshot = live.Clone();
            undoStack.Add(preSwitch);
            if (undoStack.Count > MaxHistory) undoStack.RemoveAt(0);
            redoStack.Clear();
            ThemeBackground.Reset();
            var snapshot = live;
            LongEventHandler.ExecuteWhenFinished(() => ThemeLoader.ApplyAll(snapshot));
        }

        public void ClearHistory()
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        public void Notify_Edited()
        {
            if (lastSnapshot != null)
            {
                undoStack.Add(lastSnapshot);
                if (undoStack.Count > MaxHistory) undoStack.RemoveAt(0);
                redoStack.Clear();
            }
            activeThemeDefName = "";
            editVersion++;
            live.RecomputeColors();
            live.RecomputeTextures();
            lastSnapshot = live.Clone();
        }

        public IEnumerable<ThemeDef> AllPresets()
        {
            return DefDatabase<ThemeDef>.AllDefsListForReading;
        }
    }
}