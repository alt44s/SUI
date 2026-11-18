using HarmonyLib;
using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Verse.Widgets;
using Input = UnityEngine.Input;

namespace SimpleUICustomization
{
    [StaticConstructorOnStartup]
    public static class ThemePatches
    {
        static ThemePatches()
        {
            try
            {
                new Harmony("alt4s.simpleuicustomization").PatchAll();
            }
            catch (System.Exception e)
            {
                Log.Error("[SimpleUICustomization] Failed to apply Harmony patches: " + e);
            }
        }

        static ThemeValues V => SimpleUICustomizationMod.Settings.live;
        static bool On => SimpleUICustomizationMod.Settings.enabled;
        static bool SoundsOn => V.soundsEnabled;
        static bool BackgroundsOn => V.HasBackgrounds;

        static SoundDef SoundOrNull(string defName) => string.IsNullOrEmpty(defName) ? null : ThemeAudio.Get(defName);

        static Color HoverClickTint(Rect rect, Color baseColor)
        {
            if (!Mouse.IsOver(rect)) return baseColor;
            return Input.GetMouseButton(0) ? V.ClickColor : V.HoverColor;
        }

        static int themedScopeDepth;
        static void MarkThemedScope(bool entering) => themedScopeDepth += entering ? 1 : -1;

        static readonly Texture2D CachedButtonBarTex = SafeField<Texture2D>(typeof(Widgets), "ButtonBarTex", Widgets.ButtonBGAtlas);
        static readonly Color CachedMenuSectionBGBorderColor = SafeField<Color>(typeof(Widgets), "MenuSectionBGBorderColor", Color.black);
        static readonly Color CachedWindowBGBorderColor = SafeField<Color>(typeof(Widgets), "WindowBGBorderColor", Color.black);

        static T SafeField<T>(System.Type type, string field, T fallback = default)
        {
            try { return Traverse.Create(type).Field(field).GetValue<T>(); }
            catch { Log.Warning($"[SimpleUICustomization] Couldn't reflect {type.Name}.{field}, using fallback."); return fallback; }
        }

        [HarmonyPatch]
        static class Patch_CommandBGTextures
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.PropertyGetter(typeof(Command), nameof(Command.BGTexture));
                yield return AccessTools.PropertyGetter(typeof(Command), nameof(Command.BGTextureShrunk));
            }

