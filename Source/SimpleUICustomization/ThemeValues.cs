using System.Collections.Generic;
using UnityEngine;

namespace SimpleUICustomization
{
    public class ThemeValues
    {
        public Color accent = new Color(0.4f, 0.7f, 1f);
        public Color background = new Color(0.15f, 0.15f, 0.15f);
        public Color? hoverColor;
        public Color? clickColor;
        public string fontName = "";
        public int fontSizeOffset = 0;
        public string cursorPath = "";
        public string cursorModRoot = "";

        public static class Elem
        {
            public const string Window = "window", MenuSection = "menuSection", Tab = "tab", Button = "button",
                BottomBar = "bottomBar", Gizmo = "gizmo", Scrollbar = "scrollbar", Checkbox = "checkbox",
                Slider = "slider", TextField = "textField", FillableBar = "fillableBar",
                MainMenuTitle = "mainMenuTitle", HoverClick = "hoverClick";
        }
        public HashSet<string> disabledElements = new HashSet<string>();
        public bool ElemOn(string key) => !disabledElements.Contains(key);
        public Color? windowBG;
        public Color? menuSectionBG;
        public Color? tabBG;
        public Color? buttonBG;
        public Color? bottomBarBG;
        public Color? gizmoBG;
        public Color? scrollbarBG;
        public Color? checkboxBG;
        public Color? sliderBG;
        public Color? textFieldBG;
        public Color? fillableBarBG;
        public Color? fillableBarBackdropBG;
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
        public Dictionary<string, string> soundOverrides = new Dictionary<string, string>();

        public ThemeValues Clone()
        {
            return new ThemeValues
            {
                accent = accent,
                background = background,
                hoverColor = hoverColor,
                clickColor = clickColor,
                fontName = fontName,
                fontSizeOffset = fontSizeOffset,
                cursorPath = cursorPath,
                cursorModRoot = cursorModRoot,

                disabledElements = new HashSet<string>(disabledElements),
                windowBG = windowBG,
                menuSectionBG = menuSectionBG,
                tabBG = tabBG,
                buttonBG = buttonBG,
                bottomBarBG = bottomBarBG,
                gizmoBG = gizmoBG,
                scrollbarBG = scrollbarBG,
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
                checkboxBG = checkboxBG,
                sliderBG = sliderBG,
                textFieldBG = textFieldBG,
                fillableBarBG = fillableBarBG,
                fillableBarBackdropBG = fillableBarBackdropBG,
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
                soundOverrides = new Dictionary<string, string>(soundOverrides)
            };
        }

        Color _hover, _click, _windowBG, _menuBG, _tabBG, _btnBG, _barBG, _gizmoBG, _scrollBG, _checkboxBG, _sliderBG, _textFieldBG, _fillableBarBG, _fillableBarBackdropBG;
        public Texture2D ResolvedWindowTex, ResolvedMenuSectionTex, ResolvedTabTex, ResolvedButtonTex, ResolvedBottomBarTex, ResolvedGizmoTex, ResolvedScrollbarTex, ResolvedTitleTex;
        public Texture2D ResolvedCheckboxOnTex, ResolvedCheckboxOffTex, ResolvedCheckboxPartialTex, ResolvedTextFieldTex, ResolvedFillableBarFillTex, ResolvedFillableBarBGTex;
        public List<Texture2D> ResolvedBackgrounds = new List<Texture2D>();

        public Color HoverColor => _hover;
        public Color ClickColor => _click;
        public Color WindowBG => _windowBG;
        public Color MenuSectionBG => _menuBG;
        public Color TabBG => _tabBG;
        public Color ButtonBG => _btnBG;
        public Color BottomBarBG => _barBG;
        public Color GizmoBG => _gizmoBG;
        public Color ScrollbarBG => _scrollBG;
        public Color CheckboxBG => _checkboxBG;
        public Color SliderBG => _sliderBG;
        public Color TextFieldBG => _textFieldBG;
        public Color FillableBarBG => _fillableBarBG;
        public Color FillableBarBackdropBG => _fillableBarBackdropBG;

        public bool HasBackgrounds
        {
            get
            {
                foreach (var t in ResolvedBackgrounds)
                    if (t != null) return true;
                return false;
            }
        }

        static Color TintRGB(Color c, float mul) => new Color(c.r * mul, c.g * mul, c.b * mul, c.a);

        public ThemeValues() => RecomputeColors();

        public void RecomputeColors()
        {
            _hover = hoverColor ?? TintRGB(accent, 1.15f);
            _click = clickColor ?? TintRGB(accent, 0.85f);
            _windowBG = windowBG ?? background;
            _menuBG = menuSectionBG ?? background;
            _tabBG = tabBG ?? accent;
            _btnBG = buttonBG ?? accent;
            _barBG = bottomBarBG ?? accent;
            _gizmoBG = gizmoBG ?? accent;
            _scrollBG = scrollbarBG ?? accent;
            _checkboxBG = checkboxBG ?? Color.white;
            _sliderBG = sliderBG ?? accent;
            _textFieldBG = textFieldBG ?? background;
            _fillableBarBG = fillableBarBG ?? accent;
            _fillableBarBackdropBG = fillableBarBackdropBG ?? Color.black;
        }

        public void RecomputeTextures()
        {
            ResolvedWindowTex = ThemeLoader.GetTexture(windowTex);
            ResolvedMenuSectionTex = ThemeLoader.GetTexture(menuSectionTex);
            ResolvedTabTex = ThemeLoader.GetTexture(tabTex);
            ResolvedButtonTex = ThemeLoader.GetTexture(buttonTex);
            ResolvedBottomBarTex = ThemeLoader.GetTexture(bottomBarTex);
            ResolvedGizmoTex = ThemeLoader.GetTexture(gizmoTex);
            ResolvedScrollbarTex = ThemeLoader.GetTexture(scrollbarTex);
            ResolvedTitleTex = ThemeLoader.GetTexture(titleTex);
            ResolvedCheckboxOnTex = ThemeLoader.GetTexture(checkboxOnTex);
            ResolvedCheckboxOffTex = ThemeLoader.GetTexture(checkboxOffTex);
            ResolvedCheckboxPartialTex = ThemeLoader.GetTexture(checkboxPartialTex);
            ResolvedTextFieldTex = ThemeLoader.GetTexture(textFieldTex);
            ResolvedFillableBarFillTex = ThemeLoader.GetTexture(fillableBarFillTex);
            ResolvedFillableBarBGTex = ThemeLoader.GetTexture(fillableBarBGTex);
            ResolvedBackgrounds.Clear();
            foreach (var path in mainMenuBackgrounds)
                ResolvedBackgrounds.Add(ThemeLoader.GetTexture(path));
        }
    }
}