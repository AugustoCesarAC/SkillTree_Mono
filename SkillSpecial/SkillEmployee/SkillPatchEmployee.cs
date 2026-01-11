using HarmonyLib;
using MelonLoader;
using ScheduleOne.Employees;
using ScheduleOne.GameTime;
using ScheduleOne.Management;
using ScheduleOne.Trash;
using ScheduleOne.UI.Management;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MelonLoader.MelonLogger;

namespace SkillTree.SkillSpecial.SkillEmployee
{
    public static class CanWork
    {
        public static bool Add = false;
    }

    [HarmonyPatch(typeof(Employee), "CanWork")]
    public class Patch_Employee_CanWork
    {
        static void Postfix(ref bool __result)
        {
            if (CanWork.Add)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(ClipboardScreen), "Start")]
    public class Patch_ClipboardScreen_Fix
    {
        static void Postfix(ClipboardScreen __instance)
        {
            if (__instance.Container == null) return;

            __instance.Container.localScale = new Vector3(0.9f, 0.9f, 1f);
        }
    }

    public static class EmployeeMovespeed
    {
        public static bool Add = false;
    }

    public static class EmployeeMoreStation
    {
        public static int Add = 0;
    }

    [HarmonyPatch(typeof(Employee), "SetIsPaid")]
    public class Patch_Employee_ActiveNew()
    {
        private static Packager[] packagerList;
        private static Chemist[] chemistList;
        private static Botanist[] botanistList;
        private static Cleaner[] cleanerList;

        static void Postfix()
        {
            if(!BetterBotanist.Add) return;

            packagerList = UnityEngine.Object.FindObjectsOfType<Packager>();
            chemistList = UnityEngine.Object.FindObjectsOfType<Chemist>();
            botanistList = UnityEngine.Object.FindObjectsOfType<Botanist>();
            cleanerList = UnityEngine.Object.FindObjectsOfType<Cleaner>();


            if (EmployeeMoreStation.Add == 0) return;

            foreach (Packager packager in packagerList)
            {
                if (EmployeeMovespeed.Add)
                    packager.Movement.MovementSpeedScale = 0.33f;
            }

            foreach (Chemist chemist in chemistList)
            {
                if (EmployeeMovespeed.Add)
                    chemist.Movement.MovementSpeedScale = 0.33f;

                if (EmployeeMoreStation.Add > 0)
                {
                    var config = chemist.Configuration as ChemistConfiguration;
                    config.Stations.MaxItems = 4 + EmployeeMoreStation.Add;
                }
            }

            foreach (Botanist botanist in botanistList)
            {
                if (EmployeeMovespeed.Add)
                    botanist.Movement.MovementSpeedScale = 0.33f;

                if (EmployeeMoreStation.Add > 0)
                {
                    var config = botanist.Configuration as BotanistConfiguration;
                    config.Assigns.MaxItems = 8 + (EmployeeMoreStation.Add * 2);
                }
            }
            foreach (Cleaner cleaner in cleanerList)
            {
                if (!EmployeeMovespeed.Add) continue;

                cleaner.Movement.MovementSpeedScale = 0.33f;
            }
        }
    }
}