            static void Postfix(ref Texture2D __result)
            {
                if (!On || !V.ElemOn(ThemeValues.Elem.Gizmo)) return;
                __result = V.ResolvedGizmoTex ?? __result;
            }
        }

        [HarmonyPatch(typeof(Command), "GizmoOnGUIInt")]
        static class Patch_CommandGizmoOnGUIInt
        {
            static readonly MethodInfo DrawTexMat = AccessTools.Method(typeof(GenUI), nameof(GenUI.DrawTextureWithMaterial));

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var ins in instructions)
                {
                    if (DrawTexMat != null && ins.Calls(DrawTexMat))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_2);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_CommandGizmoOnGUIInt), nameof(TintForGizmo)));
                    }
                    yield return ins;
                }
            }

            static void TintForGizmo(Rect butRect, GizmoRenderParms parms)
            {
                if (!On || !V.ElemOn(ThemeValues.Elem.Gizmo) || parms.lowLight)
                {
                    GUI.color = Color.white;
                    return;
                }
                GUI.color = HoverClickTint(butRect, V.GizmoBG);
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawButtonGraphic))]
        static class Patch_DrawButtonGraphic
        {
            static bool Prefix(Rect rect)
            {
                if (!On || !V.ElemOn(ThemeValues.Elem.Button)) return true;

                Texture2D atlas = V.ResolvedButtonTex ?? Widgets.ButtonBGAtlas;
                ThemeElementRenderer.DrawButtonBG(rect, HoverClickTint(rect, V.ButtonBG), atlas);
                return false;
            }
        }

        [HarmonyPatch(typeof(Widgets), "ButtonTextWorker")]
        static class Patch_ButtonTextWorker
        {
            static void Prefix(bool drawBackground, ref Color textColor)
            {
                if (!On || drawBackground || themedScopeDepth == 0) return;
                if (textColor == Widgets.NormalOptionColor) textColor = V.accent;
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.Label), new[] { typeof(Rect), typeof(string) })]
        static class Patch_LabelTextColor
        {
            static void Prefix(out Color __state)
            {
                __state = GUI.color;
                if (On && V.textColor.HasValue && GUI.color == Color.white)
                    GUI.color = V.textColor.Value;
            }

            static void Postfix(Color __state) => GUI.color = __state;
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.CheckboxDraw))]
        static class Patch_CheckboxDraw
        {
            static bool Prefix(float x, float y, bool active, bool disabled, float size, Texture2D texChecked, Texture2D texUnchecked)
            {
                if (!On || !V.ElemOn(ThemeValues.Elem.Checkbox)) return true;
                var tex = active ? (V.ResolvedCheckboxOnTex ?? texChecked ?? Widgets.CheckboxOnTex)
                                  : (V.ResolvedCheckboxOffTex ?? texUnchecked ?? Widgets.CheckboxOffTex);
                Color prev = GUI.color;
                if (disabled) GUI.color = Widgets.InactiveColor;
                else GUI.color = V.CheckboxBG;
                GUI.DrawTexture(new Rect(x, y, size, size), tex);
                GUI.color = prev;
                return false;
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.CheckboxMulti))]
        static class Patch_CheckboxMulti
        {
            static readonly FieldInfo PaintingField = AccessTools.Field(typeof(Widgets), "checkboxPainting");
            static readonly FieldInfo PaintingStateField = AccessTools.Field(typeof(Widgets), "checkboxPaintingState");

            static bool Prefix(Rect rect, MultiCheckboxState state, bool paintable, ref MultiCheckboxState __result)
            {
                if (!On || !V.ElemOn(ThemeValues.Elem.Checkbox)) return true;
                Texture2D tex;
                if (state == MultiCheckboxState.On) tex = V.ResolvedCheckboxOnTex ?? Widgets.CheckboxOnTex;
                else if (state == MultiCheckboxState.Off) tex = V.ResolvedCheckboxOffTex ?? Widgets.CheckboxOffTex;
                else tex = V.ResolvedCheckboxPartialTex ?? Widgets.CheckboxPartialTex;
                MouseoverSounds.DoRegion(rect);
                MultiCheckboxState flipped = state != MultiCheckboxState.Off ? MultiCheckboxState.Off : MultiCheckboxState.On;
                bool changed = false;
                Color prev = GUI.color;
                GUI.color = Mouse.IsOver(rect) ? V.HoverColor : V.CheckboxBG;
                GUI.DrawTexture(rect, tex);
                GUI.color = prev;
                var drag = Widgets.ButtonInvisibleDraggable(rect);
                if (paintable && drag == DraggableResult.Dragged)
                {
                    PaintingField.SetValue(null, true);
                    PaintingStateField.SetValue(null, flipped == MultiCheckboxState.On);
                    changed = true;
                }
                else if (drag == DraggableResult.Pressed || drag == DraggableResult.DraggedThenPressed)
                {
                    changed = true;
                }
                else if (paintable && (bool)PaintingField.GetValue(null) && Mouse.IsOver(rect))
                {
                    flipped = (bool)PaintingStateField.GetValue(null) ? MultiCheckboxState.On : MultiCheckboxState.Off;
                    if (state != flipped) changed = true;
                }
                if (changed)
                {
                    (flipped == MultiCheckboxState.On ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff).PlayOneShotOnCamera();
                    __result = flipped;
                }
                else __result = state;
                return false;
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.HorizontalSlider), new[] { typeof(Rect), typeof(float), typeof(float), typeof(float), typeof(bool), typeof(string), typeof(string), typeof(string), typeof(float) })]
        public static class Patch_HorizontalSlider
        {
            static readonly FieldInfo RangeControlTextColorField = AccessTools.Field(typeof(Widgets), "RangeControlTextColor");
            static readonly FieldInfo SliderHandleField = AccessTools.Field(typeof(Widgets), "SliderHandle");
            static readonly MethodInfo DrawTextureMethod = AccessTools.Method(typeof(GUI), nameof(GUI.DrawTexture), new[] { typeof(Rect), typeof(Texture) });

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var list = new List<CodeInstruction>(instructions);
                for (int i = 0; i < list.Count; i++)
                {
                    var ins = list[i];
                    if (RangeControlTextColorField != null && ins.opcode == OpCodes.Ldsfld && (FieldInfo)ins.operand == RangeControlTextColorField)
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_HorizontalSlider), nameof(RailColor)));
                        continue;
                    }
                    if (SliderHandleField != null && ins.opcode == OpCodes.Ldsfld && (FieldInfo)ins.operand == SliderHandleField
                        && DrawTextureMethod != null && i + 1 < list.Count && list[i + 1].Calls(DrawTextureMethod))
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_HorizontalSlider), nameof(SetHandleColor)));
                        yield return ins;
                        yield return list[++i];
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_HorizontalSlider), nameof(ResetColor)));
                        continue;
                    }
                    yield return ins;
                }
            }

            static Color RailColor() => PreviewOverride ?? (On && V.ElemOn(ThemeValues.Elem.Slider) ? V.SliderBG : (Color)RangeControlTextColorField.GetValue(null));
            static void SetHandleColor() => GUI.color = PreviewOverride ?? (On && V.ElemOn(ThemeValues.Elem.Slider) ? V.SliderBG : Color.white);
            static void ResetColor() => GUI.color = Color.white;

            public static Color? PreviewOverride;
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.FillableBar), new[] { typeof(Rect), typeof(float), typeof(Texture2D), typeof(Texture2D), typeof(bool) })]
        [StaticConstructorOnStartup]
        public static class Patch_FillableBar
        {
            public static Color? FillPreviewOverride;
            public static Color? BackdropPreviewOverride;

            static readonly Texture2D CooldownBarTex = SafeField<Texture2D>(typeof(Command_Ability), "cooldownBarTex");
            static readonly Color CooldownBarColor = CooldownBarTex != null ? CooldownBarTex.GetPixel(0, 0) : Color.white;

            static bool Prefix(Rect rect, float fillPercent, ref Texture2D fillTex, ref Texture2D bgTex, bool doBorder, ref Rect __result)
            {
                // The colonist bar's name-label health fill has its own element,
                // so it can be left vanilla or styled apart from other bars.
                bool isColonistHealth = Patch_ColonistBarDrawColonist.InColonistBar && fillTex == GenMapUI.OverlayHealthTex;
                bool elemOn = On && V.ElemOn(isColonistHealth ? ThemeValues.Elem.ColonistBarHealth : ThemeValues.Elem.FillableBar);
                if (!elemOn && FillPreviewOverride == null && BackdropPreviewOverride == null) return true;
                bool isCooldownBar = elemOn && fillTex == CooldownBarTex;
                bool hadBackdrop = bgTex != null;
                if (elemOn)
                {
                    fillTex = V.ResolvedFillableBarFillTex ?? BaseContent.WhiteTex;
                    if (hadBackdrop) bgTex = V.ResolvedFillableBarBGTex ?? BaseContent.WhiteTex;
                }
                // Vanilla asks for no border on the health fill; honour that there.
                doBorder = isColonistHealth ? doBorder : hadBackdrop && rect.height > 15f && rect.width > 20f;
                Color backdropColor = BackdropPreviewOverride ?? (elemOn ? (isColonistHealth ? V.ColonistBarHealthBackdropBG : V.FillableBarBackdropBG) : Color.white);
                if (doBorder)
                {
                    Color prevBorder = GUI.color;
                    GUI.color = elemOn || BackdropPreviewOverride != null ? backdropColor : Color.black;
                    GUI.DrawTexture(rect, BaseContent.WhiteTex);
                    GUI.color = prevBorder;
                    rect = rect.ContractedBy(3f);
                }
                if (bgTex != null)
                {
                    Color prevBg = GUI.color;
                    GUI.color = backdropColor;
                    GUI.DrawTexture(rect, bgTex);
                    GUI.color = prevBg;
                }
                __result = rect;
                rect.width *= fillPercent;
                Color prev = GUI.color;
                Color fillColor = FillPreviewOverride ?? (elemOn ? (isColonistHealth ? V.ColonistBarHealthBG : V.FillableBarBG) : Color.white);
                if (isCooldownBar && V.ResolvedFillableBarFillTex == null && FillPreviewOverride == null)
                    fillColor.a *= CooldownBarColor.a;
                GUI.color = fillColor;
                GUI.DrawTexture(rect, fillTex);
                GUI.color = prev;
                return false;
            }
        }

        /// <summary>Flags draws inside the colonist bar, so Patch_FillableBar can spot its health fill.</summary>
        [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
        public static class Patch_ColonistBarDrawColonist
        {
            // Main thread only: IMGUI drawing never runs elsewhere.
            public static bool InColonistBar;

            static void Prefix() => InColonistBar = true;

            // Finalizer, not postfix, so a throw while drawing never leaves the flag set.
            static void Finalizer() => InColonistBar = false;
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.TextField), new[] { typeof(Rect), typeof(string) })]
        static class Patch_TextField
        {
            static void Prefix(Rect rect)
            {
                if (!On || !V.ElemOn(ThemeValues.Elem.TextField)) return;
                var tex = V.ResolvedTextFieldTex;
                if (tex != null)
                {
                    Color prev = GUI.color;
                    GUI.color = V.TextFieldBG;
                    GUI.DrawTexture(rect, tex);
                    GUI.color = prev;
                }
                else Widgets.DrawBoxSolid(rect, V.TextFieldBG);
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawWindowBackground), new[] { typeof(Rect) })]
        static class Patch_DrawWindowBackground
        {
            static bool Prefix(Rect rect)
            {
                if (!On || !V.ElemOn(ThemeValues.Elem.Window)) return true;
                ThemeElementRenderer.DrawWindowBG(rect, V.WindowBG, V.ResolvedWindowTex, CachedWindowBGBorderColor, V.windowBGSliced, V.windowBGOutline);
                return false;
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawMenuSection))]
        static class Patch_DrawMenuSection
        {
            static bool Prefix(Rect rect)
            {
                if (!On || !V.ElemOn(ThemeValues.Elem.MenuSection)) return true;
                ThemeElementRenderer.DrawMenuSectionBG(rect, V.MenuSectionBG, V.ResolvedMenuSectionTex, CachedMenuSectionBGBorderColor);
                return false;
            }
        }

        [HarmonyPatch(typeof(Widgets), nameof(Widgets.BeginScrollView))]
        static class Patch_BeginScrollView
        {
            struct SavedState { public Color color; public Texture2D vThumb, vBg, hThumb, hBg; public bool swapped; }
            static readonly Stack<SavedState> stack = new Stack<SavedState>();

            static void Prefix()
            {
                var saved = default(SavedState);
                saved.color = GUI.color;
                bool elemOn = On && V.ElemOn(ThemeValues.Elem.Scrollbar);
                if (elemOn) GUI.color = V.ScrollbarBG;

                var tex = elemOn ? V.ResolvedScrollbarTex : null;
                if (tex != null)
                {
                    var vBar = GUI.skin.verticalScrollbar;
                    var vThumb = GUI.skin.verticalScrollbarThumb;
                    var hBar = GUI.skin.horizontalScrollbar;
                    var hThumb = GUI.skin.horizontalScrollbarThumb;
                    saved.vBg = vBar.normal.background;
                    saved.vThumb = vThumb.normal.background;
                    saved.hBg = hBar.normal.background;
                    saved.hThumb = hThumb.normal.background;
                    saved.swapped = true;
                    vBar.normal.background = tex;
                    vThumb.normal.background = tex;
                    hBar.normal.background = tex;
                    hThumb.normal.background = tex;
                }
                stack.Push(saved);
            }

            static void Postfix()
            {
                if (stack.Count == 0) return;
                var saved = stack.Pop();
                GUI.color = saved.color;
                if (!saved.swapped) return;
                GUI.skin.verticalScrollbar.normal.background = saved.vBg;
                GUI.skin.verticalScrollbarThumb.normal.background = saved.vThumb;
                GUI.skin.horizontalScrollbar.normal.background = saved.hBg;
                GUI.skin.horizontalScrollbarThumb.normal.background = saved.hThumb;
            }
        }

        [HarmonyPatch(typeof(TabRecord), nameof(TabRecord.Draw))]
        [StaticConstructorOnStartup]
        static class Patch_TabRecordDraw
        {
            static bool Prefix(TabRecord __instance, Rect rect)
            {
                if (!On || !V.ElemOn(ThemeValues.Elem.Tab)) return true;
                Texture2D atlas = V.ResolvedTabTex ?? CachedTabAtlas;
                if (atlas == null) return true;
                ThemeElementRenderer.DrawTabShape(__instance, rect, V.TabBG, atlas);
                DrawTabLabelOnly(__instance, rect);
                return false;
            }

            static void DrawTabLabelOnly(TabRecord t, Rect rect)
            {
                GUI.color = t.labelColor ?? Color.white;
                Rect labelRect = new Rect(rect) { width = rect.width - 10f };
                if (Mouse.IsOver(labelRect))
                {
                    GUI.color = Color.yellow;
                    labelRect.x += 2f;
                    labelRect.y -= 2f;
                }
                if (!t.TutorTag.NullOrEmpty()) UIHighlighter.HighlightOpportunity(labelRect, t.TutorTag);
                Text.WordWrap = false;
                Widgets.Label(rect, t.label);
                Text.WordWrap = true;
                GUI.color = Color.white;
            }

            static readonly Texture2D CachedTabAtlas = SafeField<Texture2D>(typeof(TabRecord), "TabAtlas");
        }

        [HarmonyPatch(typeof(Widgets), "ButtonTextSubtle")]
        static class Patch_ButtonTextSubtle
        {
            static bool Prefix(Rect rect, string label, float barPercent, float textLeftMargin, SoundDef mouseoverSound, Vector2 functionalSizeOffset, Color? labelColor, bool highlight, ref bool __result)
            {
                if (!On || themedScopeDepth == 0 || !V.ElemOn(ThemeValues.Elem.BottomBar)) return true;
                var customAtlas = V.ResolvedBottomBarTex;
                bool hasCustom = customAtlas != null && customAtlas;

                Rect rect2 = rect;
                rect2.width += functionalSizeOffset.x;
                rect2.height += functionalSizeOffset.y;
                bool hover = Mouse.IsOver(rect2);

                GUI.color = hover ? V.HoverColor : V.BottomBarBG;
                if (hasCustom) ThemeElementRenderer.DrawButtonBG(rect, GUI.color, customAtlas);
                else Widgets.DrawAtlas(rect, Widgets.ButtonSubtleAtlas);
                GUI.color = Color.white;

                if (mouseoverSound != null) MouseoverSounds.DoRegion(rect2, mouseoverSound);
                if (highlight)
                {
                    GUI.color = Color.grey;
                    Widgets.DrawBox(rect, 2);
                    GUI.color = Color.white;
                }
                if (barPercent > 0.001f)
                    Widgets.FillableBar(rect.ContractedBy(1f), barPercent, CachedButtonBarTex, null, false);

                Rect rect3 = new Rect(rect);
                float margin = textLeftMargin < 0f ? rect.width * 0.15f : textLeftMargin;
                rect3.x += margin;
                if (hover) { rect3.x += 2f; rect3.y -= 2f; }

                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;
                Text.Font = GameFont.Small;
                GUI.color = labelColor ?? Color.white;
                Widgets.Label(rect3, label);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                GUI.color = Color.white;

                __result = Widgets.ButtonInvisible(rect2, false);
                return false;
            }
        }

        static void ThemedScopePrefix(out Color __state)
        {
            __state = GUI.color;
            MarkThemedScope(true);
        }

        static void ThemedScopePostfix(Color __state)
        {
            GUI.color = __state;
            MarkThemedScope(false);
        }

        [HarmonyPatch(typeof(MainButtonWorker), nameof(MainButtonWorker.DoButton))]
        static class Patch_MainButtonWorkerDoButton
        {
            static void Prefix(out Color __state) => ThemedScopePrefix(out __state);
            static void Postfix(Color __state) => ThemedScopePostfix(__state);
        }

        [HarmonyPatch(typeof(MainTabWindow_Architect), nameof(MainTabWindow_Architect.DoWindowContents))]
        static class Patch_ArchitectDoWindowContents
        {
            static void Prefix(out Color __state) => ThemedScopePrefix(out __state);
            static void Postfix(Color __state) => ThemedScopePostfix(__state);
        }
        [HarmonyPatch(typeof(UI_BackgroundMain), nameof(UI_BackgroundMain.BackgroundOnGUI))]
        static class Patch_BackgroundOnGUI
        {
            static bool Prefix()
            {
                if (!BackgroundsOn) return true;
                ThemeBackground.Draw(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), V);
                return false;
            }
        }

        [HarmonyPatch(typeof(MainMenuDrawer), "MainMenuOnGUI")]
        static class Patch_MainMenuOnGUI
        {
            static readonly FieldInfo TexTitleField = AccessTools.Field(typeof(MainMenuDrawer), "TexTitle");
            static readonly FieldInfo TitleSizeField = AccessTools.Field(typeof(MainMenuDrawer), "TitleSize");

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (var ins in instructions)
                {
                    if (TexTitleField != null && ins.opcode == OpCodes.Ldsfld && (FieldInfo)ins.operand == TexTitleField)
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_MainMenuOnGUI), nameof(TitleTex)));
                    else if (TitleSizeField != null && ins.opcode == OpCodes.Ldsfld && (FieldInfo)ins.operand == TitleSizeField)
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_MainMenuOnGUI), nameof(TitleSize)));
                    else
                        yield return ins;
                }
            }

            static Texture2D TitleTex() => On && V.ElemOn(ThemeValues.Elem.MainMenuTitle) && V.ResolvedTitleTex != null ? V.ResolvedTitleTex : (Texture2D)TexTitleField.GetValue(null);

            static Vector2 TitleSize()
            {
                var vanilla = (Vector2)TitleSizeField.GetValue(null);
                if (!On || !V.ElemOn(ThemeValues.Elem.MainMenuTitle) || V.ResolvedTitleTex == null) return vanilla;
                var tex = V.ResolvedTitleTex;
                if (tex.height <= 0) return vanilla;
                return new Vector2(vanilla.x, vanilla.x * tex.height / tex.width);
            }
        }

        [HarmonyPatch(typeof(Root), nameof(Root.Update))]
        static class Patch_RootUpdate
        {
            static void Postfix() => ThemeLoader.TickCursorAnimation();
        }

        static SoundDef Override(SoundDef vanilla)
        {
            if (vanilla == null || !SoundsOn) return null;
            if (!V.soundOverrides.TryGetValue(vanilla.defName, out string overridePath) || string.IsNullOrEmpty(overridePath)) return null;
            var replacement = SoundOrNull(overridePath);
            return replacement == null || replacement == vanilla ? null : replacement;
        }

        [HarmonyPatch(typeof(SoundStarter), nameof(SoundStarter.PlayOneShotOnCamera))]
        static class Patch_PlayOneShotOnCamera
        {
            static bool Prefix(SoundDef soundDef)
            {
                var replacement = Override(soundDef);
                if (replacement == null) return true;
                replacement.PlayOneShotOnCamera();
                return false;
            }
        }

        [HarmonyPatch(typeof(MouseoverSounds), nameof(MouseoverSounds.DoRegion), new[] { typeof(Rect), typeof(SoundDef) })]
        static class Patch_MouseoverDoRegion
        {
            static void Prefix(ref SoundDef sound)
            {
                if (Event.current.type != EventType.Repaint) return;
                var replacement = Override(sound);
                if (replacement != null) sound = replacement;
            }
        }

        [HarmonyPatch(typeof(Window), nameof(Window.PostOpen))]
        static class Patch_WindowPostOpen
        {
            static void Prefix(Window __instance, out SoundDef __state)
            {
                __state = null;
                var replacement = Override(__instance.soundAppear);
                if (replacement == null) return;
                replacement.PlayOneShotOnCamera();
                __state = __instance.soundAppear;
                __instance.soundAppear = null;
            }

            static void Postfix(Window __instance, SoundDef __state)
            {
                if (__state != null) __instance.soundAppear = __state;
            }
        }

        [HarmonyPatch(typeof(WindowStack), nameof(WindowStack.TryRemove), new[] { typeof(Window), typeof(bool) })]
        static class Patch_WindowStackTryRemove
        {
            static bool Prefix(Window window, ref bool doCloseSound)
            {
                if (!doCloseSound) return true;
                var replacement = Override(window.soundClose);
                if (replacement == null) return true;
                replacement.PlayOneShotOnCamera();
                doCloseSound = false;
                return true;
            }
        }
    }
}