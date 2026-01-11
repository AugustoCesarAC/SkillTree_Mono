using HarmonyLib;
using MelonLoader;
using ScheduleOne.DevUtilities;
using ScheduleOne.Economy;
using ScheduleOne.Employees;
using ScheduleOne.GameTime;
using ScheduleOne.Management;
using ScheduleOne.Money;
using ScheduleOne.PlayerScripts;
using ScheduleOne.PlayerScripts.Health;
using ScheduleOne.Property;
using ScheduleOne.Tools;
using ScheduleOne.Trash;
using ScheduleOne.UI;
using ScheduleOne.UI.Management;
using SkillTree.Json;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ScheduleOne.UI.Items.FilterConfigPanel.SearchCategory;
using static ScheduleOne.UI.MainMenu.MainMenuPopup;

namespace SkillTree.SkillActive
{
    public static class SkillActive
    {
        private static Player localPlayer;
        private static TrashManager trashManager;
        private static TimeManager timeManager;
        private static Dealer[] dealerList;

        private static int currentDay = -1;

        public static bool clearTrashUsed = false;
        public static bool healUsed = false;
        public static bool getCashUsed = false;

        public static void ValidSkill()
        {
            if (timeManager == null) 
                timeManager = TimeManager.Instance;

            if (currentDay != (int)timeManager.CurrentDay)
            {
                clearTrashUsed = false;
                healUsed = false;
                getCashUsed = false;
                currentDay = (int)timeManager.CurrentDay;
            }
        }

        public static class SkillEnabled
        {
            public static bool enabledTrash = false;
            public static bool enabledHeal = false;
            public static bool enabledGetCash = false;
        }

        public static void ClearTrash()
        {
            if(clearTrashUsed)
                Singleton<NotificationsManager>.Instance.SendNotification(
                                "ClearTrash in Cooldown",
                                $"<color=#FF0000>Wait one day</color>",
                                NetworkSingleton<MoneyManager>.Instance.LaunderingNotificationIcon);
            else
            {
                trashManager = TrashManager.Instance;
                trashManager.DestroyAllTrash();
                Singleton<NotificationsManager>.Instance.SendNotification(
                                "ClearTrash",
                                $"All trash clear",
                                NetworkSingleton<MoneyManager>.Instance.LaunderingNotificationIcon);
                clearTrashUsed = true;
            }

        }

        public static void Heal()
        {
            if(healUsed)
                Singleton<NotificationsManager>.Instance.SendNotification(
                                "Heal in Cooldown",
                                $"<color=#FF0000>Wait one day</color>",
                                NetworkSingleton<MoneyManager>.Instance.LaunderingNotificationIcon);
            else
            {
                localPlayer = Player.Local;
                float oldHp = localPlayer.Health.CurrentHealth;
                localPlayer.Health.RecoverHealth(150);
                Singleton<NotificationsManager>.Instance.SendNotification(
                                "Heal",
                                $"{oldHp} to {localPlayer.Health.CurrentHealth}",
                                NetworkSingleton<MoneyManager>.Instance.LaunderingNotificationIcon);
                healUsed = true;
            }

        }

        public static void GetCashDealer()
        {
            if(getCashUsed)
                Singleton<NotificationsManager>.Instance.SendNotification(
                                "Get Cash Dealer in Cooldown",
                                $"<color=#FF0000>Wait one day</color>",
                                NetworkSingleton<MoneyManager>.Instance.LaunderingNotificationIcon);
            else
            {
                dealerList = UnityEngine.Object.FindObjectsOfType<Dealer>();
                float totalCash = 0;

                foreach (Dealer dealer in dealerList)
                {
                    float cashDealer = dealer.Cash;
                    totalCash += cashDealer;
                    MoneyManager.Instance.ChangeCashBalance(cashDealer, true, true);

                    dealer.SetCash(0f);
                }
                Singleton<NotificationsManager>.Instance.SendNotification(
                                "Get Cash from Dealer",
                                $"<color=#16F01C>{MoneyManager.FormatAmount(totalCash)}</color> cash earned",
                                NetworkSingleton<MoneyManager>.Instance.LaunderingNotificationIcon);
                getCashUsed = true;
            }
        }


    }
}
