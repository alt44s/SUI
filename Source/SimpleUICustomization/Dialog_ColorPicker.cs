using System;
using System.Globalization;
using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    [StaticConstructorOnStartup]
    public class Dialog_ColorPicker : Window
    {
        public enum PreviewKind { None, WindowBG, MenuSectionBG, TabBG, ButtonBG, BottomBarBG, GizmoBG, Accent, Checkbox, Slider, Scrollbar, FillableBar, FillableBarBackdrop }

        static readonly System.Collections.Generic.List<Color> recentSwatches = new System.Collections.Generic.List<Color>();
        const int MaxRecentSwatches = 12;

        static void PushRecent(Color c)
        {
            recentSwatches.RemoveAll(x => x == c);
            recentSwatches.Insert(0, c);
            if (recentSwatches.Count > MaxRecentSwatches) recentSwatches.RemoveRange(MaxRecentSwatches, recentSwatches.Count - MaxRecentSwatches);
        }

        Color color;
        readonly Color oldColor;
        readonly Action<Color> onSelect;
        readonly PreviewKind previewKind;
        readonly Texture2D previewTex;
        bool wheelDragging;
        string hexBuffer;
        string[] rgbBuffers = new string[3];
        float hue, sat, val;

        public override Vector2 InitialSize => new Vector2(previewKind == PreviewKind.None ? 360f : 480f, recentSwatches.Count > 0 ? 500f : 468f);

        public Dialog_ColorPicker(Color initial, Action<Color> onSelect, PreviewKind previewKind = PreviewKind.None, Texture2D previewTex = null)
        {
            color = initial;
            oldColor = initial;
            this.onSelect = onSelect;
            this.previewKind = previewKind;
            this.previewTex = previewTex;
            hexBuffer = ColorToHex(color);
            Color.RGBToHSV(color, out hue, out sat, out val);
            SyncRgbBuffers();
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        void SyncRgbBuffers()
        {
            Color32 c32 = color;
            rgbBuffers[0] = c32.r.ToString();
            rgbBuffers[1] = c32.g.ToString();
            rgbBuffers[2] = c32.b.ToString();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Color colorBefore = color;

            Rect wheelRect = new Rect(inRect.x, inRect.y, 200f, 200f);
            if (wheelDragging && !Input.GetMouseButton(0)) wheelDragging = false;
            Color wheelColor = Color.HSVToRGB(hue, sat, 1f);
            wheelColor.a = color.a;
            Widgets.HSVColorWheel(wheelRect, ref wheelColor, ref wheelDragging, 1f);
            Color.RGBToHSV(wheelColor, out hue, out sat, out _);
            color = Color.HSVToRGB(hue, sat, val);
            color.a = wheelColor.a;

            if (previewKind != PreviewKind.None)
            {
                Rect previewRect = new Rect(wheelRect.xMax + 12f, inRect.y, inRect.width - wheelRect.width - 12f, 200f);
                DrawPreview(previewRect);
            }

            Rect valRow = new Rect(inRect.x, wheelRect.yMax + 8f, inRect.width, 24f);
            Widgets.Label(valRow.LeftPartPixels(70f), "SUIC_Darkness".Translate());
            val = 1f - Widgets.HorizontalSlider(valRow.RightPartPixels(valRow.width - 74f), 1f - val, 0f, 1f);
            color = Color.HSVToRGB(hue, sat, val);
            color.a = wheelColor.a;

            Rect alphaRow = new Rect(inRect.x, valRow.yMax + 4f, inRect.width, 24f);
            Widgets.Label(alphaRow.LeftPartPixels(70f), "SUIC_Alpha".Translate());
            color.a = Widgets.HorizontalSlider(alphaRow.RightPartPixels(alphaRow.width - 74f), color.a, 0f, 1f);

            Rect rgbRow = new Rect(inRect.x, alphaRow.yMax + 4f, inRect.width, 24f);
            float fieldW = (rgbRow.width - 8f) / 3f;
            if (RgbField(new Rect(rgbRow.x, rgbRow.y, fieldW, rgbRow.height), ref rgbBuffers[0], out byte r)) SetChannel(0, r);
            if (RgbField(new Rect(rgbRow.x + fieldW + 4f, rgbRow.y, fieldW, rgbRow.height), ref rgbBuffers[1], out byte g)) SetChannel(1, g);
            if (RgbField(new Rect(rgbRow.x + (fieldW + 4f) * 2f, rgbRow.y, fieldW, rgbRow.height), ref rgbBuffers[2], out byte b)) SetChannel(2, b);

            Rect readback = new Rect(inRect.x, rgbRow.yMax + 8f, inRect.width, 24f);
            Widgets.DrawBoxSolid(readback.LeftHalf().ContractedBy(2f), color);
            Widgets.DrawBoxSolid(readback.RightHalf().ContractedBy(2f), oldColor);

            Rect hexRow = new Rect(inRect.x, readback.yMax + 8f, inRect.width, 28f);
            string prevHex = hexBuffer;
            hexBuffer = Widgets.TextField(hexRow.LeftPartPixels(hexRow.width - 174f), hexBuffer);
            if (hexBuffer != prevHex && TryParseHex(hexBuffer, out var parsed))
            {
                color = parsed;
                Color.RGBToHSV(color, out hue, out sat, out val);
            }
            if (Widgets.ButtonText(new Rect(hexRow.xMax - 168f, hexRow.y, 82f, hexRow.height), "SUIC_Copy".Translate()))
                GUIUtility.systemCopyBuffer = hexBuffer;
            if (Widgets.ButtonText(new Rect(hexRow.xMax - 84f, hexRow.y, 84f, hexRow.height), "SUIC_Paste".Translate()))
            {
                if (TryParseHex(GUIUtility.systemCopyBuffer, out var pasted))
                {
                    color = pasted;
                    Color.RGBToHSV(color, out hue, out sat, out val);
                }
            }

            if (color != colorBefore)
            {
                hexBuffer = ColorToHex(color);
                SyncRgbBuffers();
            }

            Rect btnRow = new Rect(inRect.x, inRect.yMax - 32f, inRect.width, 32f);

            if (recentSwatches.Count > 0)
            {
                Rect swatchRow = new Rect(inRect.x, hexRow.yMax + 8f, inRect.width, 24f);
                float sw = 20f, gap = 4f;
                for (int i = 0; i < recentSwatches.Count; i++)
                {
                    Rect swatchRect = new Rect(swatchRow.x + i * (sw + gap), swatchRow.y, sw, sw);
                    if (swatchRect.xMax > swatchRow.xMax) break;
                    Widgets.DrawBoxSolid(swatchRect, recentSwatches[i]);
                    GUI.color = Color.black;
                    Widgets.DrawBox(swatchRect);
                    GUI.color = Color.white;
                    if (Widgets.ButtonInvisible(swatchRect))
                    {
                        color = recentSwatches[i];
                        Color.RGBToHSV(color, out hue, out sat, out val);
                        hexBuffer = ColorToHex(color);
                        SyncRgbBuffers();
                    }
                }
            }

            if (Widgets.ButtonText(btnRow.LeftHalf().ContractedBy(2f, 0f), "SUIC_Cancel".Translate()))
                Close();
            if (Widgets.ButtonText(btnRow.RightHalf().ContractedBy(2f, 0f), "SUIC_Accept".Translate()))
            {
                PushRecent(color);
                onSelect?.Invoke(color);
                Close();
            }
        }

        void SetChannel(int index, byte value)
        {
            Color32 c32 = color;
            if (index == 0) c32.r = value;
            else if (index == 1) c32.g = value;
            else c32.b = value;
            color = c32;
            Color.RGBToHSV(color, out hue, out sat, out val);
        }

        static bool RgbField(Rect rect, ref string buffer, out byte value)
        {
            value = 0;
            string prev = buffer;
            buffer = Widgets.TextField(rect, buffer);
            if (buffer == prev) return false;
            if (!byte.TryParse(buffer, out value)) return false;
            return true;
        }

        static readonly Color PreviewBoxBG = new Color(0.5f, 0.5f, 0.5f);

        static readonly Texture2D VanillaGizmoBG = ContentFinder<Texture2D>.Get("UI/Widgets/DesButBG", false);

        void DrawPreview(Rect rect)
        {
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 18f), "SUIC_Preview".Translate());
            Rect box = new Rect(rect.x, rect.y + 20f, rect.width, rect.height - 20f);
            Widgets.DrawBoxSolid(box, PreviewBoxBG);
            Widgets.DrawBox(box);
            box = box.ContractedBy(4f);
            Texture2D atlas = previewTex != null ? previewTex : Widgets.ButtonBGAtlas;
            switch (previewKind)
            {
                case PreviewKind.WindowBG:
                    ThemeElementRenderer.DrawWindowBG(box, color, previewTex, Color.grey);
                    break;
                case PreviewKind.MenuSectionBG:
                    ThemeElementRenderer.DrawMenuSectionBG(box, color, previewTex, Color.grey);
                    break;
                case PreviewKind.TabBG:
                    ThemeElementRenderer.DrawTabShape(null, box, color, atlas);
                    break;
                case PreviewKind.ButtonBG:
                case PreviewKind.Accent:
                    {
                        Rect btn = CenteredRect(box, Mathf.Min(box.width, 120f), 32f);
                        ThemeElementRenderer.DrawButtonBG(btn, color, atlas);
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.Label(btn, "SUIC_Sample".Translate());
                        Text.Anchor = TextAnchor.UpperLeft;
                        break;
                    }
                case PreviewKind.BottomBarBG:
                    {
                        Rect btn = CenteredRect(box, Mathf.Min(box.width, 160f), 32f);
                        if (previewTex != null)
                            ThemeElementRenderer.DrawButtonBG(btn, color, previewTex);
                        else
                            Widgets.DrawBoxSolid(btn, color);
                        Text.Anchor = TextAnchor.MiddleLeft;
                        Widgets.Label(new Rect(btn.x + 8f, btn.y, btn.width - 8f, btn.height), "SUIC_Sample".Translate());
                        Text.Anchor = TextAnchor.UpperLeft;
                        break;
                    }
                case PreviewKind.GizmoBG:
                    {
                        Rect btn = CenteredRect(box, 75f, 75f);
                        Texture2D frame = previewTex != null ? previewTex : VanillaGizmoBG;
                        Color prevG = GUI.color;
                        GUI.color = color;
                        if (frame != null) GUI.DrawTexture(btn, frame);
                        GUI.color = prevG;
                        break;
                    }
                case PreviewKind.Checkbox:
                    {
                        Rect chk = CenteredRect(box, 24f, 24f);
                        Texture2D chkTex = previewTex != null ? previewTex : (SimpleUICustomizationMod.Settings.live.ResolvedCheckboxOnTex ?? Widgets.CheckboxOnTex);
                        Color prevC = GUI.color;
                        GUI.color = color;
                        GUI.DrawTexture(chk, chkTex);
                        GUI.color = prevC;
                        break;
                    }
                case PreviewKind.Slider:
                    {
                        Rect sl = CenteredRect(box, Mathf.Min(box.width, 160f), 24f);
                        float dummy = 0.6f;
                        ThemePatches.Patch_HorizontalSlider.PreviewOverride = color;
                        Widgets.HorizontalSlider(sl, dummy, 0f, 1f);
                        ThemePatches.Patch_HorizontalSlider.PreviewOverride = null;
                        break;
                    }
                case PreviewKind.Scrollbar:
                    {
                        Rect track = CenteredRect(box, 16f, Mathf.Min(box.height, 140f));
                        Color prevS = GUI.color;
                        GUI.color = color;
                        if (previewTex != null) GUI.DrawTexture(track, previewTex);
                        else GUI.VerticalScrollbar(track, 0f, 1f, 1f, 0f);
                        GUI.color = prevS;
                        break;
                    }
                case PreviewKind.FillableBar:
                    {
                        Rect bar = CenteredRect(box, Mathf.Min(box.width, 160f), 20f);
                        Texture2D fillTex = previewTex != null ? previewTex : Widgets.BarFullTexHor;
                        ThemePatches.Patch_FillableBar.FillPreviewOverride = color;
                        Widgets.FillableBar(bar, 0.65f, fillTex);
                        ThemePatches.Patch_FillableBar.FillPreviewOverride = null;
                        break;
                    }
                case PreviewKind.FillableBarBackdrop:
                    {
                        Rect bar = CenteredRect(box, Mathf.Min(box.width, 160f), 20f);
                        Texture2D bgTex = previewTex != null ? previewTex : BaseContent.BlackTex;
                        ThemePatches.Patch_FillableBar.BackdropPreviewOverride = color;
                        ThemePatches.Patch_FillableBar.FillPreviewOverride = Color.clear;
                        Widgets.FillableBar(bar, 0f, bgTex, bgTex, true);
                        ThemePatches.Patch_FillableBar.BackdropPreviewOverride = null;
                        ThemePatches.Patch_FillableBar.FillPreviewOverride = null;
                        break;
                    }
            }
        }

        static Rect CenteredRect(Rect box, float w, float h)
        {
            return new Rect(box.x + (box.width - w) / 2f, box.y + (box.height - h) / 2f, w, h);
        }

        static string ColorToHex(Color c)
        {
            Color32 c32 = c;
            return $"{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
        }

        static bool TryParseHex(string hex, out Color color)
        {
            color = Color.white;
            if (hex.NullOrEmpty()) return false;
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            if (hex.Length != 6 && hex.Length != 8) return false;
            try
            {
                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                int a = hex.Length == 8 ? int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber) : 255;
                color = new Color32((byte)r, (byte)g, (byte)b, (byte)a);
                return true;
            }
            catch { return false; }
        }
    }
}