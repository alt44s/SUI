using System;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleUICustomization
{
    public class Dialog_TextEntry : Window
    {
        string name, author, desc;
        readonly string label, note;
        readonly Action<string, string, string> onConfirm;
        readonly bool resultMode;

        public override Vector2 InitialSize => resultMode
            ? new Vector2(520f, note.NullOrEmpty() ? 190f : 230f)
            : new Vector2(400f, 260f);

        public Dialog_TextEntry(string initialName, string initialAuthor, string initialDesc, Action<string, string, string> onConfirm)
        {
            name = initialName;
            author = initialAuthor;
            desc = initialDesc;
            this.onConfirm = onConfirm;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public Dialog_TextEntry(string label, string path, string note)
        {
            this.label = label;
            this.note = note;
            name = path;
            resultMode = true;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (resultMode)
            {
                Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 24f), label);
                Widgets.TextField(new Rect(inRect.x, inRect.y + 28f, inRect.width, 28f), name);

                Rect actionRow = new Rect(inRect.x, inRect.y + 60f, inRect.width, 30f);
                if (Widgets.ButtonText(actionRow.LeftHalf().ContractedBy(2f, 0f), "SUIC_CopyPath".Translate()))
                    GUIUtility.systemCopyBuffer = name;
                if (Widgets.ButtonText(actionRow.RightHalf().ContractedBy(2f, 0f), "SUIC_OpenFolder".Translate()))
                    OpenFolder(name);

                if (!note.NullOrEmpty())
                    Widgets.Label(new Rect(inRect.x, actionRow.yMax + 8f, inRect.width, inRect.yMax - actionRow.yMax - 48f), note);

                if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 32f, inRect.width, 32f), "SUIC_Close".Translate()))
                    Close();
                return;
            }

            float y = inRect.y;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f), "SUIC_ThemeNameLabel".Translate());
            y += 24f;
            name = Widgets.TextField(new Rect(inRect.x, y, inRect.width, 28f), name);
            y += 36f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f), "SUIC_AuthorLabel".Translate());
            y += 24f;
            author = Widgets.TextField(new Rect(inRect.x, y, inRect.width, 28f), author);
            y += 36f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 22f), "SUIC_DescriptionLabel".Translate());
            y += 24f;
            desc = Widgets.TextField(new Rect(inRect.x, y, inRect.width, 28f), desc);
            y += 48f;

            Rect btnRow = new Rect(inRect.x, y, inRect.width, 32f);
            if (Widgets.ButtonText(btnRow.LeftHalf().ContractedBy(2f, 0f), "SUIC_Cancel".Translate()))
                Close();
            if (Widgets.ButtonText(btnRow.RightHalf().ContractedBy(2f, 0f), "SUIC_OK".Translate()))
            {
                if (name.NullOrEmpty())
                {
                    Messages.Message("SUIC_PleaseEnterThemeName".Translate(), MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    onConfirm?.Invoke(name, author, desc);
                    Close();
                }
            }
        }

        static void OpenFolder(string path)
        {
            try
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                    Process.Start("explorer.exe", "\"" + path + "\"");
                else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
                    Process.Start("open", "\"" + path + "\"");
                else
                    Process.Start("xdg-open", "\"" + path + "\"");
            }
            catch (Exception e)
            {
                Log.Warning("[SimpleUICustomization] Failed to open folder: " + e.Message);
                GUIUtility.systemCopyBuffer = path;
                Messages.Message("SUIC_FailedToOpenFolder".Translate(), MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}