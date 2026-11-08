using HarmonyLib;
using PassiveTree.Encounters.Halls;
using UnityEngine;

namespace SeneaLUI.PovSkills
{
    // PoV's release build strips its own pt_ test commands. Prefabs can be found with Server Devcommands' search_id
    // and spawned, but a Halls gate can't: `spawn PoV_HallsGate` skips the theme and site PoV gives it in SpawnAt.
    internal static class TestCommands
    {
        private static bool _registered;

        internal static string Patch(Harmony harmony)
        {
            var init = AccessTools.Method(typeof(Terminal), "InitTerminal");
            if (init == null) return "Terminal.InitTerminal";
            harmony.Patch(init, postfix: new HarmonyMethod(typeof(TestCommands), nameof(Register)));
            return null;
        }

        private static void Register()
        {
            if (_registered) return;
            _registered = true;

            new Terminal.ConsoleCommand("pov_halls_gate", "- a Halls of Oaths gate 4 m in front of you, themed for this biome",
                args =>
                {
                    var p = Player.m_localPlayer;
                    if (!p) return;
                    var at = p.transform.position + p.transform.forward * 4f;
                    var gate = HallsGate.SpawnAt(at, p.transform.eulerAngles.y + 180f);
                    args.Context.AddString(gate ? "gate placed: use it (E) for the Book of the Halls, Shift+E for the lobby" : "PoV has no gate prefab yet");
                }, isCheat: true);
        }
    }
}
