using FishNet;
using FluffyUnderware.Curvy.ThirdParty.LibTessDotNet;
using HarmonyLib;
using MelonLoader;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.Levelling;
using ScheduleOne.Map;
using ScheduleOne.ObjectScripts;
using ScheduleOne.Persistence;
using ScheduleOne.Product;
using ScheduleOne.Property;
using ScheduleOne.UI.Shop;
using ScheduleOne.Variables;
using UnityEngine;
using static MelonLoader.MelonLogger;
using static ScheduleOne.ObjectScripts.Pot;

namespace SkillTree.SkillPatchOperations
{
    /// <summary>
    /// ABSORBENT SOIL
    /// </summary>
    public static class AbsorbentSoil
    {
        public static bool Add = false;
    }


    [HarmonyPatch(typeof(Pot), "OnPlantFullyHarvested")]
    public static class Pot_OnPlantFullyHarvested_Patch
    {
        static bool Prefix(Pot __instance)
        {
            if (!AbsorbentSoil.Add)
                return true; 
            try
            {
                var traverse = Traverse.Create(__instance);

                // === Get Plant (private setter safe) ===
                var plant = traverse.Property("Plant")?.GetValue();
                if (plant == null)
                {
                    MelonLogger.Msg("OnPlantFullyHarvested skipped: Plant is null");
                    return false;
                }

                if (InstanceFinder.IsServer)
                {
                    float value = NetworkSingleton<VariableDatabase>.Instance
                        .GetValue<float>("HarvestedPlantCount");

                    NetworkSingleton<VariableDatabase>.Instance
                        .SetVariableValue("HarvestedPlantCount", (value + 1f).ToString());

                    NetworkSingleton<LevelManager>.Instance.AddXP(5);

                    MelonLogger.Msg("Server harvest processed");
                }

                // Plant = null (private setter)
                traverse.Property("Plant")?.SetValue(null);

                // _remainingSoilUses--
                int remainingUses = traverse.Field("_remainingSoilUses").GetValue<int>() - 1;
                __instance.SetRemainingSoilUses(remainingUses);

                __instance.SetSoilState(ESoilState.Flat);

                if (remainingUses <= 0)
                {
                    MelonLogger.Msg("Soil depleted: clearing soil and additives");

                    traverse.Method("ClearAdditives")?.GetValue();
                    traverse.Method("ClearSoil")?.GetValue();
                }
                else
                {
                    MelonLogger.Msg("Soil still usable: additives preserved");
                }

                return false; // block original
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"OnPlantFullyHarvested patch failed: {ex}");
                return true; // fallback
            }
        }
    }

    

    /// <summary>
    /// INCREASE CAULDRON OUTPUT
    /// </summary>
    public static class CauldronOutputAdd
    {
        public static int Add = 10;
    }

    [HarmonyPatch(typeof(Cauldron), "RpcLogic___FinishCookOperation_2166136261")]
    public static class Cauldron_Finish_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Cauldron __instance)
        {
            // No código original, o jogo faz: CocaineBaseDefinition.GetDefaultInstance(10)
            // Mas o FishNet/Mirror às vezes dificulta mudar variáveis locais.
            // O jeito mais fácil é deixar o original rodar e logo após dobrar o que caiu no slot,
            // OU interceptar a criação do item. 
            // Vamos usar a interceptação da criação por ser mais limpa:
        }
    }

    [HarmonyPatch(typeof(QualityItemDefinition), "GetDefaultInstance", typeof(int))]
    public static class Cauldron_Double_Output_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(QualityItemDefinition __instance, ref int quantity)
        {
            if (CauldronOutputAdd.Add == 10) return;
            // Se a definição for a do CocaineBase (verificamos pelo nome ou ID) 
            // e a quantidade solicitada for 10
            if (__instance.name.Contains("CocaineBase") && quantity == 10)
            {
                quantity = CauldronOutputAdd.Add; // Forçamos o jogo a criar 20 unidades
            }
        }
    }

    /// <summary>
    /// SPEED UP CHEMIST STATIONS
    /// </summary>
    public static class StationTimeLess
    {
        public static float TimeAjust = 1f;
    }

    [HarmonyPatch(typeof(Cauldron), "MinPass")]
    public static class Cauldron_Speed_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(Cauldron __instance)
        {
            if (__instance.RemainingCookTime > 0)
            {
                if (StationTimeLess.TimeAjust > 1f)
                    __instance.RemainingCookTime--;
            }
        }
    }

    [HarmonyPatch(typeof(ChemistryCookOperation), "Progress")]
    public static class ChemistryStation_FastProgress_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(ref int mins)
        {
            // Se queremos que demore metade do tempo, 
            // fazemos cada minuto valer por 2.
            mins = Mathf.RoundToInt(mins * StationTimeLess.TimeAjust);
        }
    }

    [HarmonyPatch(typeof(OvenCookOperation), "GetCookDuration")] 
    public static class CookStation_Duration_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            // Se a duração for válida, entregamos a metade para o jogo
            if (__result > 0)
            {
                // Usamos StationTimeLess.TimeAjust ou apenas / 2
                // Se TimeAjust for 2.0, dividimos por ele.
                __result = Mathf.Max(1, Mathf.RoundToInt(__result / StationTimeLess.TimeAjust));
            }
        }
    }

    /// <summary>
    /// INCREASE MIXSTATION OUTPUT
    /// </summary>
    public static class MixOutputAdd
    {
        public static int Add = 1;
        public static int TimeAjust = 2;
    }

    [HarmonyPatch(typeof(MixingStation))] // Certifique-se que o nome da classe é MixStation
    public static class MixStationPatch
    {
        // 1. Alterar a quantidade de Output para 2x
        [HarmonyPatch("GetMixQuantity")]
        [HarmonyPostfix]
        public static void Postfix(MixingStation __instance, ref int __result)
        {
            // Se o resultado original já era 0, não fazemos nada e não logamos
            if (__result <= 0) return;

            if (__instance.ProductSlot == null || __instance.MixerSlot == null) return;

            // Cálculo limpo para evitar o loop infinito de dobrar o valor
            int qtyProduct = __instance.ProductSlot.Quantity;
            int qtyMixer = __instance.MixerSlot.Quantity;
            int originalMax = Mathf.Min(Mathf.Min(qtyProduct, qtyMixer), __instance.MaxMixQuantity);

            __result = originalMax * MixOutputAdd.Add;
        }
    }

    [HarmonyPatch(typeof(MixingStation), "GetMixTimeForCurrentOperation")]
    public static class MixStation_Time_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(MixingStation __instance, ref int __result)
        {
            if (MixOutputAdd.Add == 1) return;

            // Se não há operação ocorrendo ou o resultado é 0, sai
            if (__instance.CurrentMixOperation == null || __result <= 0) return;

            // Calculamos o tempo baseado na quantidade da operação atual (que já estará dobrada pelo patch acima)
            // E dividimos por 2 para ser mais rápido
            int tempoCalculado = (__instance.MixTimePerItem * __instance.CurrentMixOperation.Quantity) / MixOutputAdd.TimeAjust;

            // Garante que o tempo seja ao menos 1 (para não bugar o timer do jogo)
            __result = Mathf.Max(1, tempoCalculado);
        }
    }

    /// <summary>
    /// CHANGE GROW SPEED
    /// </summary>
    public static class GrowthSpeedUp
    {
        public static float Add = 0f;
    }

    [HarmonyPatch(typeof(Plant), "MinPass")]
    public static class Plant_MinPass_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Plant __instance, int mins)
        {
            if (__instance.NormalizedGrowthProgress >= 1f || NetworkSingleton<TimeManager>.Instance.IsEndOfDay)
                return true; 

            float num = 1f / ((float)__instance.GrowthTime * 60f) * (float)mins;
            num *= __instance.Pot.GetTemperatureGrowthMultiplier();
            num *= __instance.Pot.GetAverageLightExposure(out var growSpeedMultiplier);
            num *= __instance.Pot.GrowSpeedMultiplier;
            num *= growSpeedMultiplier;

            if (GameManager.IS_TUTORIAL)
                num *= 0.3f;

            if (__instance.Pot.NormalizedMoistureAmount <= 0f)
                num *= 0f;

            num *= (1f + GrowthSpeedUp.Add);

            __instance.SetNormalizedGrowthProgress(__instance.NormalizedGrowthProgress + num);

            return false;
        }
    }

    [HarmonyPatch(typeof(ShroomColony), "ChangeGrowthPercentage")]
    public static class MushroomGrowthSpeedPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ShroomColony __instance, ref float change)
        {
            // Change the diference before ADD in percentage
            if (change > 0f && GrowthSpeedUp.Add > 0f)
            {
                //float before = change;
                change += change * GrowthSpeedUp.Add;
                //MelonLogger.Msg($"Mushroom Growth Before: {before} | Now: {change}");
            }
        }
    }

    /// <summary>
    /// CHANGE QUALITY SYSTEM BY POT TYPE -- BETTER POT = BETTER QUALITY
    /// MUSHROOM NOT INCLUDE
    /// </summary>
    /// 

    public static class QualityMushroomUP
    {
        public static float Add = 0f;
    }

    [HarmonyPatch(typeof(ShroomColony), "GetHarvestedShroom")]
    public static class MushroomQualityPatch
    {
        [HarmonyPostfix] 
        public static void Postfix(ShroomColony __instance, ref ShroomInstance __result)
        {
            if (QualityMushroomUP.Add > 0f && __result != null)
            {
                float baseQuality = __instance.NormalizedQuality;

                float boostedQuality = Mathf.Clamp01(baseQuality + QualityMushroomUP.Add);

                __result.SetQuality(ItemQuality.GetQuality(boostedQuality));

                MelonLogger.Msg($"[Quality] Base: {baseQuality} | Boosted: {boostedQuality}");
            }
        }
    }

    public static class QualityUP
    {
        public static float Add = 0f;
    }

    public static class BetterGrowTent
    {
        public static float Add = 0f;
    }

    [HarmonyPatch(typeof(Plant), "Initialize")]
    public static class PlantQualityPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Plant __instance)
        {
            if (__instance.Pot != null)
            {
                string potName = __instance.Pot.Name.ToString();
                float baseQuality = 0.5f; // Valor padrão do jogo
                float currentQuality = __instance.QualityLevel;

                // 1. Definimos a qualidade base de acordo com o pote
                // Usamos os valores que você quer como alvo final
                if (potName.Equals("Grow Tent")) baseQuality = 0.1f + BetterGrowTent.Add;
                else if (potName.Equals("Plastic Pot")) baseQuality = 0.26f;
                else if (potName.Equals("Moisture-Preserving Pot")) baseQuality = 0.26f;
                else if (potName.Equals("Air Pot")) baseQuality = 0.5f;
                else baseQuality = 0.1f; // Padrão para potes desconhecidos/ruins

                // 2. SOMAMOS a sua Skill à base do pote
                // Assim, se for Air Pot (0.8) + Skill (0.2), a planta já nasce com 1.0!
                float finalQuality = baseQuality + QualityUP.Add;

                ///
                if (AbsorbentSoil.Add)
                {
                    if (__instance.Pot == null)
                        MelonLogger.Warning("Plant.Initialize Postfix: Pot is null");
                    else
                    {
                        var additives = __instance.Pot.AppliedAdditives;
                        if (additives == null || additives.Count == 0)
                            MelonLogger.Msg("No initial additives found for instant growth");
                        else
                        {
                            float delta = 0f;
                            foreach (var additive in additives)
                            {
                                if (additive == null)
                                    continue;

                                MelonLogger.Msg("Nome do additivo: " + additive.Name.ToString().ToLower());

                                switch (additive.Name.ToString().ToLower().Trim())
                                {
                                    case "fertilizer":
                                        delta = +0.3f;
                                        break;

                                    case "pgr":
                                        delta = -0.3f;
                                        break;

                                    case "speedgrow":
                                        delta = -0.3f;
                                        break;
                                }


                                finalQuality += delta;
                                MelonLogger.Msg($"[SkillTree] Change Quality {finalQuality} | Additive: {additive.Name.ToString().ToLower().Trim()}");

                                if (additive.InstantGrowth > 0f && __instance.NormalizedGrowthProgress < 0.5f)
                                {
                                    float before = __instance.NormalizedGrowthProgress;

                                    __instance.SetNormalizedGrowthProgress(
                                        before + additive.InstantGrowth
                                    );

                                    MelonLogger.Msg(
                                        $"Instant growth applied: +{additive.InstantGrowth} (from {before} to {__instance.NormalizedGrowthProgress})"
                                    );
                                }

                                if (finalQuality < 0.27f && finalQuality > 0.17f)
                                    finalQuality = 0.27f;
                            }
                        }
                    }
                }

                var traverse = Traverse.Create(__instance);
                traverse.Field("QualityLevel").SetValue(finalQuality);

                traverse.Field("<QualityLevel>k__BackingField").SetValue(finalQuality);

                traverse.Field("_qualityLevel").SetValue(finalQuality);

                MelonLogger.Msg($"[SkillTree] Plant Init: {potName} | Final: {finalQuality} | Skill: {QualityUP.Add} | Total: {__instance.QualityLevel}");
            }
        }
    }

    [HarmonyPatch(typeof(Plant), "Initialize")]
    public static class Plant_Initialize_Patch
    {
        static void Postfix(Plant __instance)
        {
            try
            {
                if (!AbsorbentSoil.Add)
                    return;

                if (__instance.Pot == null)
                {
                    MelonLogger.Warning("Plant.Initialize Postfix: Pot is null");
                    return;
                }

                // ⚠️ Ajuste o nome da coleção se necessário
                var additives = __instance.Pot.AppliedAdditives;
                if (additives == null || additives.Count == 0)
                {
                    MelonLogger.Msg("No initial additives found for instant growth");
                    return;
                }

                foreach (var additive in additives)
                {
                    if (additive == null)
                        continue;

                    if (additive.InstantGrowth > 0f && __instance.NormalizedGrowthProgress < 0.5f)
                    {
                        float before = __instance.NormalizedGrowthProgress;

                        __instance.SetNormalizedGrowthProgress(
                            before + additive.InstantGrowth
                        );

                        MelonLogger.Msg(
                            $"Instant growth applied: +{additive.InstantGrowth} (from {before} to {__instance.NormalizedGrowthProgress})"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Plant.Initialize Postfix failed: {ex}");
            }
        }
    }

    /// <summary>
    /// ADD YIELD FROM PLANTS
    /// </summary>
    public static class YieldAdd
    {
        public static int Add = 0;
    }

    [HarmonyPatch(typeof(Plant), "GrowthDone")]
    public static class GrowthDone_SmartBasePatch
    {
        [HarmonyPrefix]
        public static void Prefix(Plant __instance)
        {
            if (!FishNet.InstanceFinder.IsServer) return;

            var traverse = Traverse.Create(__instance);
            float currentMultiplier = __instance.YieldMultiplier;

            int originalBase; // Valor base da planta
            originalBase = (int)traverse.Field("BaseYieldQuantity").GetValue();

            if (Mathf.Approximately(currentMultiplier, 1.0f) && YieldAdd.Add != 0)
            {
                originalBase =  (int)traverse.Field("BaseYieldQuantity").GetValue();
                int finalBase = originalBase + YieldAdd.Add; 

                traverse.Field("BaseYieldQuantity").SetValue(finalBase);
                MelonLogger.Msg($"[Skill More Yield] No additives detected. Skill applied. New Base: {finalBase}");
            }
            else
                traverse.Field("BaseYieldQuantity").SetValue(12);
        }
    }
}
