using System;
using System.Collections.Generic;
using System.Text;

namespace SkillTree.Json
{
    public enum SkillCategory
    {
        Stats,
        Operations,
        Social,
        Special
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SkillAttribute : Attribute
    {
        public string Name;
        public string Description;
        public string Parent; 
        public SkillCategory Category;
        public int MaxLevel;

        public SkillAttribute(
            string name,
            string description,
            SkillCategory category,
            string parent = null,
            int maxLevel = 2)
        {
            Name = name;
            Description = description;
            Category = category;
            Parent = parent;
            MaxLevel = maxLevel;
        }
    }

}
