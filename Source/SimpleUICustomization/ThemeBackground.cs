using UnityEngine;

namespace SimpleUICustomization
{
    [Verse.StaticConstructorOnStartup]
    public static class ThemeBackground
    {
        static int index;
        static Texture2D from, to;
        static float fadeT;
        static float nextSwitchTime;

        public static void Draw(Rect screenRect, ThemeValues v)
        {
            var raw = v.ResolvedBackgrounds;
            var list = new System.Collections.Generic.List<Texture2D>(raw.Count);
            for (int i = 0; i < raw.Count; i++)
                if (raw[i] != null) list.Add(raw[i]);
            if (list.Count == 0) return;

            if (index >= list.Count) index = 0;
            if (to == null) to = list[index];
            float fadeSeconds = Mathf.Max(0.1f, v.bgFadeSeconds);

            if (pendingJump != null)
            {
                var tex = ThemeLoader.GetTexture(pendingJump);
                pendingJump = null;
                int jumpIndex = list.IndexOf(tex);
                if (tex != null && jumpIndex >= 0 && tex != to)
                {
                    from = to;
                    to = tex;
                    index = jumpIndex;
                    fadeT = fadeSeconds;
                    nextSwitchTime = Time.unscaledTime + Mathf.Max(1f, v.bgCycleSeconds);
                }
            }
            else if (v.bgCycle && list.Count > 1 && Time.unscaledTime >= nextSwitchTime && fadeT <= 0f)
            {
                from = to;
                index = (index + 1) % list.Count;
                to = list[index];
                fadeT = fadeSeconds;
            }

            if (fadeT > 0f)
            {
                fadeT -= Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(fadeT / fadeSeconds);
                DrawFitted(screenRect, from, 1f);
                DrawFitted(screenRect, to, a);
                if (fadeT <= 0f) nextSwitchTime = Time.unscaledTime + Mathf.Max(1f, v.bgCycleSeconds);
                return;
            }
            if (nextSwitchTime <= 0f) nextSwitchTime = Time.unscaledTime + Mathf.Max(1f, v.bgCycleSeconds);

            DrawFitted(screenRect, to, 1f);
        }

        static string pendingJump;

        public static void JumpTo(string path) => pendingJump = path;

        static void DrawFitted(Rect screenRect, Texture2D tex, float alpha)
        {
            if (tex == null) return;
            Vector2 size = new Vector2(tex.width, tex.height);
            Rect rect;
            if (screenRect.width > screenRect.height * (size.x / size.y))
            {
                float w = screenRect.width;
                float h = w * (size.y / size.x);
                rect = new Rect(screenRect.x, screenRect.y + screenRect.height / 2f - h / 2f, w, h);
            }
            else
            {
                float h = screenRect.height;
                float w = h * (size.x / size.y);
                rect = new Rect(screenRect.x + screenRect.width / 2f - w / 2f, screenRect.y, w, h);
            }
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
            GUI.color = prev;
        }

        public static void Reset()
        {
            index = 0;
            from = to = null;
            fadeT = 0f;
            nextSwitchTime = 0f;
            pendingJump = null;
        }
    }
}