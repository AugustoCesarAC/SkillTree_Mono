using HarmonyLib;
using MelonLoader;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Money;
using ScheduleOne.Property;
using ScheduleOne.UI;
using ScheduleOne.UI.ATM;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Phone.Messages;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SkillTree.SkillPatchSocial
{

    public static class CustomerCache
    {
        // O Dictionary guarda o ID/Nome do ScriptableObject e o valor original
        public static Dictionary<string, float> OriginalMinSpend = new Dictionary<string, float>();
        public static Dictionary<string, float> OriginalMaxSpend = new Dictionary<string, float>();
        public static bool IsLoaded = false;

        public static void FillCache(List<Customer> customers)
        {
            if (IsLoaded) return; // Só preenche uma vez por sessão

            foreach (var c in customers)
            {
                string key = c.CustomerData.name;
                if (!OriginalMinSpend.ContainsKey(key))
                {
                    OriginalMinSpend.Add(key, c.CustomerData.MinWeeklySpend);
                    OriginalMaxSpend.Add(key, c.CustomerData.MaxWeeklySpend);
                }
            }
            IsLoaded = true;
            MelonLogger.Msg("Memória de gastos dos clientes armazenada com sucesso!");
        }
    }

    public static class BusinessCache
    {
        // O Dictionary guarda o ID/Nome do ScriptableObject e o valor original
        public static Dictionary<string, float> LaunderCapacity = new Dictionary<string, float>();
        public static bool IsLoaded = false;

        public static void FillCache(List<Business> business)
        {
            if (IsLoaded) return; 

            foreach (var c in business)
            {
                string key = c.PropertyName;
                if (!LaunderCapacity.ContainsKey(key))
                    LaunderCapacity.Add(key, c.LaunderCapacity);
            }
            IsLoaded = true;
            MelonLogger.Msg("Memória de Laundering dos Business armazenada com sucesso!");
        }
    }

    // BASE VALUES
    public static class ATMConfig
    {
        public static float MaxWeeklyLimit = 10000f;
    }
    public static class CustomerSample
    {
        public static float AddSampleChance = 0f;
    }
    public static class DealerUpCustomer
    {
        public static int MaxCustomer = 8;
    }
    public static class SupplierUp
    {
        public static float SupplierInc = 1;
        public static int SupplierLimit = 10;
    }
    // BASE VALUES

    [HarmonyPatch(typeof(ATMInterface))]
    public class PatchATMInterface
    {
        // 1. Corrige o cálculo interno de quanto ainda pode depositar
        [HarmonyPatch("remainingAllowedDeposit", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool PatchRemaining(ref float __result)
        {
            __result = ATMConfig.MaxWeeklyLimit - ScheduleOne.Money.ATM.WeeklyDepositSum;
            return false; // Ignora o original (10000 - sum)
        }

        // 2. Corrige o texto visual "SOMA / 10000" no Update
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void PatchUpdateVisuals(ATMInterface __instance, Text ___depositLimitText)
        {
            if (!__instance.isOpen) return;

            // Sobrescreve o texto que o Update original acabou de colocar
            string atual = MoneyManager.FormatAmount(ScheduleOne.Money.ATM.WeeklyDepositSum);
            string limite = MoneyManager.FormatAmount(ATMConfig.MaxWeeklyLimit);

            ___depositLimitText.text = $"{atual} / {limite}";

            // Corrige a cor para vermelho se atingir o NOVO limite
            if (ScheduleOne.Money.ATM.WeeklyDepositSum >= ATMConfig.MaxWeeklyLimit)
                ___depositLimitText.color = new Color32(255, 75, 75, 255);
            else
                ___depositLimitText.color = Color.white;
        }

        // 3. Corrige a validação dos botões (UpdateAvailableAmounts)
        [HarmonyPatch("UpdateAvailableAmounts")]
        [HarmonyPrefix]
        public static bool PatchButtons(ATMInterface __instance, bool ___depositing, List<Button> ___amountButtons)
        {
            float balance = (float)AccessPrivateProperty(__instance, "relevantBalance");

            for (int i = 0; i < ATMInterface.amounts.Length; i++)
            {
                if (___depositing)
                {
                    if (i == ATMInterface.amounts.Length - 1) // Botão MAX
                    {
                        float remaining = ATMConfig.MaxWeeklyLimit - ScheduleOne.Money.ATM.WeeklyDepositSum;
                        ___amountButtons[___amountButtons.Count - 1].interactable = balance > 0f && remaining > 0f;
                    }
                    else // Botões fixos (20, 50, etc)
                    {
                        float valorBotao = (float)ATMInterface.amounts[i];
                        bool temDinheiro = balance >= valorBotao;
                        bool cabeNoLimite = (ScheduleOne.Money.ATM.WeeklyDepositSum + valorBotao) <= ATMConfig.MaxWeeklyLimit;

                        ___amountButtons[i].interactable = temDinheiro && cabeNoLimite;
                    }
                }
            }
            return false; // Ignora a lógica original que usa 10000f hardcoded
        }

        // 4. Bloqueia o botão "Deposit" no menu principal se atingir o limite
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void PatchMenuButton(RectTransform ___activeScreen, RectTransform ___menuScreen, Button ___menu_DepositButton)
        {
            if (___activeScreen == ___menuScreen)
            {
                ___menu_DepositButton.interactable = ScheduleOne.Money.ATM.WeeklyDepositSum < ATMConfig.MaxWeeklyLimit;
            }
        }

        // Helper para acessar a propriedade privada 'relevantBalance'
        private static object AccessPrivateProperty(object instance, string propName)
        {
            return instance.GetType().GetProperty(propName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(instance);
        }
    }

    [HarmonyPatch(typeof(Customer), "GetSampleSuccess")]
    public class PatchSampleSuccessUI
    {
        private static int _depth = 0;
        [HarmonyPrefix]
        public static void Prefix()
        {
            _depth++;
        }

        [HarmonyPostfix]
        public static void Postfix(ref float __result, float __state)
        {
            if (_depth == 1)
            {
                if (CustomerSample.AddSampleChance <= 0) return;

                float origin = __result;

                __result = Mathf.Clamp(__result + CustomerSample.AddSampleChance, 0f, 1f);
                MelonLogger.Msg($"[Skill] Chance de Sample alterada de: {origin:P0} para {__result:P0}");
            }
            _depth--;
        }
    }

    [HarmonyPatch(typeof(DealerManagementApp))]
    public class DealerManagementPatch
    {
        // Helper method to always get the most recent limit
        private static int GetDynamicLimit() => DealerUpCustomer.MaxCustomer;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix(DealerManagementApp __instance)
        {
            // 1. Initial expansion of the UI slots
            CheckAndExpandUI(__instance);

            // 2. Reposition the "Assign Customer" button to the top
            if (__instance.AssignCustomerButton != null)
            {
                __instance.AssignCustomerButton.transform.SetSiblingIndex(1);
            }

            if (__instance.CustomerTitleLabel != null)
            {
                __instance.CustomerTitleLabel.transform.SetAsFirstSibling();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.Content);
        }

        [HarmonyPatch("SetDisplayedDealer")]
        [HarmonyPostfix]
        public static void SetDisplayedDealer_Postfix(DealerManagementApp __instance, Dealer dealer)
        {
            // 3. Check for new upgrades and expand UI if necessary
            CheckAndExpandUI(__instance);

            int currentLimit = GetDynamicLimit();

            // 4. Update the UI text with the dynamic limit
            if (__instance.CustomerTitleLabel != null)
            {
                __instance.CustomerTitleLabel.text = $"Assigned Customers ({dealer.AssignedCustomers.Count}/{currentLimit})";
            }

            // 5. Keep the assign button active and in the correct position
            if (__instance.AssignCustomerButton != null)
            {
                __instance.AssignCustomerButton.gameObject.SetActive(dealer.AssignedCustomers.Count < currentLimit);
                __instance.AssignCustomerButton.transform.SetSiblingIndex(1);
            }

            // 6. Handle all slots (original 8 + modded ones)
            // Starting from 0 to ensure all slots are updated and correctly positioned
            for (int j = 0; j < __instance.CustomerEntries.Length; j++)
            {
                if (dealer.AssignedCustomers.Count > j)
                {
                    Customer customer = dealer.AssignedCustomers[j];
                    RectTransform entry = __instance.CustomerEntries[j];

                    entry.Find("Mugshot").GetComponent<Image>().sprite = customer.NPC.MugshotSprite;
                    entry.Find("Name").GetComponent<Text>().text = customer.NPC.fullName;

                    Button removeBtn = entry.Find("Remove").GetComponent<Button>();
                    removeBtn.onClick.RemoveAllListeners();
                    removeBtn.onClick.AddListener(() => {
                        dealer.SendRemoveCustomer(customer.NPC.ID);
                        __instance.SetDisplayedDealer(dealer);
                    });

                    entry.gameObject.SetActive(true);
                }
                else
                {
                    __instance.CustomerEntries[j].gameObject.SetActive(false);
                }
            }
        }

        private static void CheckAndExpandUI(DealerManagementApp __instance)
        {
            int targetLimit = GetDynamicLimit();

            // Dynamically create clones if the upgrade increased MaxCustomer
            if (__instance.CustomerEntries.Length < targetLimit)
            {
                List<RectTransform> entriesList = __instance.CustomerEntries.ToList();
                RectTransform template = entriesList[0];
                Transform listParent = template.parent;

                while (entriesList.Count < targetLimit)
                {
                    RectTransform newSlot = GameObject.Instantiate(template, listParent);
                    newSlot.name = "CustomerEntry_Mod_Slot_" + entriesList.Count;
                    entriesList.Add(newSlot);
                }

                __instance.CustomerEntries = entriesList.ToArray();
                LayoutRebuilder.ForceRebuildLayoutImmediate(__instance.Content);
            }
        }
    }

    [HarmonyPatch(typeof(PhoneShopInterface))]
    public class PhoneShopGlobalPatch
    {
        [HarmonyPatch("Open")]
        [HarmonyPrefix]
        public static void Open_Prefix(ref float _orderLimit)
        {
            _orderLimit *= SupplierUp.SupplierInc;
        }

        [HarmonyPatch("CanConfirmOrder")]
        [HarmonyPostfix]
        public static void CanConfirmOrder_Postfix(PhoneShopInterface __instance, ref bool __result)
        {
            if (__result) return;

            // Acessamos o carrinho privado e o limite de ordem via Reflection
            var cartField = typeof(PhoneShopInterface).GetField("_cart", BindingFlags.NonPublic | BindingFlags.Instance);
            var limitField = typeof(PhoneShopInterface).GetField("orderLimit", BindingFlags.NonPublic | BindingFlags.Instance);

            if (cartField != null && limitField != null)
            {
                var cart = (IEnumerable<PhoneShopInterface.CartEntry>)cartField.GetValue(__instance);
                float currentOrderLimit = (float)limitField.GetValue(__instance);

                int totalCount = 0;
                float totalPrice = 0f;

                foreach (var entry in cart)
                {
                    totalCount += entry.Quantity;
                    totalPrice += entry.Listing.Price * entry.Quantity;
                }

                // Nova regra de validação:
                // Preço dentro do limite (que já foi aumentado no Open) E itens até 20
                if (totalPrice > 0f && totalPrice <= currentOrderLimit && totalCount <= SupplierUp.SupplierLimit)
                {
                    __result = true;
                }
            }
        }
    }

    public static class BetterBusiness
    {
        public static float Add = 0f;
    }

    [HarmonyPatch]
    public static class BusinessLaunderingPatch
    {
        // Handles the progression of minutes and partial payments every 4 hours (240 mins)
        [HarmonyPatch(typeof(Business), "MinsPass")]
        [HarmonyPrefix]
        public static bool Prefix_MinsPass(Business __instance, int mins)
        {

            // Accessing protected field 'propertyName' via Traverse
            string pName = Traverse.Create(__instance).Field("propertyName").GetValue<string>() ?? "Business";

            for (int i = 0; i < __instance.LaunderingOperations.Count; i++)
            {
                var op = __instance.LaunderingOperations[i];
                int oldMins = op.minutesSinceStarted;
                op.minutesSinceStarted += mins;

                // Check if it's not the final completion yet
                if (op.minutesSinceStarted < op.completionTime_Minutes)
                {
                    int oldInterval = oldMins / 240;
                    int newInterval = op.minutesSinceStarted / 240;

                    // If a new 4-hour window (240 mins) is reached
                    if (newInterval > oldInterval)
                    {
                        // Calculate installment (1/6th of total for a 24h cycle)
                        float installment = op.amount / 6f;

                        if (FishNet.InstanceFinder.IsServer)
                        {
                            // Create partial transaction on the server
                            NetworkSingleton<MoneyManager>.Instance.CreateOnlineTransaction(
                                $"Partial Laundering ({pName})",
                                installment, 1f, string.Empty);

                            MelonLogger.Msg($"[LaunderingMod] Partial payout of {installment} processed for {pName}");
                        }

                        // Send UI notification to the player
                        Singleton<NotificationsManager>.Instance.SendNotification(
                            pName,
                            $"<color=#16F01C>{MoneyManager.FormatAmount(installment)}</color> Laundered (Partial)",
                            NetworkSingleton<MoneyManager>.Instance.LaunderingNotificationIcon);
                    }
                }

                // Handle total operation completion (1440 mins reached)
                if (op.minutesSinceStarted >= op.completionTime_Minutes)
                {
                    // Adjust amount to the last remaining installment
                    op.amount = op.amount / 6f;

                    // CORRECTED: Use .Method().GetValue() to invoke a method that returns void/value
                    Traverse.Create(__instance).Method("CompleteOperation", op).GetValue();

                    MelonLogger.Msg($"[LaunderingMod] Operation completed for {pName}. Final installment paid.");
                    i--;
                }
            }

            // Skip the original method to avoid double-processing
            return false;
        }
    }

}
