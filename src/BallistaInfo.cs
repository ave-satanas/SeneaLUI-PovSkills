using HarmonyLib;
using SeneaLUI.Hud;
using TMPro;
using UnityEngine;
using Valheiman.Api;

namespace SeneaLUI.PovSkills
{
    // PoV 6.0 adds «Ballistas n/m» to the game's build piece description, which SeneaL blanks and replaces with its
    // own build panel built from the piece's description. This adds the same line to SeneaL's panel.
    internal static class BallistaInfo
    {
        private const string BallistaPrefab = "PT_Ballista";

        private static readonly AccessTools.FieldRef<BuildInfoView, TextMeshProUGUI> Desc =
            AccessTools.FieldRefAccess<BuildInfoView, TextMeshProUGUI>("_desc");
        private static readonly AccessTools.FieldRef<BuildInfoView, Piece> Shown =
            AccessTools.FieldRefAccess<BuildInfoView, Piece>("_piece");
        private static readonly AccessTools.FieldRef<BuildInfoView, float> DescH =
            AccessTools.FieldRefAccess<BuildInfoView, float>("_descH");

        private static string _line;
        private static bool _broken;

        internal static string Patch(Harmony harmony)
        {
            var tick = AccessTools.Method(typeof(BuildInfoView), "Tick", new[] { typeof(Player) });
            if (tick == null) return "SeneaL BuildInfoView.Tick";
            if (!Pov.Has("ballistas")) return "PoV ballistas feature";
            harmony.Patch(tick, postfix: new HarmonyMethod(typeof(BallistaInfo), nameof(TickPostfix)));
            return null;
        }

        private static void TickPostfix(BuildInfoView __instance)
        {
            Plugin.Guard(ref _broken, "Ballista count", () =>
            {
                var piece = Shown(__instance);
                var desc = Desc(__instance);
                if (!desc) return;
                string text = desc.text;
                // SeneaL rewrites the text only when the piece changes; strip our last line before adding the new one.
                if (_line != null && text.EndsWith(_line)) text = text.Substring(0, text.Length - _line.Length);
                _line = null;
                if (piece && piece.name.StartsWith(BallistaPrefab))
                {
                    _line = (text.Length > 0 ? "\n" : "") + "<color=#E0B53F>"
                        + string.Format(Texts.Get("ballista.menu.limit", "Ballistas {0}/{1}"), Ballistas.Count, Ballistas.Limit)
                        + "</color>";
                    text += _line;
                }
                if (text != desc.text)
                {
                    desc.text = text;
                    DescH(__instance) = -1f;
                }
            });
        }
    }
}
