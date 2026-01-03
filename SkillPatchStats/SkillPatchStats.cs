using HarmonyLib;
using MelonLoader;
using Mono.Unix.Native;
using ScheduleOne;
using ScheduleOne.Delivery;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.Growing;
using ScheduleOne.Interaction;
using ScheduleOne.Levelling;
using ScheduleOne.Messaging;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.CharacterClasses;
using ScheduleOne.ObjectScripts;
using ScheduleOne.ObjectScripts.Cash;
using ScheduleOne.PlayerScripts.Health;
using ScheduleOne.Product;
using ScheduleOne.Quests;
using ScheduleOne.Tools;
using ScheduleOne.UI.Phone;
using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MelonLoader.MelonLogger;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace SkillTree.SkillPatchStats
{
    // BASE VALUES
    public static class PlayerMovespeed
    {
        public static float MovespeedBase = 1f;
    }
    public static class PlayerXpMoney
    {
        public static bool XpMoney = false;
    }
    // BASE VALUES

    /// <summary>
    /// CHANGE HEALTH BASE
    /// </summary>
    public static class PlayerHealthConfig
    {
        public static float MaxHealth = 100f;
    }

    [HarmonyPatch(typeof(PlayerHealth))]
    public class PatchPlayerHealth
    {
        [HarmonyPatch("SetHealth")]
        [HarmonyPrefix]
        public static bool Prefix_SetHealth(PlayerHealth __instance, float health)
        {
            float clamped = Mathf.Clamp(health, 0f, PlayerHealthConfig.MaxHealth);
            SetInternalHealth(__instance, clamped);
            return false;
        }

        [HarmonyPatch("RecoverHealth")]
        [HarmonyPrefix]
        public static bool Prefix_RecoverHealth(PlayerHealth __instance, float recovery)
        {
            if (__instance.CurrentHealth <= 0f) return false;

            float novaVida = Mathf.Clamp(__instance.CurrentHealth + recovery, 0f, PlayerHealthConfig.MaxHealth);
            SetInternalHealth(__instance, novaVida);
            return false;
        }

        [HarmonyPatch("RpcLogic___TakeDamage_3505310624")]
        [HarmonyPrefix]
        public static bool Prefix_TakeDamage(PlayerHealth __instance, float damage)
        {
            if (!__instance.IsAlive || !__instance.CanTakeDamage) return false;

            float novaVida = Mathf.Clamp(__instance.CurrentHealth - damage, 0f, PlayerHealthConfig.MaxHealth);
            SetInternalHealth(__instance, novaVida);

            // O resto da lógica original de morte e câmera
            AccessTools.Field(typeof(PlayerHealth), "TimeSinceLastDamage").SetValue(__instance, 0f);
            if (novaVida <= 0f) __instance.SendDie();

            return false;
        }

        private static void SetInternalHealth(PlayerHealth instance, float value)
        {
            var field = AccessTools.Field(typeof(PlayerHealth), "<CurrentHealth>k__BackingField");
            field?.SetValue(instance, value);

            instance.onHealthChanged?.Invoke(value);
        }
    }


    /// <summary>
    /// INCREASE XP GAIN
    /// </summary>
    public static class PlayerXPConfig
    {
        public static float XpBase = 100f;
        public static float XpBase2 = 100f;
    }

    [HarmonyPatch(typeof(LevelManager), "AddXP")]
    public class PatchLevelManager
    {
        private static bool _jaProcessado = false;
        [HarmonyPrefix]
        public static void Prefix(LevelManager __instance, ref int xp)
        {
            if (_jaProcessado)
                return;

            float multiplicador = PlayerXPConfig.XpBase / 100f;

            if (multiplicador != 1.0f)
            {
                _jaProcessado = true;
                int xpOriginal = xp;
                xp = Mathf.CeilToInt(xp * multiplicador);
                MelonLogger.Msg($"[XP] Apply: {xpOriginal} -> {xp} (Base: {PlayerXPConfig.XpBase}%)");
                MelonLogger.Msg("Total XP Now: " + (__instance.TotalXP + xp));
            }
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            _jaProcessado = false;
        }
    }

    /// <summary>
    /// MORE XP BASE IN DEAL PAYMENTS
    /// </summary>
    [HarmonyPatch(typeof(Contract), "SubmitPayment")]
    public class PatchContractPayment
    {
        private static bool _jaProcessado = false;
        [HarmonyPrefix]
        public static void Prefix(Contract __instance, float bonusTotal)
        {
            if (_jaProcessado)
                return;

            if (!PlayerXpMoney.XpMoney)
                return;

            float valorTotalDinheiro = __instance.Payment + bonusTotal;

            if (valorTotalDinheiro > 0)
            {
                int xpGanhaPeloDinheiro = Mathf.RoundToInt(valorTotalDinheiro * 0.05f);

                if (xpGanhaPeloDinheiro > 0)
                {
                    LevelManager levelManager = LevelManager.Instance;

                    if (levelManager != null)
                    {
                        MelonLogger.Msg($"[Contract] Payment of ${valorTotalDinheiro} converted into {xpGanhaPeloDinheiro} base XP.");

                        levelManager.AddXP(xpGanhaPeloDinheiro);
                        _jaProcessado = true;
                    }
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            _jaProcessado = false;
        }
    }

    /// <summary>
    /// SLEEP SYSTEM
    /// </summary>
    public static class SkipSchedule
    {
        public static bool Add = false;
    }
    public static class AllowSleepAthEne
    {
        public static bool Add = false;
    }

    public static class ScheduleLogic
    {
        private static int lastDayUsed = -1;

        public static bool CanUseBedSkill()
        {
            int currentDay = (int)NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.CurrentDay;
            return currentDay != lastDayUsed;
        }

        public static string GetTimeRemaining(float currentTime)
        {
            int next = GetNextSchedule();
            if (next == 0) next = 2400;

            int currentTotalMin = ((int)currentTime / 100 * 60) + ((int)currentTime % 100);
            int nextTotalMin = (next / 100 * 60) + (next % 100);

            int diff = nextTotalMin - currentTotalMin;
            int h = diff / 60;
            int m = diff % 60;

            return $"{h:00}h {m:00}m";
        }

        public static int GetNextSchedule()
        {
            float time = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.CurrentTime;

            if (time >= 700 && time < 1200) return 1200;
            if (time >= 1203 && time < 1800) return 1800;
            if (time >= 1803 && time < 2357) return 2357;

            return (int)time;
        }

        [HarmonyPatch(typeof(Bed), "CanSleep")]
        public static class Bed_AlwaysAllow
        {
            [HarmonyPrefix]
            public static bool Prefix(ref bool __result)
            {
                float currentTime = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.CurrentTime;

                if (!AllowSleepAthEne.Add)
                    return true;

                if (currentTime > 700 && currentTime < 1800 && !SkipSchedule.Add)
                    return true;

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Bed), "Hovered")]
        public static class Bed_Hovered_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Bed __instance, ref InteractableObject ___intObj)
            {
                if (!SkipSchedule.Add)
                    return true;

                if (Singleton<ManagementClipboard>.Instance.IsEquipped || __instance.AssignedEmployee != null)
                    return true;

                float currentTime = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.CurrentTime;

                if (currentTime >= 0 && currentTime < 700)
                    return true;
                else if (!CanUseBedSkill() && currentTime <= 1800)
                {
                    ___intObj.SetMessage("You've already rested today! Use it only tomorrow.");
                }
                else if (CanUseBedSkill() && currentTime < 2357)
                {
                    string remaining = ScheduleLogic.GetTimeRemaining(currentTime);
                    ___intObj.SetMessage($"Next Shift in: {remaining}");
                }
                else
                    return true;

                return false;
            }
        }

        [HarmonyPatch(typeof(Bed), "Interacted")]
        public static class Bed_Interacted_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (!SkipSchedule.Add)
                    return true;

                if (!CanUseBedSkill())
                {
                    MelonLogger.Msg("[BedSkill] You've already rested today! Use it only tomorrow.");
                    return true;
                }

                float currentTime = NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.CurrentTime;

                if (currentTime >= 700)
                {
                    int nextTarget = GetNextSchedule();

                    int totalMinutesPassed = (int)(CalculateMinutesBetween(currentTime, (float)nextTarget)) / 3;

                    if (totalMinutesPassed > 0)
                    {
                        foreach (GrowContainer container in UnityEngine.Object.FindObjectsOfType<GrowContainer>())
                            AccessTools.Method(typeof(GrowContainer), "DrainMoisture")?.Invoke(container, new object[] { totalMinutesPassed * 3 });
                        foreach (Plant plant in UnityEngine.Object.FindObjectsOfType<Plant>())
                            plant.MinPass((int)(totalMinutesPassed));
                    }

                    lastDayUsed = (int)NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.CurrentDay;

                    NetworkSingleton<ScheduleOne.GameTime.TimeManager>.Instance.SetTime(nextTarget);

                    MelonLogger.Msg($"[BedSkill] Interação detectada. Próximo schedule definido para: {nextTarget}");
                    return false;
                }
                return true;
            }
        }

        private static int CalculateMinutesBetween(float start, float end)
        {
            if (end == 0) end = 2400;

            int startHours = (int)start / 100;
            int startMins = (int)start % 100;
            int endHours = (int)end / 100;
            int endMins = (int)end % 100;

            int startTotal = (startHours * 60) + startMins;
            int endTotal = (endHours * 60) + endMins;

            return endTotal - startTotal;
        }
    }

    /// <summary>
    /// COUNTER OFFER 
    /// </summary>
    public static class CounterofferHelper
    {
        public static bool Counteroffer = false;

        public static float CalculateSuccessChance(CounterofferInterface instance)
        {
            var conversation = Traverse.Create(instance).Field<MSGConversation>("conversation").Value; 
            var price = Traverse.Create(instance).Field<float>("price").Value; 
            var product = Traverse.Create(instance).Field<ProductDefinition>("selectedProduct").Value; 
            var quantity = Traverse.Create(instance).Field<int>("quantity").Value;

            Customer customer = conversation.sender.GetComponent<Customer>(); 
            CustomerData customerData = customer.CustomerData; 
            NPC NPC = customer.NPC;

            float adjustedWeeklySpend = customerData.GetAdjustedWeeklySpend(NPC.RelationData.RelationDelta / 5f);

            List<EDay> orderDays = customerData.GetOrderDays(customer.CurrentAddiction, NPC.RelationData.RelationDelta / 5f);
            float num = adjustedWeeklySpend / (float)orderDays.Count;

            if (price >= num * 3f) 
                return 0f;

            float valueProposition = Customer.GetValueProposition(Registry.GetItem<ProductDefinition>(customer.OfferedContractInfo.Products.entries[0].ProductID), 
                                    customer.OfferedContractInfo.Payment / (float)customer.OfferedContractInfo.Products.entries[0].Quantity);

            float productEnjoyment = customer.GetProductEnjoyment(product, customerData.Standards.GetCorrespondingQuality());

            float num2 = Mathf.InverseLerp(-1f, 1f, productEnjoyment);

            float valueProposition2 = Customer.GetValueProposition(product, price / (float)quantity);

            float num3 = Mathf.Pow((float)quantity / (float)customer.OfferedContractInfo.Products.entries[0].Quantity, 0.6f);

            float num4 = Mathf.Lerp(0f, 2f, num3 * 0.5f); float num5 = Mathf.Lerp(1f, 0f, Mathf.Abs(num4 - 1f));

            if (valueProposition2 * num5 > valueProposition)
                return 1f;

            if (valueProposition2 < 0.12f) 
                return 0f;

            float num6 = productEnjoyment * valueProposition;
            float num7 = num2 * num5 * valueProposition2;
            if (num7 > num6) 
              return 1f;

            float num8 = num6 - num7;
            float num9 = Mathf.Lerp(0f, 1f, num8 / 0.2f);
            float t = Mathf.Max(customer.CurrentAddiction, NPC.RelationData.NormalizedRelationDelta);
            float num10 = Mathf.Lerp(0f, 0.2f, t);

            if (num9 <= num10) 
                return 1f;

            if (num9 - num10 >= 0.9f) 
                return 0f;

            float probability = (0.9f + num10 - num9) / 0.9f;
            return Mathf.Clamp(probability, 0f, 1f);
        }

        public static Text SuccessLabel;

        public static void CreateSuccessLabel(CounterofferInterface instance)
        {
            if (SuccessLabel != null)
                return;

            var fairLabel = Traverse.Create(instance)
                .Field<Text>("FairPriceLabel").Value;


            var parent = fairLabel.transform.parent;

            var go = UnityEngine.Object.Instantiate(
                fairLabel.gameObject,
                parent
            );

            go.name = "SuccessChanceLabel";

            SuccessLabel = go.GetComponent<Text>();
            SuccessLabel.font = fairLabel.font;
            SuccessLabel.fontSize = fairLabel.fontSize;
            SuccessLabel.fontStyle = FontStyle.Bold;
            SuccessLabel.alignment = fairLabel.alignment;
            SuccessLabel.color = Color.black;
            SuccessLabel.supportRichText = true;
            SuccessLabel.enabled = true;
            SuccessLabel.text = "Success chance: --%";

            var layout = go.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;

            RectTransform fairRT = fairLabel.rectTransform;
            RectTransform rt = SuccessLabel.rectTransform;

            rt.anchorMin = fairRT.anchorMin;
            rt.anchorMax = fairRT.anchorMax;
            rt.pivot = fairRT.pivot;
            rt.sizeDelta = fairRT.sizeDelta;

            rt.anchoredPosition = fairRT.anchoredPosition + new Vector2(0f, -23f);

            go.transform.SetAsLastSibling();
            //MelonLogger.Msg("SuccessChanceLabel visível abaixo do FairPrice");

        }

        public static void UpdateSuccessLabel(CounterofferInterface instance)
        {
            if (SuccessLabel == null)
                return;

            float chance = CounterofferHelper.CalculateSuccessChance(instance);
            //MelonLogger.Msg($"CalculateSuccessChance {chance}");

            string color =
                chance >= 0.75f ? "#4CAF50" :
                chance >= 0.4f ? "#FFC107" :
                "#F44336";

            SuccessLabel.text =
                $"<color={color}>Success chance: {(chance * 100f):0}%</color>";
            //MelonLogger.Msg($"SuccessLabel.text {SuccessLabel.text}");
        }

        [HarmonyPatch(typeof(CounterofferInterface), "Open")]
        public static class Counteroffer_Open_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(CounterofferInterface __instance)
            {
                if (!Counteroffer)
                    return;

                CreateSuccessLabel(__instance);
                UpdateSuccessLabel(__instance);
            }
        }

        [HarmonyPatch(typeof(CounterofferInterface), "ChangeQuantity")]
        public static class Counteroffer_ChangeQuantity_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(CounterofferInterface __instance)
            {
                if (!Counteroffer)
                    return;

                UpdateSuccessLabel(__instance);
            }
        }

        [HarmonyPatch(typeof(CounterofferInterface), "ChangePrice")]
        public static class Counteroffer_ChangePrice_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(CounterofferInterface __instance)
            {
                if (!Counteroffer)
                    return;

                UpdateSuccessLabel(__instance);
            }
        }

    }

    /// <summary>
    /// BETTER DELIVERY
    /// </summary>
    public static class BetterDelivery
    {
        public static bool Add = false;
    }

    [HarmonyPatch(typeof(DeliveryManager), "SendDelivery")]
    public static class DeliveryTime_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(ref DeliveryInstance delivery)
        {
            if (!BetterDelivery.Add)
                return;

            if (delivery == null) return;

            if (delivery.TimeUntilArrival <= 120)
                return;

            int originalTime = delivery.TimeUntilArrival;

            float ratio = Mathf.InverseLerp(60f, 360f, (float)originalTime);

            int newTime = Mathf.RoundToInt(Mathf.Lerp(30f, 120f, ratio));

            delivery.TimeUntilArrival = newTime;

            MelonLogger.Msg($"[DeliverySkill] Delivery scaling adjusted. Original: {originalTime}m | New: {newTime}m");
        }
    }

}