using MelonLoader;
using MelonLoader.Utils;
using ScheduleOne.DevUtilities;
using ScheduleOne.Persistence;
using SkillTree.SkillPatchSocial;
using UnityEngine;

namespace SkillTree.Json
{
    public static class SkillTreeSaveManager
    {
        public static string GetDynamicPath()
        {
            string saveID = GetCurrentSaveID();
            return Path.Combine(MelonEnvironment.UserDataDirectory, $"SkillTree_{saveID}.json");
        }

        public static SkillTreeData LoadOrCreate()
        {
            string path = GetDynamicPath();

            if (!File.Exists(path))
            {
                MelonLogger.Msg($"[SkillTree] Novo save detectado ou arquivo ausente: {path}");

                // Reset do cache de clientes para garantir que o novo save leia valores limpos
                CustomerCache.IsLoaded = false;
                CustomerCache.OriginalMinSpend.Clear();
                CustomerCache.OriginalMaxSpend.Clear();

                var data = CreateDefault();
                Save(data); // Salva o novo arquivo dinâmico
                return data;
            }

            // Lê o arquivo dinâmico correto
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SkillTreeData>(json);
        }

        public static void Save(SkillTreeData data)
        {
            string path = GetDynamicPath(); // Salva no arquivo do save atual
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }

        private static SkillTreeData CreateDefault()
        {
            return new SkillTreeData();
        }

        public static string GetCurrentSaveID()
        {
            string fullPath = Singleton<LoadManager>.Instance.LoadedGameFolderPath;

            if (string.IsNullOrEmpty(fullPath))
                return "DefaultPlayer";

            // Retorna apenas o nome da pasta (ex: "MeuSave01") para não sujar o nome do arquivo
            return Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
    }
}
