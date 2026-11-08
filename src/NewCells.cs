using System.Collections.Generic;
using HarmonyLib;
using SeneaLUI.Mods;
using UnityEngine;
using Valheiman.Api;

namespace SeneaLUI.PovSkills
{
    // PoV 6.0 added a belt lantern cell (opened by the craft tree's Belt Lantern node). SeneaL lists PoV's cells in
    // PovGear.Read and routes cells from 45 up through PoV's Equipment API by id, but its id table ends at ring 5
    // (slot 53). This appends the cell to that list (SeneaL shows unnamed cells as its "extra" equipment cells) and
    // answers SeneaL's routing questions for it through the same public API.
    // Not bridged: the siege crossbow (61) and bolt magazine (62) cells. 6.0 retires them - nothing uses them, they
    // accept no items, and PoV empties them into the bag once per session.
    internal static class NewCells
    {
        private struct NewCell
        {
            public int Slot;
            public string Id;
            public string Word;
            public string English;
        }

        // Slot numbers are PoV's PassiveTree.Gear.Slot values; ids are PoV's Equipment API cell ids.
        private static readonly NewCell[] Cells =
        {
            new NewCell { Slot = 7, Id = "quiver", Word = "Quiver", English = "Quiver" },
            new NewCell { Slot = 60, Id = "belt.lantern", Word = "BeltLantern", English = "Belt lantern" },
        };

        private static bool _broken;

        internal static string Patch(Harmony harmony)
        {
            foreach (var c in Cells)
                if (Equipment.Cell(c.Id) == null) return "PoV cell " + c.Id;

            var t = typeof(PovGear);
            var read = AccessTools.Method(t, "Read");
            var targets = new[]
            {
                AccessTools.Method(t, "HasCell"), AccessTools.Method(t, "IsOpen"), AccessTools.Method(t, "TakeOut"),
                AccessTools.Method(t, "Cell"), AccessTools.Method(t, "Accepts"), AccessTools.Method(t, "Assign"),
                AccessTools.Method(t, "Routed"),
            };
            if (read == null || System.Array.IndexOf(targets, null) >= 0) return "SeneaL PovGear routing";

            harmony.Patch(read, postfix: new HarmonyMethod(typeof(NewCells), nameof(ReadPostfix)));
            harmony.Patch(targets[0], prefix: new HarmonyMethod(typeof(NewCells), nameof(HasCellPrefix)));
            harmony.Patch(targets[1], prefix: new HarmonyMethod(typeof(NewCells), nameof(IsOpenPrefix)));
            harmony.Patch(targets[2], prefix: new HarmonyMethod(typeof(NewCells), nameof(TakeOutPrefix)));
            harmony.Patch(targets[3], prefix: new HarmonyMethod(typeof(NewCells), nameof(CellPrefix)));
            harmony.Patch(targets[4], prefix: new HarmonyMethod(typeof(NewCells), nameof(AcceptsPrefix)));
            harmony.Patch(targets[5], prefix: new HarmonyMethod(typeof(NewCells), nameof(AssignPrefix)));
            // Routed just calls HasCell, but is small enough for Mono to inline it past the HasCell patch.
            harmony.Patch(targets[6], prefix: new HarmonyMethod(typeof(NewCells), nameof(HasCellPrefix)));
            return null;
        }

        private static string IdOf(int slot)
        {
            if (_broken) return null;
            foreach (var c in Cells)
                if (c.Slot == slot) return c.Id;
            return null;
        }

        private static void ReadPostfix(Player player, List<AzuSlot> into, List<AzuSlot> pool, ref int __result)
        {
            int n = __result;
            // 0 means PoV's rows aren't attached yet; SeneaL then shows no PoV cells at all.
            if (n == 0 || !player) return;
            Plugin.Guard(ref _broken, "New equipment cells", () =>
            {
                var inv = player.GetInventory();
                foreach (var c in Cells)
                {
                    var existing = into.Find(slot => slot.Pov == c.Slot);
                    var worn = c.Slot == 7 ? Quivers.Worn(player) : null;
                    if (existing != null && worn == null) continue;
                    bool open = Equipment.IsOpen(player, c.Id);
                    var pos = worn == null ? Equipment.GridPosition(player, c.Id) : worn.m_gridPos;
                    // Keep a closed cell visible while it still holds something, so it can be taken out.
                    if (pos.x < 0 || (!open && inv.GetItemAt(pos.x, pos.y) == null)) continue;

                    if (existing == null && pool.Count <= n) pool.Add(new AzuSlot());
                    var s = existing ?? pool[n++];
                    s.Index = c.Slot;
                    s.Name = PovGear.Word(c.Word);
                    // Original stays English: SeneaL matches it against fixed words (ring, trinket...) to pick a seat.
                    s.Original = c.English;
                    s.Quick = false;
                    s.Equip = true;
                    s.Custom = false;
                    s.Grid = pos;
                    s.Linear = pos.y * inv.GetWidth() + pos.x;
                    s.Item = inv.GetItemAt(pos.x, pos.y);
                    s.Occupied = s.Item != null;
                    s.Kind = 0;
                    s.Pov = c.Slot;
                    s.KindIndex = 0;
                    s.Our = 0;
                    s.QuickIndex = -1;
                    s.Closed = false;
                    if (existing == null) into.Add(s);
                }
            });
            __result = n;
        }

        private static bool HasCellPrefix(int slot, ref bool __result)
        {
            if (IdOf(slot) == null) return true;
            __result = true;
            return false;
        }

        private static bool IsOpenPrefix(Player p, int slot, ref bool __result)
        {
            var id = IdOf(slot);
            if (id == null) return true;
            __result = p && Equipment.IsOpen(p, id);
            return false;
        }

        private static bool TakeOutPrefix(Player p, int slot, ref bool __result)
        {
            var id = IdOf(slot);
            if (id == null) return true;
            if (p) Equipment.TakeOut(p, id);
            __result = p;
            return false;
        }

        private static bool CellPrefix(Inventory inv, int slot, ref Vector2i __result)
        {
            var id = IdOf(slot);
            var p = Player.m_localPlayer;
            if (id == null || !p || p.GetInventory() != inv) return true;
            __result = Equipment.GridPosition(p, id);
            return false;
        }

        private static bool AcceptsPrefix(Player p, int slot, ItemDrop.ItemData item, ref bool __result)
        {
            var id = IdOf(slot);
            if (id == null) return true;
            __result = p && item != null && Equipment.Accepts(id, item) && Equipment.IsOpen(p, id);
            return false;
        }

        private static bool AssignPrefix(Player p, int slot, ItemDrop.ItemData item, ref bool __result)
        {
            var id = IdOf(slot);
            if (id == null) return true;
            __result = p && item != null && Equipment.Put(p, id, item);
            return false;
        }
    }
}
