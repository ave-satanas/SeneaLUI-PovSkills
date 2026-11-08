using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SeneaLUI.PovSkills
{
    // Bridges Path of Valheiman 6.0's new UI into SeneaL UI, which hosts PoV's equipment window and replaces the
    // crafting panel and build HUD. Each part patches only its own SeneaL/PoV members and turns itself off alone
    // (logged) if one of them is missing, so a later SeneaL or PoV update breaks at most that part.
    [BepInPlugin(Guid, "SeneaLUI PoV 6.0 Bridge", "2.4.1")]
    [BepInDependency("seneaL.valheim.ui")]
    [BepInDependency("treextr.pathofvalheiman")]
    [BepInDependency("Azumatt.BowsBeforeHoes", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Guid = "avesatanas.senealui.povskills";
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            var harmony = new Harmony(Guid);
            Part(harmony, "Skills (gem board) button", SkillsBoard.Patch);
            Part(harmony, "quiver and belt lantern cells", NewCells.Patch);
            Part(harmony, "PoV quiver back mount", h => Quivers.PatchVisual(h, Config));
            Part(harmony, "BowsBeforeHoes quiver equipment", Quivers.PatchEquipment);
            Part(harmony, "PoV Halls map icons", HallsPins.Patch);
            Part(harmony, "Runic Anvil and Book of the Halls windows", CraftPanelWindows.Patch);
            Part(harmony, "ballista count in the build panel", BallistaInfo.Patch);
            Part(harmony, "test command pov_halls_gate", TestCommands.Patch);
        }

        private static void Part(Harmony harmony, string name, Func<Harmony, string> patch)
        {
            string missing;
            try
            {
                missing = patch(harmony);
            }
            catch (Exception e)
            {
                missing = e.Message;
            }
            if (missing == null) Log.LogInfo(name + ": on");
            else Log.LogError(name + ": off (" + missing + ")");
        }

        // Every per-frame hook goes through this so one failure turns off only its own part.
        internal static void Guard(ref bool broken, string name, Action body)
        {
            if (broken) return;
            try
            {
                body();
            }
            catch (Exception e)
            {
                broken = true;
                Log.LogError(name + " turned off: " + e);
            }
        }
    }
}
