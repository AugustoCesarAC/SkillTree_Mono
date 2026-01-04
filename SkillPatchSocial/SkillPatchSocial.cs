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
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SkillTree.SkillPatchSocial
{

    public static class CustomerCache
    {
        public static Dictionary<string, float> OriginalMinSpend = new Dictionary<string, float>();
        public static Dictionary<string, float> OriginalMaxSpend = new Dictionary<string, float>();
        public static bool IsLoaded = false;

        public static void FillCache(List<Customer> customers)
        {
            if (IsLoaded) return; 

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
            MelonLogger.Msg("Customer spending history successfully stored!");
        }
    }

    public static class BusinessCache
    {
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
            MelonLogger.Msg("Business Laundering Memory successfully stored!");
        }
    }

    public static class ATMConfig
    {
        public static float MaxWeeklyLimit = 10000f;
    }

    [HarmonyPatch(typeof(ATMInterface))]
    public static class ATMInterface_UnlockLimit_Patch
    {
        [HarmonyPatch("remainingAllowedDeposit", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool PrefixRemaining(ref float __result)
        {
            __result = Mathf.Max(0f, ATMConfig.MaxWeeklyLimit - ATM.WeeklyDepositSum); 
            return false; 
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void PostfixUpdate(ATMInterface __instance, Button ___menu_DepositButton, Text ___depositLimitText)
        {
            if (!__instance.isOpen) return;

            bool limitReached = ATM.WeeklyDepositSum >= ATMConfig.MaxWeeklyLimit;

            if (___menu_DepositButton != null)
                ___menu_DepositButton.interactable = !limitReached;

            if (___depositLimitText != null)
            {
                ___depositLimitText.text = MoneyManager.FormatAmount(ATM.WeeklyDepositSum) + " / " + MoneyManager.FormatAmount(ATMConfig.MaxWeeklyLimit);
                ___depositLimitText.color = limitReached ? new Color32(255, 75, 75, 255) : Color.white;
            }
        }

        [HarmonyPatch("UpdateAvailableAmounts")]
        [HarmonyPrefix]
        public static bool PrefixUpdateAmounts(ATMInterface __instance, bool ___depositing, List<Button> ___amountButtons)
        {
            if (___depositing)
            {
                float cash = NetworkSingleton<MoneyManager>.Instance.cashBalance;
                float remaining = Mathf.Max(0f, ATMConfig.MaxWeeklyLimit - ATM.WeeklyDepositSum);

                for (int i = 0; i < ATMInterface.amounts.Length; i++)
                {
                    if (i >= ___amountButtons.Count) break;
                    if (i == ___amountButtons.Count - 1)
                        ___amountButtons[i].interactable = cash > 0f && remaining > 0f;
                    else
                    {
                        float amountVal = (float)ATMInterface.amounts[i];
                        ___amountButtons[i].interactable = (cash >= amountVal) && (ATM.WeeklyDepositSum + amountVal <= ATMConfig.MaxWeeklyLimit);
                    }
                }
                return false;
            }
            return true;
        }
    }

    /*[HarmonyPatch(typeof(ATMInterface))]
    public class PatchATMInterface
    {
        [HarmonyPatch("remainingAllowedDeposit", MethodType.Getter)]
        [HarmonyPrefix]
        public static bool PatchRemaining(ref float __result)
        {
            __result = ATMConfig.MaxWeeklyLimit - ScheduleOne.Money.ATM.WeeklyDepositSum;
            return false; 
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void PatchUpdateVisuals(ATMInterface __instance, Text ___depositLimitText)
        {
            if (!__instance.isOpen) return;

            string atual = MoneyManager.FormatAmount(ScheduleOne.Money.ATM.WeeklyDepositSum);
            string limite = MoneyManager.FormatAmount(ATMConfig.MaxWeeklyLimit);

            ___depositLimitText.text = $"{atual} / {limite}";

            if (ScheduleOne.Money.ATM.WeeklyDepositSum >= ATMConfig.MaxWeeklyLimit)
                ___depositLimitText.color = new Color32(255, 75, 75, 255);
            else
                ___depositLimitText.color = Color.white;
        }

        [HarmonyPatch("UpdateAvailableAmounts")]
        [HarmonyPrefix]
        public static bool PatchButtons(ATMInterface __instance, bool ___depositing, List<Button> ___amountButtons)
        {
            float balance = (float)AccessPrivateProperty(__instance, "relevantBalance");

            for (int i = 0; i < ATMInterface.amounts.Length; i++)
            {
                if (___depositing)
                {
                    if (i == ATMInterface.amounts.Length - 1) 
                    {
                        float remaining = ATMConfig.MaxWeeklyLimit - ScheduleOne.Money.ATM.WeeklyDepositSum;
                        ___amountButtons[___amountButtons.Count - 1].interactable = balance > 0f && remaining > 0f;
                    }
                    else 
                    {
                        float valorBotao = (float)ATMInterface.amounts[i];
                        bool temDinheiro = balance >= valorBotao;
                        bool cabeNoLimite = (ScheduleOne.Money.ATM.WeeklyDepositSum + valorBotao) <= ATMConfig.MaxWeeklyLimit;

                        ___amountButtons[i].interactable = temDinheiro && cabeNoLimite;
                    }
                }
            }
            return false; 
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void PatchMenuButton(RectTransform ___activeScreen, RectTransform ___menuScreen, Button ___menu_DepositButton)
        {
            if (___activeScreen == ___menuScreen)
            {
                ___menu_DepositButton.interactable = ScheduleOne.Money.ATM.WeeklyDepositSum < ATMConfig.MaxWeeklyLimit;
            }
        }
        private static object AccessPrivateProperty(object instance, string propName)
        {
            return instance.GetType().GetProperty(propName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(instance);
        }
    }*/

    /// <summary>
    /// UP CUSTOMER SAMPLE
    /// </summary>
    public static class CustomerSample
    {
        public static float AddSampleChance = 0f;
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


    /// <summary>
    /// UP ASSIGN CUSTOMER DEALER
    /// </summary>
    public static class DealerUpCustomer
    {
        public static int MaxCustomer = 8;
    }

    [HarmonyPatch(typeof(DealerManagementApp))]
    public class DealerManagementPatch
    {
        private static int GetDynamicLimit() => DealerUpCustomer.MaxCustomer;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix(DealerManagementApp __instance)
        {
            CheckAndExpandUI(__instance);

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
            CheckAndExpandUI(__instance);

            int currentLimit = GetDynamicLimit();

            if (__instance.CustomerTitleLabel != null)
            {
                __instance.CustomerTitleLabel.text = $"Assigned Customers ({dealer.AssignedCustomers.Count}/{currentLimit})";
            }

            if (__instance.AssignCustomerButton != null)
            {
                __instance.AssignCustomerButton.gameObject.SetActive(dealer.AssignedCustomers.Count < currentLimit);
                __instance.AssignCustomerButton.transform.SetSiblingIndex(1);
            }

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

    /// <summary>
    /// BETTER SUPPLIER
    /// </summary>
    public static class SupplierUp
    {
        public static float SupplierInc = 1;
        public static int SupplierLimit = 10;
    }

    [HarmonyPatch(typeof(PhoneShopInterface))]
    public class PhoneShopGlobalPatch
    {
        [HarmonyPatch("CanConfirmOrder")]
        [HarmonyPostfix]
        public static void CanConfirmOrder_Postfix(PhoneShopInterface __instance, ref bool __result)
        {
            if (__result) return;

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

                if (totalPrice > 0f && totalPrice <= currentOrderLimit && totalCount <= SupplierUp.SupplierLimit)
                {
                    __result = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Supplier), "GetDeadDropLimit")]
    public static class Supplier_GetDeadDropLimit_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Supplier __instance, ref float __result)
        {
            if (SupplierUp.SupplierLimit == 10) return true; 

            __result = __instance.MaxOrderLimit * SupplierUp.SupplierInc;

            return false; 
        }
    }

    /// <summary>
    /// BETTER BUSINESS
    /// </summary>
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

            string pName = Traverse.Create(__instance).Field("propertyName").GetValue<string>() ?? "Business";

            for (int i = 0; i < __instance.LaunderingOperations.Count; i++)
            {
                var op = __instance.LaunderingOperations[i];
                int oldMins = op.minutesSinceStarted;
                op.minutesSinceStarted += mins;

                if (op.minutesSinceStarted < op.completionTime_Minutes)
                {
                    int oldInterval = oldMins / 240;
                    int newInterval = op.minutesSinceStarted / 240;

                    if (newInterval > oldInterval)
                    {
                        float installment = Mathf.Ceil(op.amount / 6f);

                        if (FishNet.InstanceFinder.IsServer)
                        {
                            NetworkSingleton<MoneyManager>.Instance.CreateOnlineTransaction(
                                $"Partial Laundering ({pName})",
                                installment, 1f, string.Empty);

                            MelonLogger.Msg($"[LaunderingMod] Partial payout of {installment} processed for {pName}");
                        }

                        Singleton<NotificationsManager>.Instance.SendNotification(
                            pName,
                            $"<color=#16F01C>{MoneyManager.FormatAmount(installment)}</color> Laundered (Partial)",
                            NetworkSingleton<MoneyManager>.Instance.LaunderingNotificationIcon);
                    }
                }

                if (op.minutesSinceStarted >= op.completionTime_Minutes)
                {
                    op.amount = op.amount / 6f;

                    Traverse.Create(__instance).Method("CompleteOperation", op).GetValue();

                    MelonLogger.Msg($"[LaunderingMod] Operation completed for {pName}. Final installment paid.");
                    i--;
                }
            }

            return false;
        }
    }

}
