using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PassiveTree.Encounters.HallMaps;
using SeneaLUI.Inv;
using SeneaLUI.Patches;
using UnityEngine;

namespace SeneaLUI.PovSkills
{
    // PoV 6.0's Runic Anvil window and Book of the Halls (with its shop) draw inside the game's own crafting panel.
    // SeneaL replaces that panel and keeps the game's one invisible, so these windows open unseen. While one is open
    // this runs SeneaL's own fallback for crafting UIs it doesn't replace: its craft panel steps aside and the game's
    // panel is shown in SeneaL's craft column. At the anvil (a container) SeneaL's chest panel is also hidden, as
    // PoV hides the game's container panel there and shows the anvil stock in its own window.
    internal static class CraftPanelWindows
    {
        private static MethodInfo _fallback, _show;
        private static FieldInfo _vanilla, _applied, _foreign, _fallbackHide;
        private static readonly AccessTools.FieldRef<RectTransform> AnvilWindow =
            AccessTools.StaticFieldRefAccess<RectTransform>(AccessTools.Field(typeof(PassiveTree.UI.RunicAnvilWindow), "window"));
        private static readonly AccessTools.FieldRef<Container> AnvilShown =
            AccessTools.StaticFieldRefAccess<Container>(AccessTools.Field(typeof(PassiveTree.UI.RunicAnvilWindow), "shown"));

        private static bool _active;
        private static CanvasGroup _chestGroup;
        private static bool _broken;

        internal static string Patch(Harmony harmony)
        {
            var t = typeof(CraftView);
            _fallback = AccessTools.Method(t, "Fallback", new[] { typeof(bool), typeof(bool) });
            _show = AccessTools.Method(t, "Show", new[] { typeof(bool) });
            _vanilla = AccessTools.Field(t, "_vanilla");
            _applied = AccessTools.Field(t, "_applied");
            _foreign = AccessTools.Field(t, "_foreign");
            _fallbackHide = AccessTools.Field(t, "_fallbackHide");
            var tick = AccessTools.Method(t, "Tick", new[] { typeof(bool), typeof(bool) });
            if (_fallback == null || _show == null || _vanilla == null || _applied == null || _foreign == null
                || _fallbackHide == null || tick == null)
                return "SeneaL CraftView fallback members";
            harmony.Patch(tick, postfix: new HarmonyMethod(typeof(CraftPanelWindows), nameof(TickPostfix)));
            return null;
        }

        private static bool AnvilOpen()
        {
            var w = AnvilWindow();
            return w && w.gameObject.activeSelf && AnvilShown() != null;
        }

        private static void TickPostfix(CraftView __instance, bool open)
        {
            Plugin.Guard(ref _broken, "Runic Anvil / Book of the Halls windows", () => Tick(__instance, open));
            if (_broken && _active) Release(__instance);
        }

        private static void Tick(CraftView view, bool open)
        {
            // SeneaL already shows the game's panel when its own crafting is off or a foreign station tab is open.
            bool seneaLOwns = (bool)_applied.GetValue(view) && !(bool)_foreign.GetValue(view);
            bool anvil = AnvilOpen();
            bool want = seneaLOwns && open && (anvil || HallMapWindow.IsOpen || HallMapShopWindow.IsOpen);

            if (want)
            {
                _active = true;
                _show.Invoke(view, new object[] { false });
                _fallback.Invoke(view, new object[] { true, false });
                SetChestHidden(anvil);
            }
            else if (_active)
            {
                Release(view);
            }
            if (open) TintButtons();
        }

        // PoV builds every button in these windows (and its Enchant tab) by cloning the game's craft button, named
        // PT_AnvilButton, which looks the same usable or not; PoV only greys the label when you can't afford it.
        // Tint the button itself: gold when usable, grey when not.
        private static readonly Color Usable = new Color(1f, 0.8f, 0.42f);
        private static readonly Color Unusable = new Color(0.55f, 0.55f, 0.55f);
        private static readonly List<UnityEngine.UI.Button> Buttons = new List<UnityEngine.UI.Button>();
        private static float _nextScan;

        private static void TintButtons()
        {
            var crafting = InventoryGui.instance ? InventoryGui.instance.m_crafting : null;
            if (!crafting) return;
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.5f;
                crafting.GetComponentsInChildren(true, Buttons);
                Buttons.RemoveAll(b => b.name != "PT_AnvilButton");
            }
            foreach (var b in Buttons)
            {
                if (!b || !b.image) continue;
                var c = b.interactable ? Usable : Unusable;
                if (b.image.color != c) b.image.color = c;
            }
        }

        private static void Release(CraftView view)
        {
            _active = false;
            SetChestHidden(false);
            if (!(bool)_applied.GetValue(view) || (bool)_foreign.GetValue(view)) return;
            // What SeneaL's LeaveForeign does: hide the game's panel again and let SeneaL's panel back.
            var g = (CanvasGroup)_vanilla.GetValue(view);
            if (g)
            {
                g.alpha = 0f;
                g.blocksRaycasts = false;
                g.interactable = false;
            }
            _fallbackHide.SetValue(view, -1);
            CraftView.Dirty = true;
        }

        private static void SetChestHidden(bool hide)
        {
            var chest = InventoryGuiPatches.Chest;
            if (chest == null || !chest.Panel) return;
            if (!_chestGroup)
            {
                if (!hide) return;
                _chestGroup = chest.Panel.GetComponent<CanvasGroup>() ?? chest.Panel.gameObject.AddComponent<CanvasGroup>();
            }
            _chestGroup.alpha = hide ? 0f : 1f;
            _chestGroup.blocksRaycasts = !hide;
            _chestGroup.interactable = !hide;
        }
    }
}
