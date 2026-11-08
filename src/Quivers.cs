using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using EquipmentApi = PassiveTree.Gear.EquipmentApi;

namespace SeneaLUI.PovSkills
{
    internal static class Quivers
    {
        private const string Bbh = "Azumatt.BowsBeforeHoes";
        private static FieldInfo _prefabs;
        private static Func<Humanoid, ItemDrop.ItemData> _bbhWorn;
        private static Func<Humanoid, ItemDrop.ItemData> _povWorn;
        private static Func<ItemDrop.ItemData, bool> _isBbh;
        private static readonly HashSet<string> Registered = new HashSet<string>(StringComparer.Ordinal);

        private static AccessTools.FieldRef<VisEquipment, ZNetView> _view;
        private static object _shown;
        private static MethodInfo _getShown;
        private static FieldInfo _model;
        private static ConfigEntry<Vector3> _position;
        private static ConfigEntry<Vector3> _rotation;

        internal static ItemDrop.ItemData Worn(Player player)
        {
            var item = _bbhWorn?.Invoke(player);
            return item != null && player.GetInventory().ContainsItem(item) ? item : null;
        }

        internal static string PatchEquipment(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.TryGetValue(Bbh, out var plugin)) return "BowsBeforeHoes absent";
            var functions = plugin.Instance.GetType().Assembly.GetType("BowsBeforeHoes.Util.Functions");
            var worn = functions == null ? null : AccessTools.Method(functions, "GetEquippedQuiver");
            var isQuiver = functions == null ? null : AccessTools.Method(functions, "IsQuiverSlot", new[] { typeof(ItemDrop.ItemData) });
            var povWorn = AccessTools.Method(typeof(PassiveTree.QuiverSlot), "Get");
            _prefabs = AccessTools.Field(plugin.Instance.GetType(), "QuiverList");
            if (worn == null || isQuiver == null || povWorn == null || _prefabs == null)
                return "quiver equipment contract";

            _bbhWorn = (Func<Humanoid, ItemDrop.ItemData>)Delegate.CreateDelegate(typeof(Func<Humanoid, ItemDrop.ItemData>), worn);
            _povWorn = (Func<Humanoid, ItemDrop.ItemData>)Delegate.CreateDelegate(typeof(Func<Humanoid, ItemDrop.ItemData>), povWorn);
            _isBbh = (Func<ItemDrop.ItemData, bool>)Delegate.CreateDelegate(typeof(Func<ItemDrop.ItemData, bool>), isQuiver);

            harmony.Patch(AccessTools.Method(typeof(ZNetScene), "Awake"), postfix:
                new HarmonyMethod(typeof(Quivers), nameof(Register)) { priority = Priority.Last, after = new[] { Bbh } });
            harmony.Patch(AccessTools.Method(typeof(Humanoid), "EquipItem"), postfix:
                new HarmonyMethod(typeof(Quivers), nameof(OneQuiver)) { priority = Priority.Last, after = new[] { Bbh } });
            return null;
        }

        private static void Register(ZNetScene __instance)
        {
            foreach (var prefab in (IEnumerable<GameObject>)_prefabs.GetValue(null))
            {
                if (!prefab || !__instance.GetPrefab(prefab.name) || !Registered.Add(prefab.name)) continue;
                EquipmentApi.RegisterQuiver(prefab.name, 3,
                    (player, item) => Worn(player) == item || (player.EquipItem(item) && Worn(player) == item),
                    (player, item) => player.UnequipItem(item),
                    (player, item) => Worn(player) == item);
            }
        }

        private static void OneQuiver(Humanoid __instance, ItemDrop.ItemData item, bool __result)
        {
            if (!__result || item == null || !(__instance is Player player)) return;
            var bbh = Worn(player);
            var pov = _povWorn(player);
            if (bbh == null || pov == null || bbh == pov) return;
            if (bbh == item && _isBbh(item)) player.UnequipItem(pov, false);
            else if (pov == item) player.UnequipItem(bbh, false);
        }

        internal static string PatchVisual(Harmony harmony, ConfigFile config)
        {
            var slot = typeof(PassiveTree.QuiverSlot);
            var vis = AccessTools.Inner(slot, "Vis");
            var table = AccessTools.Field(slot, "shown");
            var shownType = AccessTools.Inner(slot, "Shown");
            var draw = vis == null ? null : AccessTools.Method(vis, "Postfix");
            _shown = table?.GetValue(null);
            _getShown = _shown == null ? null : AccessTools.Method(_shown.GetType(), "TryGetValue");
            _model = shownType == null ? null : AccessTools.Field(shownType, "Go");
            if (draw == null || _getShown == null || _model == null) return "PoV worn quiver visual";

            _view = AccessTools.FieldRefAccess<VisEquipment, ZNetView>("m_nview");
            _position = config.Bind("Quiver", "Body Position", new Vector3(0.08f, 0.22f, 0.005f), "Offset from BackShield_attach in character axes, in metres.");
            _rotation = config.Bind("Quiver", "Body Rotation", new Vector3(-10f, 0f, -20f), "Euler rotation relative to the character.");
            harmony.Patch(draw, postfix: new HarmonyMethod(typeof(Quivers), nameof(Mount)));
            return null;
        }

        private static void Mount(VisEquipment __0)
        {
            var view = _view(__0);
            if (!view || !view.IsValid() || view.GetZDO().GetInt("pt.quiver.item", 0) == 0) return;
            var args = new object[] { __0, null };
            if (!(bool)_getShown.Invoke(_shown, args) || !(_model.GetValue(args[1]) is GameObject model) || !model) return;

            var transform = model.transform;
            var joint = transform.parent;
            if (!joint || joint.name != "BackShield_attach")
            {
                joint = Utils.FindChild(__0.transform, "BackShield_attach");
                if (!joint) return;
                transform.SetParent(joint, true);
            }

            // PoV already aligns the mouth to model-up. The shield joint rotates that axis away from the back.
            var body = __0.transform;
            transform.position = joint.position + body.rotation * _position.Value;
            transform.rotation = body.rotation * Quaternion.Euler(_rotation.Value);
        }
    }
}
