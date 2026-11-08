using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SeneaLUI.PovSkills
{
    internal static class HallsPins
    {
        private const string GatePrefix = "pov_hallsgate_";
        private static Dictionary<string, Minimap.PinData> _shown;
        private static MethodInfo _iconFor;

        internal static string Patch(Harmony harmony)
        {
            var pov = AccessTools.TypeByName("PassiveTree.Encounters.Pins");
            var map = AccessTools.TypeByName("SeneaLUI.Map.MapPins");
            _shown = (pov == null ? null : AccessTools.Field(pov, "shown")?.GetValue(null))
                as Dictionary<string, Minimap.PinData>;
            if (_shown == null || map == null) return "PoV/SeneaL pin registry";

            var sync = AccessTools.Method(map, "Sync");
            _iconFor = AccessTools.Method(map, "IconFor");
            if (sync == null || _iconFor == null) return "SeneaL map rendering";
            harmony.Patch(sync, transpiler: new HarmonyMethod(typeof(HallsPins), nameof(UseOriginalIcon)));
            return null;
        }

        private static IEnumerable<CodeInstruction> UseOriginalIcon(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            int call = code.FindIndex(instruction => instruction.Calls(_iconFor));
            var pinType = AccessTools.Field(typeof(Minimap.PinData), "m_type");
            if (call < 3 || code.FindLastIndex(instruction => instruction.Calls(_iconFor)) != call ||
                !code[call - 2].LoadsField(pinType) || !code[call - 3].IsLdloc() || code[call].blocks.Count != 0)
                throw new System.InvalidOperationException("SeneaL pin icon call changed");

            var pin = new CodeInstruction(code[call - 3].opcode, code[call - 3].operand);
            pin.labels.AddRange(code[call].labels);
            code[call].labels.Clear();
            code[call].operand = AccessTools.Method(typeof(HallsPins), nameof(IconFor));
            code.Insert(call, pin);
            return code;
        }

        private static string IconFor(Minimap.PinType type, int custom, Minimap.PinData pin)
        {
            if (pin.m_icon && type == Minimap.PinType.Icon3 && !pin.m_save)
                foreach (var entry in _shown)
                    if (ReferenceEquals(entry.Value, pin) && entry.Key.StartsWith(GatePrefix, System.StringComparison.Ordinal))
                        return null; // SeneaL uses pin.m_icon for a null icon name.

            return SeneaLUI.Map.MapPins.IconFor(type, custom);
        }
    }
}
