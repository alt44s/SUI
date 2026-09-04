using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    public class Dialog_ThemePreviewCapture : Window
    {
        readonly ThemeValues v;
        readonly string themeName;
        readonly System.Action<Texture2D> onCaptured;
        int framesWaited;

        public const int CaptureW = 640;
        public const int CaptureH = 360;

        public override Vector2 InitialSize => new Vector2(CaptureW, CaptureH);

        public Dialog_ThemePreviewCapture(string themeName, ThemeValues v, System.Action<Texture2D> onCaptured)
        {
            this.themeName = themeName.NullOrEmpty() ? "Theme" : themeName;
            this.v = v;
            this.onCaptured = onCaptured;
            doCloseX = false;
            doCloseButton = false;
            drawShadow = false;
            closeOnAccept = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
            forcePause = false;
            layer = WindowLayer.Super;
            focusWhenOpened = false;
            absorbInputAroundWindow = false;
        }

        protected override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect((UI.screenWidth - CaptureW) / 2f, (UI.screenHeight - CaptureH) / 2f, CaptureW, CaptureH);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, Color.white);

            GUI.color = Color.black;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.WordWrap = true;
            Rect titleRect = new Rect(40f, 40f, inRect.width - 80f, inRect.height - 120f);
            int prevSize = Text.fontStyles[(int)GameFont.Medium].fontSize;
            Text.Font = GameFont.Medium;
            Text.fontStyles[(int)GameFont.Medium].fontSize = FitFontSize(themeName, titleRect.size);
            Widgets.Label(titleRect, themeName);
            Text.fontStyles[(int)GameFont.Medium].fontSize = prevSize;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            DrawMockup(inRect);

            framesWaited++;
            if (framesWaited == 2 && Event.current.type == EventType.Repaint)
                CaptureAndClose();
        }

        const int MaxTitleSize = 52;
        const int MinTitleSize = 12;

        static int FitFontSize(string name, Vector2 boxSize)
        {
            for (int size = MaxTitleSize; size > MinTitleSize; size -= 2)
            {
                Text.fontStyles[(int)GameFont.Medium].fontSize = size;
                Vector2 measured = Text.CalcSize(name);
                float wrappedHeight = Text.CalcHeight(name, boxSize.x);
                if (measured.x <= boxSize.x && wrappedHeight <= boxSize.y) return size;
            }
            return MinTitleSize;
        }

        void DrawMockup(Rect inRect)
        {
            const float w = 220f, btnH = 28f, gap = 8f, margin = 24f;
            float panelH = btnH * 2f + gap * 3f;
            Rect panel = new Rect(margin, inRect.height - margin - panelH, w, panelH);

            ThemeElementRenderer.DrawPanelBG(panel, v.WindowBG, v.ResolvedWindowTex, Color.black);

            Rect btn = new Rect(panel.x + gap, panel.y + gap, w - gap * 2f, btnH);
            Texture2D btnAtlas = v.ResolvedButtonTex ?? Widgets.ButtonBGAtlas;
            ThemeElementRenderer.DrawButtonBG(btn, v.ButtonBG, btnAtlas);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(btn, "Button");

            Rect barBtn = new Rect(panel.x + gap, btn.yMax + gap, w - gap * 2f, btnH);
            if (v.ResolvedBottomBarTex != null)
                ThemeElementRenderer.DrawButtonBG(barBtn, v.BottomBarBG, v.ResolvedBottomBarTex);
            else
                Widgets.DrawBoxSolid(barBtn, v.BottomBarBG);
            GUI.color = Color.white;
            Widgets.Label(barBtn, "Architect");
            Text.Anchor = TextAnchor.UpperLeft;

            DrawPalette(inRect, panel);
        }

        void DrawPalette(Rect inRect, Rect panel)
        {
            const float swatch = 28f, gap = 6f, margin = 24f;
            var colors = new System.Collections.Generic.List<Color> { v.accent, v.background };
            if (v.windowBG.HasValue) colors.Add(v.windowBG.Value);
            if (v.tabBG.HasValue) colors.Add(v.tabBG.Value);
            if (v.buttonBG.HasValue) colors.Add(v.buttonBG.Value);
            if (v.gizmoBG.HasValue) colors.Add(v.gizmoBG.Value);
            const int maxSwatches = 6;
            if (colors.Count > maxSwatches) colors.RemoveRange(maxSwatches, colors.Count - maxSwatches);

            float totalW = colors.Count * swatch + (colors.Count - 1) * gap;
            float startX = inRect.width - margin - totalW;
            float y = panel.yMax - swatch;
            for (int i = 0; i < colors.Count; i++)
            {
                Rect r = new Rect(startX + i * (swatch + gap), y, swatch, swatch);
                Widgets.DrawBoxSolid(r, colors[i]);
                GUI.color = Color.black;
                Widgets.DrawBox(r);
                GUI.color = Color.white;
            }
        }

        void CaptureAndClose()
        {
            Rect r = windowRect;
            int screenW = UI.screenWidth;
            int screenH = UI.screenHeight;
            int x = Mathf.RoundToInt(r.x);
            int y = Mathf.RoundToInt(screenH - r.y - r.height);
            int w = Mathf.RoundToInt(r.width);
            int h = Mathf.RoundToInt(r.height);
            x = Mathf.Clamp(x, 0, Mathf.Max(0, screenW - 1));
            y = Mathf.Clamp(y, 0, Mathf.Max(0, screenH - 1));
            w = Mathf.Clamp(w, 1, screenW - x);
            h = Mathf.Clamp(h, 1, screenH - y);

            var cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
            cropped.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
            cropped.Apply();

            onCaptured?.Invoke(cropped);
            Close(false);
        }
    }
}