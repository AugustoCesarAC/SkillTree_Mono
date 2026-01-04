using MelonLoader;
using S1API.Entities.Schedule;
using ScheduleOne.Delivery;
using ScheduleOne.Economy;
using ScheduleOne.GameTime;
using ScheduleOne.Levelling;
using ScheduleOne.Money;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Property;
using ScheduleOne.UI.ATM;
using ScheduleOne.UI.Phone;
using ScheduleOne.UI.Phone.Delivery;
using SkillTree.Json;
using SkillTree.SkillEffect;
using SkillTree.UI;
using UnityEngine;

[assembly: MelonInfo(typeof(SkillTree.Core), "SkillTree", "1.0.0", "CrazyReizor", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace SkillTree
{
    public class Core : MelonMod
    {

        public static Core Instance;

        private TimeManager timeManager;
        private LevelManager levelManager;
        private PlayerMovement playerMovement;
        private Player localPlayer;
        private PlayerCamera playerCamera;
        private PlayerInventory playerInventory;
        private Customer[] customerList;

        private float timer = 2f;

        private SkillTreeData skillData;
        private SkillTreeUI skillTreeUI;
        private int skillPointValid = 0;

        private bool waiting = false;
        private bool firstTime = false;

        private int lastProcessedTier = -1;
        private ERank lastProcessedRank = (ERank)(-1);

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("SkillTree Initialized.");
            Instance = this;

            var harmony = new HarmonyLib.Harmony("com.reizor.skilltree");

            harmony.PatchAll();

            LoggerInstance.Msg("Harmony patches applied.");
        }

        public void Init()
        {
            LoggerInstance.Msg("Init()");
            if (timeManager == null)
                timeManager = TimeManager.Instance;

            if (levelManager == null)
                levelManager = LevelManager.Instance;

            if (playerMovement == null)
                playerMovement = PlayerMovement.Instance;

            if (playerCamera == null)
                playerCamera = PlayerCamera.Instance;

            if (playerInventory == null)
                playerInventory = PlayerInventory.Instance;

            if (localPlayer == null)
                localPlayer = Player.Local;

            if (customerList == null)
                customerList = UnityEngine.Object.FindObjectsOfType<Customer>();
        }

        public override void OnUpdate()
        {
            if (TimeManager.Instance == null || PlayerMovement.Instance == null || PlayerCamera.Instance == null)
                return;

            if (!waiting)
                if (!WaitTime())
                    return;

            bool treeUiChange = false;

            if (Input.GetKeyDown(KeyCode.C) && waiting)
            {
                skillTreeUI.Visible = !skillTreeUI.Visible;
                treeUiChange = true;
            }

            if (skillTreeUI.Visible)
                playerCamera.SetDoFActive(true, 0.06f);

            if (!skillTreeUI.Visible)
                playerCamera.SetDoFActive(false, 0f);

            if (skillTreeUI.Visible && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) || Input.GetMouseButtonDown(1)))
            {
                skillTreeUI.Visible = !skillTreeUI.Visible;
                treeUiChange = true;
            }

            if (treeUiChange)
            {
                treeUiChange = false;
                Cursor.lockState = skillTreeUI.Visible ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = skillTreeUI.Visible ? true : false;
                PlayerMovement.Instance.CanMove = !skillTreeUI.Visible;
                PlayerCamera.Instance.enabled = !skillTreeUI.Visible;
                PlayerManager.Instance.enabled = !skillTreeUI.Visible;
                playerInventory.SetInventoryEnabled(!skillTreeUI.Visible);
            }

        }

        private bool WaitTime()
        {   
            if (!firstTime)
            {
                if (timeManager == null || levelManager == null || playerMovement == null)
                    Init();
                skillData = SkillTreeSaveManager.LoadOrCreate();
                skillTreeUI = new SkillTreeUI(skillData);
                SkillSystem.ApplyAll(skillData);
            }

            firstTime = true;
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                ItemUnlocker.UnlockSpecificItems();
                AttPoints();
                waiting = true;
                return true;
            }
            return false;
        }

        public void AttPoints(bool levelUp = false)
        {
            if (timeManager == null || levelManager == null || playerMovement == null)
                Init();

            int currentRank = (int)levelManager.Rank;
            int currentTier = levelManager.Tier - 1;

            int maxPointsPossible = (currentRank * 6) + currentTier;
            //MelonLogger.Msg("maxPointsPossible " + maxPointsPossible);
            int maxPointsJson = skillData.StatsPoints + skillData.OperationsPoints + skillData.SocialPoints + skillData.UsedSkillPoints;
            //MelonLogger.Msg("maxPointsJson " + maxPointsJson);

            if (maxPointsPossible != maxPointsJson && !levelUp)
            {
                MelonLogger.Msg("Desync detected! Synchronizing points with saved XP in the game...");
                string path = SkillTreeSaveManager.GetDynamicPath();
                if (File.Exists(path))
                    File.Delete(path);
                skillData = SkillTreeSaveManager.LoadOrCreate();
                skillTreeUI = new SkillTreeUI(skillData);
                SkillSystem.ApplyAll(skillData);
                skillPointValid = maxPointsPossible;
            }

            if (currentRank == 0 && currentTier == 0)
                return;

            if (levelUp && currentTier == (lastProcessedTier - 1) && (int)levelManager.Rank == (int)lastProcessedRank)
                return;

            else if (levelUp)
                MelonLogger.Msg("Level Up Detected! Skill points updated.");

            if (levelUp)
            {
                skillPointValid = 1;
                if (lastProcessedTier == 5)
                    skillPointValid = 2;
            }

            lastProcessedTier = levelManager.Tier;
            lastProcessedRank = levelManager.Rank;

            //MelonLogger.Msg("skillPointValid " + skillPointValid);

            int totalSkillPoint = skillData.StatsPoints + skillData.OperationsPoints + skillData.SocialPoints + skillData.UsedSkillPoints;
            //MelonLogger.Msg("totalSkillPoint " + totalSkillPoint);

            if (skillPointValid > 0)
            {
                int statsGained = 0;
                int opsGained = 0;
                int socialGained = 0;

                for (int i = 0; i < skillPointValid; i++)
                {
                    int mod = (totalSkillPoint + i) % 3;
                    switch (mod)
                    {
                        case 0:
                            statsGained++;
                            break;
                        case 1:
                            opsGained++;
                            break;
                        case 2:
                            socialGained++;
                            break;
                    }
                }

                if (skillTreeUI == null)
                    skillTreeUI = new SkillTreeUI(skillData);

                if (skillTreeUI != null)
                    skillTreeUI.AddPoints(statsGained, opsGained, socialGained);

                MelonLogger.Msg($"[SkillTree] Processed: Rank {levelManager.Rank} Tier {levelManager.Tier}. Gains: Stats+{statsGained} Operations+{opsGained} Social+{socialGained}");
            }
        }

        public override void OnGUI()
        {
            if (skillTreeUI == null || !skillTreeUI.Visible)
                return;

            skillTreeUI.EnsureSkin();

            GUI.skin = skillTreeUI.Skin;

            if (Event.current.type == EventType.MouseDown)
                GUI.FocusControl(null);

            skillTreeUI.Draw();
        }
    }
}