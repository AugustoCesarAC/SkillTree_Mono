using MelonLoader;
using SkillTree.Json;
using SkillTree.SkillEffect;
using SkillTree.SkillsJson;
using System.Reflection;
using UnityEngine;

namespace SkillTree.UI
{
    public class SkillTreeUI
    {
        private Rect windowRect = new Rect(300, 150, 600, 450);

        private SkillTreeData originalData;
        private SkillTreeData editData;
        private SkillCategory? selectedCategory = null;

        private SkillConfig settings;
        public bool isRebinding = false;

        public bool Visible { get; set; }

        private List<SkillField> skillFields = new List<SkillField>();

        public GUISkin Skin { get; private set; }
        private bool skinReady;
        private bool guiInitialized = false;

        // =========================
        // Internal helper class
        // =========================
        private class SkillField
        {
            public string Id;
            public string Name;
            public string Description;
            public string Parent;
            public SkillCategory Category;
            public FieldInfo Field;
            public int MaxLevel;
        }

        // =========================
        // Constructor
        // =========================
        public SkillTreeUI(SkillTreeData data, SkillConfig sharedConfig)
        {
            originalData = data;
            editData = Clone(data);
            this.settings = sharedConfig;
            BuildSkillMap();
        }

        // =========================
        // Public draw
        // =========================
        public void Draw()
        {
            if (!Visible)
                return;

            if (!guiInitialized)
            {
                CenterWindow();
                guiInitialized = true;
            }

            windowRect = GUI.Window(
                9001,
                windowRect,
                DrawWindow,
                "Skill Tree"
            );
        }

        // =========================
        // Window content
        // =========================
        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("Skill Tree", GUILayout.Width(120));

            GUILayout.FlexibleSpace();

            GUILayout.Label($"Stats: {editData.StatsPoints}");
            GUILayout.Space(10);
            GUILayout.Label($"Operations: {editData.OperationsPoints}");
            GUILayout.Space(10);
            GUILayout.Label($"Social: {editData.SocialPoints}");
            GUILayout.Space(10);
            GUILayout.Label($"Special: {editData.SpecialPoints}");

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (selectedCategory == null)
            {
                DrawCategoryButtons();
            }
            else
            {
                DrawCategoryHeader();
                DrawTreeByCategory(selectedCategory.Value);
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical("box");
            string buttonLabel = isRebinding ? "<Color=yellow>Press any key... (ESC to cancel)</Color>" : $"Menu Hotkey: {settings.MenuHotkey}";

            if (GUILayout.Button(buttonLabel, new GUILayoutOption[] { GUILayout.Height(30) }))
            {
                isRebinding = true;
                MelonLogger.Msg("[SkillTree] Rebinding started. Waiting for input...");
            }

            GUILayout.EndVertical();

            if (isRebinding)
            {
                Event e = Event.current;

                if (e.isKey && e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.Escape)
                    {
                        isRebinding = false;
                        MelonLogger.Msg("[SkillTree] Rebinding cancelled by user.");
                    }
                    else if (e.keyCode != KeyCode.None)
                    {
                        settings.MenuHotkey = e.keyCode;

                        SkillTreeSaveManager.SaveConfig(settings);

                        isRebinding = false;
                        MelonLogger.Msg($"[SkillTree] Hotkey successfully changed to: {settings.MenuHotkey}");
                    }
                    e.Use();
                }
            }

            GUI.DragWindow();
        }

        private void DrawCategoryButtons()
        {
            GUILayout.Label("Choose a category");
            GUILayout.Space(10);

            if (GUILayout.Button("Stats"))
                selectedCategory = SkillCategory.Stats;

            if (GUILayout.Button("Operations"))
                selectedCategory = SkillCategory.Operations;

            if (GUILayout.Button("Social"))
                selectedCategory = SkillCategory.Social;

            if (GUILayout.Button("Special"))
                selectedCategory = SkillCategory.Special;
        }

