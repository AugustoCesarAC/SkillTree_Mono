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
        [Skill("More Health", "Increase Max Health by +20", SkillCategory.Stats, null, 1)]
        public int Stats = 0;

        // Stats subs
        [Skill("More Movespeed", "Increase 10% by Movespeed", SkillCategory.Stats, "Stats", 3)]
        public int MoreMovespeed = 0;

        // Stats subs
        [Skill("More XP", "Increase XP Gain by 5%", SkillCategory.Stats, "Stats")]
        public int MoreXP = 0;

        // MoreXP subs
        [Skill("More Item Stack (x2)", "Increase Item Stack (x2)", SkillCategory.Stats, "MoreXP", 1)]
        public int MoreStackItem = 0;

        // MoreXP subs
        [Skill("More XP Per Sell When Earn Money", "Earn 5% bonus XP based on item value when selling drugs", SkillCategory.Stats, "MoreXP", 1)]
        public int MoreXPWhenEarnMoney = 0;

        // Stats subs
        [Skill("Better Delivery", "Make Deliveries More Fast (6H -> 2H)", SkillCategory.Stats, "Stats", 1)]
        public int BetterDelivery = 0;

        // Stats subs
        [Skill("Allow Sleep with Athletic or Energizing", "Allow sleeping while Athletic or Energizing effects are active", SkillCategory.Stats, "Stats", 1)]
        public int AllowSleepAthEne = 0;

        // Stats subs
        [Skill("Counteroffer Perception", "Allows you to see the chance of a customer making a counteroffer", SkillCategory.Stats, "Stats", 1)]
        public int AllowSeeCounteroffChance = 0;

        // AllowSleepAthEne subs
        [Skill("Allow Use Bed to Skip the Current Schedule", "Skip Schedule Only Affect Plants (Plants Grow 33% of the time)", SkillCategory.Stats, "AllowSleepAthEne", 1)]
        public int SkipSchedule = 0;

        // Stats subs
        [Skill("More XP 2", "Increase XP Gain by an additional 5%", SkillCategory.Stats, "MoreXP", 4)]
        public int MoreXP2 = 0;

        /* STATUS END HERE */

        /* OPERATIONS START HERE */

        [Skill("Better Grow Tent Quality", "Improve Grow Tent Quality (Trash -> Low)", SkillCategory.Operations, null, 1)]
        public int Operations = 0;

        [Skill("Increase Growth Speed", "Increase Growth Speed by 2.5%", SkillCategory.Operations, "Operations")]
        public int GrowthSpeed = 0;

        // Operations subs
        [Skill("More Yield", "Increase Yield by 1 (Excludes Mushrooms/Grow Tents)", SkillCategory.Operations, "GrowthSpeed", 1)]
        public int MoreYield = 0;

        // Operations subs
        [Skill("Advanced Pot Techniques", "+15% Quality. [Lvl 1] Moisture Pots gain +1 Tier. [Max Lvl] All Pots gain +1 Tier.", SkillCategory.Operations, "MoreYield", 2)]
        public int MoreQuality = 0;

        // Operations subs
        [Skill("More Quality Mushroom", "Upgrade Mushroom Quality Tier", SkillCategory.Operations, "MoreQuality", 1)]
        public int MoreQualityMushroom = 0;

        // Operations subs
        [Skill("More Quality Meth", "Upgrade Meth Quality Tier", SkillCategory.Operations, "MoreYield", 1)]
        public int MoreQualityMeth = 0;

        // Operations subs
        [Skill("Increase Growth Speed 2°", "Increase Growth Speed by 2.5%", SkillCategory.Operations, "MoreQuality", 2)]
        public int GrowthSpeed2 = 0;

        // MoreMixOutput subs
        [Skill("Chemist Station Quick", "Increase the speed of ALL Chemistry Station (x2 or a little more)", SkillCategory.Operations, "MoreYield", 1)]
        public int ChemistStationQuick = 0;

        // Operations subs
        [Skill("More Mix and Drying Rack Output", "Double Mix and Drying Rack Output", SkillCategory.Operations, "ChemistStationQuick", 1)]
        public int MoreMixAndDryingRackOutput = 0;

        // Operations subs
        [Skill("AbsorbentSoil", "Preserve soil additives", SkillCategory.Operations, "ChemistStationQuick", 1)]
        public int AbsorbentSoil = 0;

        // MoreMixOutput subs
        [Skill("More Cauldron Output", "Double Cauldron Output", SkillCategory.Operations, "MoreMixAndDryingRackOutput", 1)]
        public int MoreCauldronOutput = 0;

        /* OPERATIONS END HERE */

        /* SOCIAL START HERE */

        [Skill("More sample chance", "Increase Sample Chance by 5%", SkillCategory.Social)]
        public int Social = 0;

        // Social subs
        [Skill("Civil More Money per week", "Increase Citizens' weekly money by 10%", SkillCategory.Social, "Social", 2)]
        public int CityEvolving = 0;

        // Social subs
        [Skill("More ATM Limit", "Increase ATM Deposit Limit by +1500", SkillCategory.Social, "Social", 4)]
        public int MoreATMLimit = 0;

        // MoreATMLimit subs
        [Skill("Better Business", "Increase Max Money Laundering Capacity by 20%", SkillCategory.Social, "Social", 3)]
        public int BusinessEvolving = 0;

        // Social subs
        [Skill("Dealer More Customer", "Increase Dealer Customers (+2)", SkillCategory.Social, "Social", 1)]
        public int DealerMoreCustomer = 0;

        // Social subs
        [Skill("Less Dealer Cut", "Decrease Dealer's Cut by 5%", SkillCategory.Social, "DealerMoreCustomer", 2)]
        public int DealerCutLess = 0;

        // Social subs
        [Skill("Better Supplier", "Increase Debt and Item Limits by 50%", SkillCategory.Social, "Social", 2)]
        public int BetterSupplier = 0;

        // DealerCutLess subs
        [Skill("Dealer Speed Up", "Increase Movespeed Speed of Dealers (2x)", SkillCategory.Social, "DealerMoreCustomer", 1)]
        public int DealerSpeedUp = 0;

        /* SOCIAL ENDS HERE */

    }
}
