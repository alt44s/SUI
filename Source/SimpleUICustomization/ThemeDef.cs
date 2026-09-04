using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    public class ThemeDef : Def
    {
        public Color accentColor = new Color(0.4f, 0.7f, 1f);
        public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        public Color? hoverColor;
        public Color? clickColor;
        public Color? textColor;
        public string fontName = "";
        public int fontSizeOffset = 0;
        public string cursorPath = "";

        public List<string> disabledElements = new List<string>();
        public Color? windowBGColor;
        public Color? menuSectionBGColor;
        public Color? tabBGColor;
        public Color? buttonBGColor;
        public Color? bottomBarBGColor;
        public Color? gizmoBGColor;
        public Color? scrollbarBGColor;
        public Color? checkboxBGColor;
        public Color? sliderBGColor;
        public Color? textFieldBGColor;
        public Color? fillableBarBGColor;
        public Color? fillableBarBackdropBGColor;
        public string windowTex = "";
        public bool windowBGSliced = true;
        public bool windowBGOutline = true;
        public string menuSectionTex = "";
        public string tabTex = "";
        public string buttonTex = "";
        public string bottomBarTex = "";
        public string gizmoTex = "";
        public string scrollbarTex = "";
        public string titleTex = "";
        public string checkboxOnTex = "";
        public string checkboxOffTex = "";
        public string checkboxPartialTex = "";
        public string textFieldTex = "";
        public string fillableBarFillTex = "";
        public string fillableBarBGTex = "";

        public List<string> mainMenuBackgrounds = new List<string>();
        public bool bgCycle = false;
        public float bgCycleSeconds = 30f;
        public float bgFadeSeconds = 1.5f;

        public bool soundsEnabled = false;
        public List<string> soundOverrides = new List<string>();

        public Dictionary<string, string> SoundOverridesDict()
        {
            var dict = new Dictionary<string, string>();
            foreach (var entry in soundOverrides)
            {
                int i = entry.IndexOf('=');
                if (i > 0) dict[entry.Substring(0, i)] = entry.Substring(i + 1);
            }
            return dict;
        }

        public ThemeValues ToValues()
        {
            return new ThemeValues
            {
                accent = accentColor,
                background = backgroundColor,
                hoverColor = hoverColor,
                clickColor = clickColor,
                textColor = textColor,
                fontName = fontName,
                fontSizeOffset = fontSizeOffset,
                cursorPath = cursorPath,
                cursorModRoot = modContentPack != null ? modContentPack.RootDir : "",
                disabledElements = new HashSet<string>(disabledElements),
                windowBG = windowBGColor,
                menuSectionBG = menuSectionBGColor,
                tabBG = tabBGColor,
                buttonBG = buttonBGColor,
                bottomBarBG = bottomBarBGColor,
                gizmoBG = gizmoBGColor,
                scrollbarBG = scrollbarBGColor,
                checkboxBG = checkboxBGColor,
                sliderBG = sliderBGColor,
                textFieldBG = textFieldBGColor,
                fillableBarBG = fillableBarBGColor,
                fillableBarBackdropBG = fillableBarBackdropBGColor,
                windowTex = windowTex,
                windowBGSliced = windowBGSliced,
                windowBGOutline = windowBGOutline,
                menuSectionTex = menuSectionTex,
                tabTex = tabTex,
                buttonTex = buttonTex,
                bottomBarTex = bottomBarTex,
                gizmoTex = gizmoTex,
                scrollbarTex = scrollbarTex,
                titleTex = titleTex,
                checkboxOnTex = checkboxOnTex,
                checkboxOffTex = checkboxOffTex,
                checkboxPartialTex = checkboxPartialTex,
                textFieldTex = textFieldTex,
                fillableBarFillTex = fillableBarFillTex,
                fillableBarBGTex = fillableBarBGTex,
                mainMenuBackgrounds = new List<string>(mainMenuBackgrounds),
                bgCycle = bgCycle,
                bgCycleSeconds = bgCycleSeconds,
                bgFadeSeconds = bgFadeSeconds,
                soundsEnabled = soundsEnabled,
                soundOverrides = SoundOverridesDict()
            };
        }
    }
}