using HarmonyLib;
using MelonLoader;
using ScheduleOne.NPCs.Behaviour;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SkillTree.SkillSpecial.SkillEmployee
{

    public static class BetterBotanist
    {
        public static bool Add = false;
    }

    [HarmonyPatch]
    public static class Patch_Botanist_Speeds
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            return new List<MethodBase>
        {
            AccessTools.Method(typeof(SowSeedInPotBehaviour), "GetActionDuration"),
            AccessTools.Method(typeof(WaterPotBehaviour), "GetActionDuration"),
            AccessTools.Method(typeof(HarvestPotBehaviour), "GetActionDuration"),
            AccessTools.Method(typeof(AddSoilToGrowContainerBehaviour), "GetActionDuration"),
            AccessTools.Method(typeof(ApplyAdditiveToGrowContainerBehaviour), "GetActionDuration"),
            AccessTools.Method(typeof(HarvestMushroomBedBehaviour), "GetActionDuration"),
            AccessTools.Method(typeof(ApplySpawnToMushroomBedBehaviour), "GetActionDuration")
        };
        }

        [HarmonyPostfix]
        public static void Postfix(ref float __result, GrowContainerBehaviour __instance)
        {
            if (BetterBotanist.Add && __result > 0)
            {
                __result /= 2f;
            }
        }
    }
}
