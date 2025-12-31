using HarmonyLib;
using MelonLoader;
using ScheduleOne;
using ScheduleOne.DevUtilities;
using ScheduleOne.Effects;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.Levelling;
using ScheduleOne.Money;
using ScheduleOne.NPCs.CharacterClasses;
using ScheduleOne.ObjectScripts;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Product;
using ScheduleOne.UI;
using ScheduleOne.UI.Items;
using ScheduleOne.UI.Shop;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace SkillTree
{
    /// <summary>
    /// ADD POINTS AT LEVEL UP
    /// </summary>
    [HarmonyPatch(typeof(LevelManager), "IncreaseTier")]
    public static class LevelUp_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Core.Instance != null)
                Core.Instance.AttPoints(true); 
        }
    }

    /// <summary>
    /// CHANGE THE RANK NECESSARY TO UNLOCK
    /// </summary>
    public static class ItemUnlocker
    {
        private static readonly Dictionary<string, FullRank> TargetRanks = new Dictionary<string, FullRank>
        {
            { "moisturepreservingpot",  new FullRank(ERank.Hoodlum, 5) },
            { "ledgrowlight",           new FullRank(ERank.Hoodlum, 3) },
            { "plasticpot",             new FullRank(ERank.Street_Rat, 5) },
            { "halogengrowlight",       new FullRank(ERank.Street_Rat, 5) },
            { "suspensionrack",         new FullRank(ERank.Street_Rat, 5) },
            { "airpot",                 new FullRank(ERank.Hustler, 5) },
            { "cauldron",               new FullRank(ERank.Bagman, 3) },
            { "brickpress",             new FullRank(ERank.Bagman, 5) },
            { "dryingrack",             new FullRank(ERank.Street_Rat, 5) }
        };

        public static void UnlockSpecificItems()
        {
            var registry = Registry.Instance;
            if (registry == null) return;

            List<ItemDefinition> allItems = registry.GetAllItems();
            if (allItems == null) return;

            int patchedCount = 0;

            foreach (var def in allItems)
            {
                if (def == null || string.IsNullOrEmpty(def.ID)) continue;

                string id = def.ID.ToLowerInvariant();

                if (TargetRanks.TryGetValue(id, out FullRank rankAlvo))
                {
                    var storable = def as StorableItemDefinition;
                    if (storable != null)
                    {
                        storable.RequiredRank = rankAlvo;

                        storable.RequiresLevelToPurchase = true;

                        patchedCount++;
                        MelonLogger.Msg($"[SkillTree Unlocker] Item {id} atualizado para Rank: {rankAlvo.Rank}, Tier: {rankAlvo.Tier}");
                    }
                }
            }

            MelonLogger.Msg($"[SkillTree Unlocker] Total de {patchedCount} itens remapeados com sucesso.");
        }
    }

    /// <summary>
    /// ADD ITEMS TO HARDWARE STORES 
    /// </summary>
    [HarmonyPatch(typeof(ShopInterface), "Awake")]
    public static class ShopInjectionPatch
    {
        private static List<string> itemIdsToInject = new List<string>
        {
            "moisturepreservingpot",
            "ledgrowlight",
            "plasticpot",
            "halogengrowlight",
            "suspensionrack",
            "airpot",
            "dryingrack"
        };

        [HarmonyPostfix]
        public static void Postfix(ShopInterface __instance)
        {
            if (__instance.ShopCode.ToLower().Contains("hardware") || __instance.ShopCode.ToLower().Contains("handy_hanks"))
            {
                StorableItemDefinition[] allItems = Resources.FindObjectsOfTypeAll<StorableItemDefinition>();

                foreach (string id in itemIdsToInject)
                {
                    if (__instance.Listings.Exists(x => x.Item != null && x.Item.ID.ToLower() == id))
                        continue;

                    StorableItemDefinition targetItem = System.Array.Find(allItems, x => x.ID.ToLower() == id);

                    if (targetItem != null)
                    {
                        // Cria a instância da classe
                        ShopListing newListing = new ShopListing();
                        newListing.Item = targetItem;

                        var trvListing = Traverse.Create(newListing);

                        if (id == "moisturepreservingpot")
                        {
                            trvListing.Field("OverridePrice").SetValue(true);
                            trvListing.Field("OverriddenPrice").SetValue(125f);
                        }
                        if (id == "ledgrowlight")
                        {
                            trvListing.Field("OverridePrice").SetValue(true);
                            trvListing.Field("OverriddenPrice").SetValue(200f);
                        }
                        if (id == "plasticpot")
                        {
                            trvListing.Field("OverridePrice").SetValue(true);
                            trvListing.Field("OverriddenPrice").SetValue(50f);
                        }
                        if (id == "halogengrowlight")
                        {
                            trvListing.Field("OverridePrice").SetValue(true);
                            trvListing.Field("OverriddenPrice").SetValue(100f);
                        }
                        if (id == "suspensionrack")
                        {
                            trvListing.Field("OverridePrice").SetValue(true);
                            trvListing.Field("OverriddenPrice").SetValue(100f);
                        }
                        if (id == "airpot")
                        {
                            trvListing.Field("OverridePrice").SetValue(true);
                            trvListing.Field("OverriddenPrice").SetValue(300f);
                        }
                        if (id == "dryingrack")
                        {
                            trvListing.Field("OverridePrice").SetValue(true);
                            trvListing.Field("OverriddenPrice").SetValue(400f);
                        }

                        Traverse trv = Traverse.Create(newListing);

                        if (trv.Field("isUnlimitedStock").FieldExists())
                            trv.Field("isUnlimitedStock").SetValue(true);
                        else if (trv.Field("_isUnlimitedStock").FieldExists())
                            trv.Field("_isUnlimitedStock").SetValue(true);

                        __instance.Listings.Add(newListing);

                        newListing.Initialize(__instance);

                        Traverse.Create(__instance).Method("CreateListingUI", new object[] { newListing }).GetValue();

                        MelonLogger.Msg($"[SkillTree Shop] Item injetado com sucesso: {targetItem.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// FIX MOVESPEED OF EFFECTS Athletic AND Energizing
        /// </summary>
        [HarmonyPatch]
        public static class SpeedEffect_SkillHarmony_Patch
        {

            [HarmonyPatch(typeof(Athletic), "ApplyToPlayer")]
            [HarmonyPostfix]
            public static void Athletic_Apply_Postfix()
            {
                float baseWithSkill = SkillPatchStats.PlayerMovespeed.MovespeedBase;
                PlayerSingleton<PlayerMovement>.Instance.MoveSpeedMultiplier = baseWithSkill + 0.3f;
            }

            [HarmonyPatch(typeof(Energizing), "ApplyToPlayer")]
            [HarmonyPostfix]
            public static void Energizing_Apply_Postfix()
            {
                // Pega a base da sua skill e adiciona o boost do Energizing (0.15f)
                float baseWithSkill = SkillPatchStats.PlayerMovespeed.MovespeedBase;
                PlayerSingleton<PlayerMovement>.Instance.MoveSpeedMultiplier = baseWithSkill + 0.15f;
            }

            // --- LIMPEZA (CLEAR) ---
            // Aqui corrigimos o erro do jogo de voltar para 1.0f

            [HarmonyPatch(typeof(Athletic), "ClearFromPlayer")]
            [HarmonyPostfix]
            public static void Athletic_Clear_Postfix()
            {
                PlayerSingleton<PlayerMovement>.Instance.MoveSpeedMultiplier = SkillPatchStats.PlayerMovespeed.MovespeedBase;
            }

            [HarmonyPatch(typeof(Energizing), "ClearFromPlayer")]
            [HarmonyPostfix]
            public static void Energizing_Clear_Postfix()
            {
                PlayerSingleton<PlayerMovement>.Instance.MoveSpeedMultiplier = SkillPatchStats.PlayerMovespeed.MovespeedBase;
            }
        }
    }

    public static class QuickPackagers
    {
        public static bool Add = false;
    }

    [HarmonyPatch]
    public static class RouteExpanderPatch
    {
        [HarmonyPatch(typeof(PackagingStation), "Awake")]
        [HarmonyPostfix]
        public static void Postfix_Speed(PackagingStation __instance)
        {
            if (QuickPackagers.Add)
                __instance.PackagerEmployeeSpeedMultiplier = 2f;
        }
    }

}