        private void DrawCategoryHeader()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("< Back", GUILayout.Width(80)))
                selectedCategory = null;

            GUILayout.Label(selectedCategory.ToString());

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void DrawTreeByCategory(SkillCategory category)
        {
            foreach (var skill in skillFields)
            {
                if (skill.Parent == null && skill.Category == category)
                    DrawSkillNode(skill, 0);
            }
        }

        private int GetMaxLevelOfSkill(string skillId)
        {
            var parentSkill = skillFields.Find(s => s.Id == skillId);
            return parentSkill != null ? parentSkill.MaxLevel : 1;
        }

        private void DrawSkillNode(SkillField skill, int depth)
        {
            int value = (int)skill.Field.GetValue(editData); 
            int maxLevel = skill.MaxLevel;

            bool parentUnlocked = true;
            if (skill.Parent != null)
            {
                int parentCurrentValue = GetSkillValue(skill.Parent);
                int parentMaxLevel = GetMaxLevelOfSkill(skill.Parent);

                parentUnlocked = parentCurrentValue >= parentMaxLevel;
            }

            bool canBuy = parentUnlocked && value < maxLevel && GetPoints(skill.Category) > 0;

            GUILayout.BeginHorizontal();

            GUILayout.Space(depth * 20);

            GUILayout.Label($"{skill.Name} ({value}/{maxLevel})", GUILayout.Width(270));

            if (value < maxLevel)
            {
                GUI.enabled = canBuy;
                if (GUILayout.Button("+", GUILayout.Width(24), GUILayout.Height(20)))
                {
                    int newValue = value + 1;
                    skill.Field.SetValue(editData, newValue);

                    ConsumePoint(skill.Category);
                    SkillSystem.ApplySkill(skill.Id, editData);
                    SkillTreeSaveManager.Save(editData);
                }
                GUI.enabled = true;
            } else
                GUILayout.Label("✔", GUILayout.Width(24));


            GUILayout.EndHorizontal();

            Rect lineRect = GUILayoutUtility.GetLastRect();

            DrawTooltip(skill.Description, lineRect);

            if (value > 0)
            {
                foreach (var child in skillFields)
                {
                    if (child.Parent == skill.Id)
                        DrawSkillNode(child, depth + 1);
                }
            }
        }


        private void CenterWindow()
        {
            windowRect.x = (Screen.width - windowRect.width) / 2;
            windowRect.y = (Screen.height - windowRect.height) / 2;
        }


        private int GetPoints(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.Stats:
                    return editData.StatsPoints;
                case SkillCategory.Operations:
                    return editData.OperationsPoints;
                case SkillCategory.Social:
                    return editData.SocialPoints;
                case SkillCategory.Special:
                    return editData.SpecialPoints;
                default:
                    return 0;
            }
        }

        private void ConsumePoint(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.Stats:
                    editData.StatsPoints--;
                    break;
                case SkillCategory.Operations:
                    editData.OperationsPoints--;
                    break;
                case SkillCategory.Social:
                    editData.SocialPoints--;
                    break;
                case SkillCategory.Special:
                    editData.SpecialPoints--;
                    break;
            }

            editData.UsedSkillPoints++;
        }

        // =========================
        // Tree drawing
        // =========================
        private void DrawTree(string parent, int depth)
        {
            foreach (var skill in skillFields)
            {
                if (skill.Parent == parent)
                    DrawSkill(skill, depth);
            }
        }

        private void DrawSkill(SkillField skill, int depth)
        {
            if (skill.Parent != null && GetSkillValue(skill.Parent) == 0)
                return;

            int value = (int)skill.Field.GetValue(editData);

            GUILayout.BeginHorizontal();
            GUILayout.Space(depth * 30);

            GUILayout.Label($"{skill.Name}: {value}/1", GUILayout.Width(250));

            bool parentUnlocked =
                skill.Parent == null ||
                GetSkillValue(skill.Parent) == 1;

            bool canBuy =
                value == 0 &&
                GetPoints(skill.Category) > 0 &&
                parentUnlocked;

            GUI.enabled = canBuy;
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                skill.Field.SetValue(editData, 1);
                ConsumePoint(skill.Category);
                SkillTreeSaveManager.Save(editData);
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();

            Rect lineRect = GUILayoutUtility.GetLastRect();

            DrawTooltip(skill.Description, lineRect);

            DrawTree(skill.Id, depth);
        }

        // =========================
        // Tooltip
        // =========================
        private void DrawTooltip(string text, Rect anchorRect)
        {
            if (anchorRect.Contains(Event.current.mousePosition))
            {
                GUIContent content = new GUIContent(text);

                Vector2 size = GUI.skin.label.CalcSize(content);

                float padding = 10f;
                float tooltipWidth = size.x + padding;
                float tooltipHeight = size.y + padding;

                Rect tooltipRect = new Rect(
                    anchorRect.x + 20,
                    anchorRect.y - tooltipHeight - 5,
                    tooltipWidth,
                    tooltipHeight
                );

                GUI.Box(tooltipRect, text);
            }
        }

        // =========================
        // Reflection helpers
        // =========================
        private void BuildSkillMap()
        {
            skillFields.Clear();

            FieldInfo[] fields = typeof(SkillTreeData)
                .GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<SkillAttribute>();
                if (attr == null)
                    continue;

                skillFields.Add(new SkillField
                {
                    Id = field.Name,
                    Name = attr.Name,
                    Description = attr.Description,
                    Parent = attr.Parent,
                    Category = attr.Category,
                    Field = field,
                    MaxLevel = attr.MaxLevel
                });
            }
        }

        private int GetSkillValue(string fieldName)
        {
            FieldInfo field = typeof(SkillTreeData).GetField(fieldName);
            return (int)field.GetValue(editData);
        }

        private SkillTreeData Clone(SkillTreeData source)
        {
            string json = JsonUtility.ToJson(source);
            return JsonUtility.FromJson<SkillTreeData>(json);
        }

        private void Copy(SkillTreeData from, SkillTreeData to)
        {
            foreach (var field in typeof(SkillTreeData)
                     .GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                field.SetValue(to, field.GetValue(from));
            }
        }
        public void EnsureSkin()
        {
            if (skinReady)
                return;

            Skin = UnityEngine.Object.Instantiate(GUI.skin);

            Texture2D winBg = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.15f, 1f));
            Texture2D btnNormal = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.25f, 1f));
            Texture2D btnHover = MakeTex(2, 2, new Color(0.35f, 0.35f, 0.35f, 1f));
            Texture2D btnActive = MakeTex(2, 2, new Color(0.20f, 0.20f, 0.20f, 1f));
            Texture2D tipBg = MakeTex(2, 2, new Color(0.10f, 0.10f, 0.10f, 1f));

            // WINDOW
            GUIStyle win = new GUIStyle(Skin.window);
            Skin.window.normal.background = winBg;
            Skin.window.focused.background = winBg;
            win.normal.background = winBg;
            win.focused.background = winBg;
            win.active.background = winBg;
            win.hover.background = winBg;

            win.onNormal.background = winBg;
            win.onFocused.background = winBg;
            win.onActive.background = winBg;
            win.onHover.background = winBg;

            win.normal.textColor =
            win.focused.textColor =
            win.active.textColor =
            win.hover.textColor =
            win.onNormal.textColor =
            win.onFocused.textColor =
            win.onActive.textColor =
            win.onHover.textColor = Color.white;

            Skin.window = win;


            // LABEL
            Skin.label.normal.textColor = Color.white;
            Skin.label.focused.textColor = Color.white;

            // BUTTON
            GUIStyle btn = new GUIStyle(Skin.button);

            btn.normal.background = btnNormal;
            btn.hover.background = btnHover;
            btn.active.background = btnActive;
            btn.focused.background = btnNormal;

            btn.onNormal.background = btnNormal;
            btn.onHover.background = btnHover;
            btn.onActive.background = btnActive;
            btn.onFocused.background = btnNormal;

            btn.normal.textColor =
            btn.hover.textColor =
            btn.active.textColor =
            btn.focused.textColor =
            btn.onNormal.textColor =
            btn.onHover.textColor =
            btn.onActive.textColor =
            btn.onFocused.textColor = Color.white;

            Skin.button = btn;

            // TOOLTIP
            Skin.box.normal.background = tipBg;
            Skin.box.focused.background = tipBg;
            Skin.box.normal.textColor = Color.white;
            Skin.box.focused.textColor = Color.white;
            Skin.box.wordWrap = true;

            skinReady = true;
        }


        private Texture2D MakeTex(int w, int h, Color col)
        {
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = col;

            Texture2D tex = new Texture2D(w, h);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public void AddPoints(int stats, int ops, int social, int special)
        {
            this.editData.StatsPoints += stats;
            this.editData.OperationsPoints += ops;
            this.editData.SocialPoints += social;
            this.editData.SpecialPoints += special;

            this.originalData.StatsPoints += stats;
            this.originalData.OperationsPoints += ops;
            this.originalData.SocialPoints += social;
            this.originalData.SpecialPoints += special;

            SkillTreeSaveManager.Save(this.editData);

            //MelonLogger.Msg($"[SkillTreeUI] HUD Updated: +{stats}/+{ops}/+{social}");
        }

    }
}
