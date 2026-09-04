using LudeonTK;
using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    public static class ThemeElementRenderer
    {
        public static void DrawPanelBG(Rect rect, Color bg, Texture2D tex, Color borderColor)
        {
            Color prev = GUI.color;
            if (tex == null || !tex)
            {
                Widgets.DrawBoxSolid(rect, bg);
                GUI.color = borderColor;
                Widgets.DrawBox(rect);
            }
            else
            {
                GUI.color = bg;
                Widgets.DrawAtlas(rect, tex);
            }
            GUI.color = prev;
        }

        public static void DrawWindowBG(Rect rect, Color bg, Texture2D customTex, Color borderColor, bool sliced = true, bool outline = true)
        {
            Color prev = GUI.color;
            if (customTex == null || !customTex) Widgets.DrawBoxSolid(rect, bg);
            else
            {
                GUI.color = bg;
                if (sliced) Widgets.DrawAtlas(rect, customTex);
                else GUI.DrawTexture(rect, customTex);
            }
            if (outline)
            {
                GUI.color = borderColor;
                Widgets.DrawBox(rect);
            }
            GUI.color = prev;
        }
        public static void DrawMenuSectionBG(Rect rect, Color bg, Texture2D tex, Color borderColor) => DrawPanelBG(rect, bg, tex, borderColor);

        public static void DrawButtonBG(Rect rect, Color tint, Texture2D atlas)
        {
            Color prev = GUI.color;
            GUI.color = tint;
            Widgets.DrawAtlas(rect, atlas);
            GUI.color = prev;
        }

        public static void DrawTabShape(TabRecord t, Rect rect, Color tint, Texture2D atlas)
        {
            Color prev = GUI.color;
            GUI.color = tint;
            Rect end1 = new Rect(rect) { width = 30f };
            Rect end2 = new Rect(rect) { width = 30f, x = rect.x + rect.width - 30f };
            Rect mid = new Rect(rect);
            mid.x += 30f;
            mid.width = Mathf.Max(0f, mid.width - 60f);
            mid.xMin = UIScaling.AdjustCoordToUIScalingFloor(mid.xMin);
            mid.xMax = UIScaling.AdjustCoordToUIScalingCeil(mid.xMax);
            Rect midUV = new Rect(30f, 0f, 4f, atlas.height).ToUVRect(new Vector2(atlas.width, atlas.height));
            Widgets.DrawTexturePart(end1, new Rect(0f, 0f, 15f / 32f, 1f), atlas);
            Widgets.DrawTexturePart(mid, midUV, atlas);
            Widgets.DrawTexturePart(end2, new Rect(17f / 32f, 0f, 15f / 32f, 1f), atlas);
            if (t == null || !t.Selected)
            {
                Rect underline = new Rect(rect) { y = rect.y + rect.height - 1f, height = 1f };
                Widgets.DrawTexturePart(underline, new Rect(0.5f, 0.01f, 0.01f, 0.01f), atlas);
            }
            GUI.color = prev;
        }

    }
}