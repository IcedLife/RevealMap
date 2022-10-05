using Config;
using GameData.Common;
using GameData.Domains;
using GameData.Domains.Character;
using GameData.Domains.Map;
using GameData.Domains.Organization;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using TaiwuModdingLib.Core.Plugin;

namespace RevealMap
{
    [PluginConfig("开地图", "ReachingFoul", "1.0.0")]
    public class RevealMapPlugin : TaiwuRemakePlugin
    {
        public static bool disableTravelCost;
        public static bool disableTravelTimeForSmallMap;
        public static bool disableTravelTimeForLargeMap;
        public static bool maxViewRange;
        private static TravelRoute route;
        private Harmony harmony;
        public override void Dispose()
        {
            if (this.harmony == null)
                return;
            this.harmony.UnpatchSelf();
        }

        public virtual void OnModSettingUpdate() 
        {
            DomainManager.Mod.GetSetting(this.ModIdStr, "DisableTravelCost", ref RevealMapPlugin.disableTravelCost);
            DomainManager.Mod.GetSetting(this.ModIdStr, "DisableTravelTimeForSmallMap", ref RevealMapPlugin.disableTravelTimeForSmallMap);
            DomainManager.Mod.GetSetting(this.ModIdStr, "DisableTravelTimeForLargeMap", ref RevealMapPlugin.disableTravelTimeForLargeMap);
            DomainManager.Mod.GetSetting(this.ModIdStr, "MaxViewRange", ref RevealMapPlugin.maxViewRange);
        }

        public override void Initialize()
        {
            this.harmony = Harmony.CreateAndPatchAll(typeof(RevealMapPlugin), (string)null);
            DomainManager.Mod.GetSetting(this.ModIdStr, "DisableTravelCost", ref RevealMapPlugin.disableTravelCost);
            DomainManager.Mod.GetSetting(this.ModIdStr, "DisableTravelTimeForSmallMap", ref RevealMapPlugin.disableTravelTimeForSmallMap);
            DomainManager.Mod.GetSetting(this.ModIdStr, "DisableTravelTimeForLargeMap", ref RevealMapPlugin.disableTravelTimeForLargeMap);
            DomainManager.Mod.GetSetting(this.ModIdStr, "MaxViewRange", ref RevealMapPlugin.maxViewRange);
            HarmonyFileLog.Enabled = true;
            FileLog.Reset();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapDomain), "OnLoadedArchiveData")]
        public static void MapDomain_RevealMap_loadArchive_PostFix(MapDomain __instance)
        {
            revealMap(__instance, (DataContext)null);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapDomain), "CreateAllAreas")]
        public static void MapDomain_RevealMap_enterNewWorld_PostFix(MapDomain __instance, DataContext context)
        {
            revealMap(__instance, context);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapDomain), "GetTravelCost")]
        public static void MapDomain_RevealMap_GetTravelCost_postfix(ref CrossAreaMoveInfo __result, MapDomain __instance, short fromAreaId, short toAreaId)
        {
            if (RevealMapPlugin.disableTravelCost)
            {
                __result.MoneyCost = 0;
                __result.AuthorityCost = 0;
            }

            if(RevealMapPlugin.disableTravelTimeForLargeMap)
            {
                bool flag = (int)fromAreaId > (int)toAreaId;
                TravelRouteKey key = new TravelRouteKey(flag ? toAreaId : fromAreaId, flag ? fromAreaId : toAreaId);
                TravelRoute travelRoute;
                if (__instance.TryGetElement_TravelRouteDict(key, out travelRoute))
                {
                    List<short> costList = travelRoute.CostList;
                    for (int i = 0; i < costList.Count; i++)
                    {
                        __instance.GetElement_TravelRouteDict(key).CostList[i] = RevealMapPlugin.route.CostList[i];
                    }
                }
                 
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapDomain), "GetTravelCost")]
        public static void MapDomain_RevealMap_GetTravelCost_prefix(short fromAreaId, short toAreaId, MapDomain __instance)
        {
            if (RevealMapPlugin.disableTravelTimeForLargeMap)
            {
                bool flag = (int)fromAreaId > (int)toAreaId;
                TravelRouteKey key = new TravelRouteKey(flag ? toAreaId : fromAreaId, flag ? fromAreaId : toAreaId);
                TravelRoute travelRoute;
                if (__instance.TryGetElement_TravelRouteDict(key, out travelRoute))
                {
                    RevealMapPlugin.route = new TravelRoute(travelRoute);
                    List<short> costList = __instance.GetElement_TravelRouteDict(key).CostList;
                    for (int i = 0; i < costList.Count; i++)
                    {
                        __instance.GetElement_TravelRouteDict(key).CostList[i] = 0;
                    }
                }
               
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapDomain), "Move", new Type[] { typeof(DataContext), typeof(short), typeof(bool) })]
        public static void MapDomain_RevealMap_noTravelTimeSmallMap(ref bool notCostTime)
        {
            if(RevealMapPlugin.disableTravelTimeForSmallMap)
            {
                notCostTime = true;
            }
        }

        private static void revealMap(MapDomain __instance, DataContext context)
        {
            for (short areaId = 0; areaId < 139; areaId++)
            {
                MapAreaData mapArea = __instance.GetElement_Areas((int)areaId);
                mapArea.Discovered = true;
                Span<MapBlockData> areaBlocks = __instance.GetAreaBlocks(areaId);
                foreach (MapBlockData block in areaBlocks)
                {
                    if(block != null && RevealMapPlugin.maxViewRange)
                    {
                        MapBlockItem config = block.GetConfig();
                        config.ViewRange = sbyte.MaxValue;
                    }
                    if (block != null && !block.Visible)
                    {
                        block.SetVisible(true, context);
                    }
                }

            }
        }



    }
}