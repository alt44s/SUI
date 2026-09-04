using System.Collections.Generic;
using Verse;

namespace SimpleUICustomization
{
    public static class ThemeAudio
    {
        public static readonly string[] UISoundNames =
        {
            "Tick_High", "Tick_Low", "Tick_Tiny", "DragElement", "DropElement", "Crunch",
            "Click", "ClickReject", "CancelMode", "TabOpen", "TabClose",
            "Checkbox_TurnedOn", "Checkbox_TurnedOff", "RowTabSelect", "ArchitectCategorySelect", "ExecuteTrade",
            "FloatMenu_Open", "FloatMenu_Cancel", "DialogBoxAppear", "TutorMessageAppear", "TinyBell",
            "PageChange", "DragSlider", "DragGoto", "Lesson_Activated", "Lesson_Deactivated",
            "DraftOn", "DraftOff", "CommsWindow_Open", "CommsWindow_Close", "InfoCard_Open", "InfoCard_Close",
            "Quest_Accepted", "Quest_Succeded", "Quest_Concluded", "Quest_Failed",
            "Mouseover_Standard", "Mouseover_Category", "Mouseover_Command", "Mouseover_ButtonToggle", "Mouseover_Tab", "Mouseover_Thump",
            "ThingSelected", "MapSelected", "ColonistSelected", "ColonistOrdered",
            "LetterArrive", "LetterArrive_BadUrgent", "LetterArrive_BadUrgentBig", "LetterArrive_BadUrgentSmall", "LetterArrive_Good",
            "Message_NeutralEvent", "Message_PositiveEvent", "Message_NegativeEvent", "Message_ThreatBig", "Message_ThreatSmall",
            "Message_PawnDeath", "Message_NegativeHealthEvent", "Message_TaskCompletion",
            "ResearchStart", "GameStartSting"
        };

        static readonly Dictionary<string, SoundDef> cache = new Dictionary<string, SoundDef>();

        public static SoundDef Get(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return null;
            if (cache.TryGetValue(defName, out var def)) return def;
            def = DefDatabase<SoundDef>.GetNamedSilentFail(defName);
            if (def != null && (def.sustain || !HasOnCameraSubSound(def))) def = null;
            cache[defName] = def;
            return def;
        }

        static bool HasOnCameraSubSound(SoundDef d)
        {
            foreach (var s in d.subSounds)
                if (s.onCamera) return true;
            return false;
        }

        public static string[] AllSoundDefNames()
        {
            var list = new List<string>();
            foreach (var d in DefDatabase<SoundDef>.AllDefsListForReading)
            {
                if (d.sustain || d.isUndefined || d.subSounds.NullOrEmpty()) continue;
                bool hasOnCamera = false;
                foreach (var s in d.subSounds)
                    if (s.onCamera) { hasOnCamera = true; break; }
                if (hasOnCamera) list.Add(d.defName);
            }
            list.Sort();
            return list.ToArray();
        }
    }
}