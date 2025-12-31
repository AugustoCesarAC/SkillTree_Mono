using HarmonyLib;
using MelonLoader;
using ScheduleOne.DevUtilities;
using ScheduleOne.Effects;
using ScheduleOne.Growing;
using ScheduleOne.Interaction;
using ScheduleOne.Levelling;
using ScheduleOne.Money;
using ScheduleOne.ObjectScripts;
using ScheduleOne.PlayerScripts;
using ScheduleOne.PlayerScripts.Health;
using ScheduleOne.Product;
using ScheduleOne.Property;
using ScheduleOne.Quests;
using ScheduleOne.Tools;
using ScheduleOne.UI;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static SkillTree.Core;

namespace SkillTree.SkillPatchStats
{
    // BASE VALUES
    public static class PlayerHealthConfig
    {
        public static float MaxHealth = 100f;
    }
    public static class PlayerXPConfig
    {
        public static float XpBase = 100f;
        public static float XpBase2 = 100f;
    }
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
    [HarmonyPatch(typeof(PlayerHealth))]
    public class PatchPlayerHealth
    {
        // Patch para SetHealth
        [HarmonyPatch("SetHealth")]
        [HarmonyPrefix]
        public static bool Prefix_SetHealth(PlayerHealth __instance, float health)
        {
            float clamped = Mathf.Clamp(health, 0f, PlayerHealthConfig.MaxHealth);
            SetInternalHealth(__instance, clamped);
            return false;
        }

        // Patch para RecoverHealth
        [HarmonyPatch("RecoverHealth")]
        [HarmonyPrefix]
        public static bool Prefix_RecoverHealth(PlayerHealth __instance, float recovery)
        {
            if (__instance.CurrentHealth <= 0f) return false;

            float novaVida = Mathf.Clamp(__instance.CurrentHealth + recovery, 0f, PlayerHealthConfig.MaxHealth);
            SetInternalHealth(__instance, novaVida);
            return false;
        }

        // Patch para o dano (importante para não clipar em 100)
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

        // Helper para injetar o valor na propriedade protected
        private static void SetInternalHealth(PlayerHealth instance, float value)
        {
            // Tenta o backing field da propriedade automática
            var field = AccessTools.Field(typeof(PlayerHealth), "<CurrentHealth>k__BackingField");
            field?.SetValue(instance, value);

            // Dispara o evento de UI
            instance.onHealthChanged?.Invoke(value);
        }
    }


    /// <summary>
    /// INCREASE XP GAIN
    /// </summary>
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
                xp = Mathf.RoundToInt(xp * multiplicador);
                MelonLogger.Msg($"[XP] Aplicado: {xpOriginal} -> {xp} (Base: {PlayerXPConfig.XpBase}%)");
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
            // 1. Calculamos o valor total recebido (Base + Bônus)
            float valorTotalDinheiro = __instance.Payment + bonusTotal;

            if (valorTotalDinheiro > 0)
            {
                // 2. Calculamos 5% desse valor para converter em XP
                // Ex: Ganhou $1000 -> Ganha 50 XP base
                int xpGanhaPeloDinheiro = Mathf.RoundToInt(valorTotalDinheiro * 0.05f);

                if (xpGanhaPeloDinheiro > 0)
                {
                    // 3. Tentamos encontrar o LevelManager para dar o XP
                    // Usando a lógica de instância que funciona no seu jogo
                    LevelManager levelManager = LevelManager.Instance;

                    if (levelManager != null)
                    {
                        MelonLogger.Msg($"[Contrato] Pagamento de ${valorTotalDinheiro} convertido em {xpGanhaPeloDinheiro} XP base.");

                        // Chamamos o método de XP do jogo
                        // Note que o seu patch anterior de bônus de XP vai agir aqui também!
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
    /// Sleep System
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
            if (next == 0) next = 2400; // Para conta de subtração

            // Cálculo de horas e minutos para exibição
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
            public static bool Prefix(out string noSleepReason, ref bool __result)
            {
                noSleepReason = null;

                if (!AllowSleepAthEne.Add)
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

                // Se estiver no horário da madrugada (0-700), mantém o texto padrão "Sleep"
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

                    int totalMinutesPassed = (int)(CalculateMinutesBetween(currentTime, (float)nextTarget))/3;

                    if (totalMinutesPassed > 0)
                    {
                        foreach (GrowContainer container in UnityEngine.Object.FindObjectsOfType<GrowContainer>())
                            AccessTools.Method(typeof(GrowContainer), "DrainMoisture")?.Invoke(container, new object[] { totalMinutesPassed*3 });
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

}
