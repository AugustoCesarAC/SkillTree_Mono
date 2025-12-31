using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Rendering;

namespace SkillTree.Json
{
    [System.Serializable]
    public class SkillTreeData
    {
        public int StatsPoints = 0;
        public int OperationsPoints = 0;
        public int SocialPoints = 0;
        public int UsedSkillPoints = 0;

        /* STATUS START HERE */

        // Stats 
        [Skill("More Health", "Increase Max Health +20", SkillCategory.Stats, null, 1)]
        public int Stats = 0;

        // Stats subs
        [Skill("More Movespeed", "Increase 10% Movespeed", SkillCategory.Stats, "Stats", 3)]
        public int MoreMovespeed = 0;

        // Stats subs
        [Skill("More XP", "Increase 15% XP", SkillCategory.Stats, "Stats")]
        public int MoreXP = 0;

        // Stats subs
        [Skill("More Item Stack (x2)", "Increase Item Stack (x2)", SkillCategory.Stats, "MoreXP", 1)]
        public int MoreStackItem = 0;

        // Stats subs
        [Skill("Allow Sleep with Athletic or Energizing", "Allow Sleep even when the effects Athletic or Energizing is active", SkillCategory.Stats, "Stats", 1)]
        public int AllowSleepAthEne = 0;

        // AllowSleepAthEne subs
        [Skill("Allow Use Bed to Skip the Current Schedule", "Skip Schedule Only Affect Plants (Plants Grow 1/3 of the time)", SkillCategory.Stats, "AllowSleepAthEne", 1)]
        public int SkipSchedule = 0;

        // MoreXP subs
        [Skill("More XP Per Sell When Earn Money", "Earn 5% XP base on value itens when you sell drugs", SkillCategory.Stats, "MoreXP", 1)]
        public int MoreXPWhenEarnMoney = 0;

        // Stats subs
        [Skill("More XP 2", "Increase 15% XP", SkillCategory.Stats, "MoreXP", 5)]
        public int MoreXP2 = 0;

        /* STATUS END HERE */

        /* OPERATIONS START HERE */

        [Skill("Better Grow Tent Quality", "Increase Quality of the Grow Tent (Trash -> Low)", SkillCategory.Operations, null, 1)]
        public int Operations = 0;

        [Skill("Increase Growth Speed", "Increase 7.5% Growth Speed", SkillCategory.Operations, "Operations")]
        public int GrowthSpeed = 0;

        // Operations subs
        [Skill("More Yield", "Increase Yield (+1) (Not Mushroom/Grow Tent)", SkillCategory.Operations, "GrowthSpeed", 1)]
        public int MoreYield = 0;

        // Operations subs
        [Skill("More Quality", "Increase Quality in 10% (At Max Advance Tier)", SkillCategory.Operations, "MoreYield", 3)]
        public int MoreQuality = 0;

        // Operations subs
        [Skill("More Quality Mushroom", "Advance Quality Tier of Mushroom (At Max)", SkillCategory.Operations, "MoreQuality", 2)]
        public int MoreQualityMushroom = 0;

        // Operations subs
        [Skill("Increase Growth Speed 2°", "Increase 7.5% Growth Speed", SkillCategory.Operations, "MoreQuality", 2)]
        public int GrowthSpeed2 = 0;

        // Operations subs
        [Skill("AbsorbentSoil", "Preserves Additives in Soil At Max Level", SkillCategory.Operations, "MoreQuality", 3)]
        public int AbsorbentSoil = 0;

        // Operations subs
        [Skill("More Mix Output", "Increase Mix Output (x2)", SkillCategory.Operations, "MoreYield", 1)]
        public int MoreMixOutput = 0;

        // MoreMixOutput subs
        [Skill("Chemist Station Quick", "Increase speed of ALL Chemist Station (x2 or a little more)", SkillCategory.Operations, "MoreMixOutput", 1)]
        public int ChemistStationQuick = 0;

        // MoreMixOutput subs
        [Skill("More Cauldron Output", "Increase Cauldron 25% Output", SkillCategory.Operations, "MoreMixOutput", 4)]
        public int MoreCauldronOutput = 0;

        /* OPERATIONS END HERE */

        /* SOCIAL START HERE */

        [Skill("More sample chance", "Increase sample chance in 5%", SkillCategory.Social)]
        public int Social = 0;

        // Social subs
        [Skill("Civil More Money per week", "Increase civil weekly money in 10%", SkillCategory.Social, "Social", 4)]
        public int CityEvolving = 0;

        // Social subs
        [Skill("More ATM Limit", "Increase ATM Limit +1000", SkillCategory.Social, "Social", 3)]
        public int MoreATMLimit = 0;

        // MoreATMLimit subs
        [Skill("Better Business", "Increase 10% Max Laundering Capacity", SkillCategory.Social, "MoreATMLimit", 3)]
        public int BusinessEvolving = 0;

        // Social subs
        [Skill("Dealer More Customer", "Increase Customer of Dealer (+2)", SkillCategory.Social, "Social", 1)]
        public int DealerMoreCustomer = 0;

        // Social subs
        [Skill("Less Dealer Cut", "Decrease Dealer Cut 5%", SkillCategory.Social, "DealerMoreCustomer", 2)]
        public int DealerCutLess = 0;

        // Social subs
        [Skill("Better Supplier", "Increase Debt and Items Limit (2x)", SkillCategory.Social, "Social", 1)]
        public int BetterSupplier = 0;

        // DealerCutLess subs
        [Skill("Dealer Speed Up", "Increase Speed of Dealer (2x)", SkillCategory.Social, "DealerMoreCustomer", 1)]
        public int DealerSpeedUp = 0;

        /* SOCIAL ENDS HERE */

    }
}
