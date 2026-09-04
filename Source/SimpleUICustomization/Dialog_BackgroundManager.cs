using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    public class Dialog_BackgroundManager : Window
    {
        readonly List<string> backgrounds;
        readonly System.Action onChanged;
        Vector2 scroll;

        const float ThumbW = 220f, ThumbH = 124f, Gap = 12f, XSize = 20f, DragThreshold = 6f;

        int dragIndex = -1;
        int dropIndex = -1;
        Vector2 dragStartMouse;
        bool dragCommitted;

        public override Vector2 InitialSize => new Vector2(780f, 620f);

        public Dialog_BackgroundManager(List<string> backgrounds, System.Action onChanged)
        {
            this.backgrounds = backgrounds;
            this.onChanged = onChanged;
            doCloseButton = true;
            doCloseX = true;
            forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            inRect.height -= CloseButSize.y + 10f;

            Rect addRow = new Rect(inRect.x, inRect.y, 200f, 32f);
            if (Widgets.ButtonText(addRow, "SUIC_AddBackground".Translate()))
            {
                Find.WindowStack.Add(new Dialog_Picker(path =>
                {
                    backgrounds.Add(path);
                    ThemeBackground.Reset();
                    onChanged?.Invoke();
                }, true));
            }
            Widgets.Label(new Rect(addRow.xMax + 12f, addRow.y + 6f, inRect.width - addRow.width - 12f, 24f),
                "SUIC_BackgroundCount".Translate(backgrounds.Count));

            Rect gridRect = new Rect(inRect.x, addRow.yMax + 12f, inRect.width, inRect.yMax - addRow.yMax - 12f);
            if (backgrounds.Count == 0)
            {
                Widgets.Label(gridRect, "SUIC_NoBackgroundsInRotation".Translate());
                return;
            }

            int perRow = Mathf.Max(1, Mathf.FloorToInt((gridRect.width + Gap) / (ThumbW + Gap)));
            int rows = Mathf.CeilToInt(backgrounds.Count / (float)perRow);
            Rect viewRect = new Rect(0, 0, gridRect.width - 16f, rows * (ThumbH + Gap));
            Widgets.BeginScrollView(gridRect, ref scroll, viewRect);

            int removeIndex = -1;
            int perRowCaptured = perRow;
            for (int i = 0; i < backgrounds.Count; i++)
            {
                if (dragCommitted && i == dragIndex) continue;

                int col = i % perRow;
                int row = i / perRow;
                Rect cell = new Rect(col * (ThumbW + Gap), row * (ThumbH + Gap), ThumbW, ThumbH);

                bool isDropTarget = dragCommitted && i == dropIndex;
                DrawThumb(cell, backgrounds[i], isDropTarget);

                Rect xRect = new Rect(cell.xMax - XSize - 2f, cell.y + 2f, XSize, XSize);
                if (Mouse.IsOver(cell) && !dragCommitted)
                    GUI.DrawTexture(xRect.ContractedBy(4f), TexButton.CloseXSmall);

                bool mouseDown = Event.current.type == EventType.MouseDown && Event.current.button == 0;
                if (Mouse.IsOver(xRect) && mouseDown)
                {
                    removeIndex = i;
                    Event.current.Use();
                }
                else if (Mouse.IsOver(cell) && mouseDown)
                {
                    dragIndex = i;
                    dragStartMouse = Event.current.mousePosition;
                    dragCommitted = false;
                    Event.current.Use();
                }
            }

            if (dragIndex >= 0)
            {
                if (!dragCommitted && (Event.current.mousePosition - dragStartMouse).magnitude > DragThreshold)
                    dragCommitted = true;

                if (dragCommitted)
                {
                    Vector2 m = Event.current.mousePosition;
                    dropIndex = Mathf.Clamp(Mathf.FloorToInt(m.y / (ThumbH + Gap)) * perRowCaptured + Mathf.FloorToInt(m.x / (ThumbW + Gap)), 0, backgrounds.Count - 1);
                    Rect ghost = new Rect(m.x - ThumbW / 2f, m.y - ThumbH / 2f, ThumbW, ThumbH);
                    DrawThumb(ghost, backgrounds[dragIndex], false);
                }

                bool mouseUp = Event.current.type == EventType.MouseUp && Event.current.button == 0;
                if (mouseUp)
                {
                    if (dragCommitted && dropIndex >= 0 && dropIndex != dragIndex)
                    {
                        string moved = backgrounds[dragIndex];
                        backgrounds.RemoveAt(dragIndex);
                        backgrounds.Insert(dropIndex, moved);
                        ThemeBackground.Reset();
                        onChanged?.Invoke();
                    }
                    else if (!dragCommitted)
                    {
                        ThemeBackground.JumpTo(backgrounds[dragIndex]);
                    }
                    dragIndex = -1;
                    dropIndex = -1;
                    dragCommitted = false;
                    Event.current.Use();
                }
            }

            Widgets.EndScrollView();

            if (removeIndex >= 0)
            {
                backgrounds.RemoveAt(removeIndex);
                ThemeBackground.Reset();
                onChanged?.Invoke();
            }
        }

        static void DrawThumb(Rect cell, string path, bool highlight)
        {
            var tex = ThemeLoader.GetTexture(path);
            Widgets.DrawBoxSolid(cell, new Color(0.1f, 0.1f, 0.1f));
            if (tex != null) Widgets.DrawTextureFitted(cell, tex, 1f);
            GUI.color = highlight ? Color.yellow : Color.white;
            Widgets.DrawBox(cell, highlight ? 2 : 1);
            GUI.color = Color.white;

            Rect labelRect = new Rect(cell.x, cell.yMax - 20f, cell.width, 20f);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(labelRect, BaseContent.WhiteTex);
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(labelRect.x + 4f, labelRect.y, labelRect.width - 8f, labelRect.height), BackgroundLabel(path));
            Text.Font = GameFont.Small;
        }

        static string BackgroundLabel(string path)
        {
            if (path.StartsWith(ThemeLoader.ExpansionPrefix))
            {
                var defName = path.Substring(ThemeLoader.ExpansionPrefix.Length);
                var exp = Verse.ModLister.AllExpansions.Find(e => e.defName == defName);
                return exp != null ? exp.label : defName;
            }
            string key = path.StartsWith(Dialog_Picker.GamePrefix) ? path.Substring(Dialog_Picker.GamePrefix.Length)
                : path.StartsWith(Dialog_Picker.FilePrefix) ? path.Substring(Dialog_Picker.FilePrefix.Length)
                : path;
            return System.IO.Path.GetFileNameWithoutExtension(key);
        }
    }
}