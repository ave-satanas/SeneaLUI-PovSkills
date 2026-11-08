using System.Reflection;
using HarmonyLib;
using SeneaLUI.Components;
using SeneaLUI.Core;
using SeneaLUI.Inv;
using SeneaLUI.Theme;
using UnityEngine;
using UnityEngine.UI;
using Valheiman.Api;
using Tooltip = SeneaLUI.Components.Tooltip;

namespace SeneaLUI.PovSkills
{
    // PoV 6.0 put a gem board ("Skills" tab) in its equipment window. SeneaL hosts that window and only knows the
    // Appearance tab, so this adds a button beside SeneaL's Appearance button and shows PoV's own board panel next
    // to the bag, the way PoV shows it beside its sheet in place of the crafting panel.
    internal static class SkillsBoard
    {
        private const float Gap = 12f;
        private const float BoardW = 520f;

        private static readonly AccessTools.FieldRef<InventoryView, RectTransform> LooksBtn =
            AccessTools.FieldRefAccess<InventoryView, RectTransform>("_looksBtn");
        private static readonly AccessTools.FieldRef<InventoryView, Sprite> DollN =
            AccessTools.FieldRefAccess<InventoryView, Sprite>("_dollN");
        private static readonly AccessTools.FieldRef<InventoryView, Sprite> DollH =
            AccessTools.FieldRefAccess<InventoryView, Sprite>("_dollH");

        private static ConstructorInfo _ctor;
        private static MethodInfo _show, _fit, _tick;

        private static RectTransform _btn;
        private static Image _btnBg;
        private static RectTransform _host;
        private static object _board;
        private static bool _open;
        private static bool _wantOpen;
        private static bool _broken;
        private static readonly Tip TipCache = new Tip();

        internal static string Patch(Harmony harmony)
        {
            // GemBoardPanel is internal to PoV; its public API has no board, so it is reached by reflection.
            var panel = AccessTools.TypeByName("PassiveTree.Gems.GemBoardPanel");
            if (panel == null) return "PoV GemBoardPanel";
            _ctor = AccessTools.Constructor(panel, new[] { typeof(RectTransform), typeof(TMPro.TMP_FontAsset), typeof(InventoryGui) });
            _show = AccessTools.Method(panel, "Show", new[] { typeof(bool) });
            _fit = AccessTools.Method(panel, "Fit", new[] { typeof(Vector2), typeof(Vector2) });
            _tick = AccessTools.Method(panel, "Tick", new[] { typeof(Player) });
            var tickPov = AccessTools.Method(typeof(InventoryView), "TickPov");
            var showSkills = AccessTools.Method("PassiveTree.Gear.Module:ShowSkills");
            if (_ctor == null || _show == null || _fit == null || _tick == null) return "PoV GemBoardPanel members";
            if (tickPov == null) return "SeneaL InventoryView.TickPov";

            harmony.Patch(tickPov, postfix: new HarmonyMethod(typeof(SkillsBoard), nameof(TickPovPostfix)));
            harmony.Patch(AccessTools.Method(typeof(InventoryGui), "Hide"), postfix: new HarmonyMethod(typeof(SkillsBoard), nameof(HidePostfix)));
            // Right-clicking an Awakening Chisel asks PoV's own (hidden) sheet for the board; open ours instead.
            if (showSkills != null)
                harmony.Patch(showSkills, postfix: new HarmonyMethod(typeof(SkillsBoard), nameof(ShowSkillsPostfix)));
            return null;
        }

        private static void ShowSkillsPostfix() => _wantOpen = true;

        private static void TickPovPostfix(InventoryView __instance, Player player)
        {
            Plugin.Guard(ref _broken, "Skills button", () => Tick(__instance, player));
            if (_broken)
            {
                SetOpen(false);
                if (_btn) _btn.gameObject.SetActive(false);
            }
        }

        // Closing the inventory must hide the board so PoV returns a lifted gem to the bag.
        private static void HidePostfix()
        {
            _wantOpen = false;
            if (_open) SetOpen(false);
        }

        private static void Tick(InventoryView view, Player player)
        {
            var looks = LooksBtn(view);
            if (!looks) return;
            if (!_btn) Build(view, looks);

            bool on = looks.gameObject.activeSelf && Pov.Has("gems");
            if (_btn.gameObject.activeSelf != on)
            {
                _btn.gameObject.SetActive(on);
                if (!on) Tooltip.Exit(_btn);
            }
            _btn.anchoredPosition = looks.anchoredPosition - new Vector2(looks.sizeDelta.x + 6f, 0f);
            if (_wantOpen && on && !_open) SetOpen(true);
            _wantOpen = false;
            if (!on && _open) SetOpen(false);
            if (!_open) return;

            // Beside the bag panel, over the craft/chest column, like PoV's own board replaces its crafting panel.
            var bag = view.Panel;
            var corners = new Vector3[4];
            bag.GetWorldCorners(corners);
            var parent = (RectTransform)_host.parent;
            _host.SetAsLastSibling();
            _host.localPosition = parent.InverseTransformPoint(corners[2]) + new Vector3(Gap, 0f, 0f);
            float h = Mathf.Max(400f, bag.rect.height);
            _fit.Invoke(_board, new object[] { Vector2.zero, new Vector2(BoardW, h) });
            _tick.Invoke(_board, new object[] { player });
        }

        private static void Build(InventoryView view, RectTransform looks)
        {
            // Clone keeps SeneaL's look; Unity does not copy the delegates, so the handlers below are the only ones.
            _btn = Object.Instantiate(looks.gameObject, looks.parent, false).GetComponent<RectTransform>();
            _btn.name = "povskillsbtn";
            _btnBg = _btn.GetComponent<Image>();
            var icon = _btn.Find("i")?.GetComponent<Image>();
            if (icon) icon.sprite = Phosphor.Get(Phosphor.Has("hexagon") ? "hexagon" : "diamond");

            var input = _btn.GetComponent<UIInputHandler>();
            input.m_onPointerEnter = h =>
            {
                if (_btnBg) _btnBg.sprite = DollH(view);
                Tooltip.Show(_btn, () => Tips.Text(TipCache, Texts.Get("gem.tab", "Skills"), null));
            };
            input.m_onPointerExit = h =>
            {
                if (_btnBg) _btnBg.sprite = DollN(view);
                Tooltip.Exit(_btn);
            };
            input.m_onLeftClick = h =>
            {
                Tooltip.Exit(_btn);
                Click.Play();
                SetOpen(!_open);
            };

            _host = new GameObject("povskills", typeof(RectTransform)).GetComponent<RectTransform>();
            _host.SetParent(view.Root, false);
            _host.pivot = _host.anchorMin = _host.anchorMax = new Vector2(0f, 1f);
            _host.sizeDelta = Vector2.zero;
            _board = _ctor.Invoke(new object[] { _host, null, InventoryGui.instance });
            SetOpen(false);
        }

        private static void SetOpen(bool on)
        {
            _open = on;
            if (_board != null) _show.Invoke(_board, new object[] { on });
            if (_host) _host.gameObject.SetActive(on);
        }
    }
}
