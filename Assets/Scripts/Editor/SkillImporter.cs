using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectChicken.Systems.SkillTree;

namespace ProjectChicken.Editor
{
    /// <summary>
    /// Imports Skill Tree data from CSV into SkillNodeData ScriptableObjects.
    /// Syncs Unity with external spreadsheet (e.g. Google Sheets export).
    /// </summary>
    public static class SkillImporter
    {
        private const string CsvPath = "Assets/Data/Skills.csv";
        private const string TargetFolder = "Assets/Data/Resources/Skills";
        private const string IconFolder = "Assets/Art/Icons";

        private static readonly string[] CsvColumns = new[]
        {
            "ID", "DisplayName", "Description", "MaxLevel", "BaseCost", "CostMultiplier",
            "ParentIDs", "IconName"
        };

        [MenuItem("Tools/Import Skills from CSV")]
        public static void ImportFromCsv()
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError($"[SkillImporter] CSV not found: {CsvPath}");
                return;
            }

            string[] lines = File.ReadAllLines(CsvPath);
            if (lines.Length < 2)
            {
                Debug.LogWarning("[SkillImporter] CSV has no data rows.");
                return;
            }

            string[] header = ParseCsvLine(lines[0]);
            int[] columnIndices = GetColumnIndices(header);
            if (columnIndices == null)
            {
                Debug.LogError("[SkillImporter] CSV missing required columns.");
                return;
            }

            EnsureTargetFolderExists();

            // Pass 1: Create or update assets
            var idToAssetAndParentIds = new Dictionary<string, (SkillNodeData asset, string parentIds)>();

            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCsvLine(lines[i]);
                if (values.Length <= columnIndices[0])
                    continue;

                string id = GetValue(values, columnIndices, 0);
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                string displayName = GetValue(values, columnIndices, 1);
                string description = GetValue(values, columnIndices, 2);
                int maxLevel = ParseInt(GetValue(values, columnIndices, 3), 1);
                int baseCost = ParseInt(GetValue(values, columnIndices, 4), 10);
                float costMultiplier = ParseFloat(GetValue(values, columnIndices, 5), 1f);
                string parentIds = GetValue(values, columnIndices, 6);
                string iconName = GetValue(values, columnIndices, 7);

                SkillNodeData asset = FindOrCreateSkillAsset(id);
                ApplySkillData(asset, id, displayName, description, maxLevel, baseCost, costMultiplier);
                idToAssetAndParentIds[id] = (asset, parentIds ?? "");
            }

            // Pass 2: Link prerequisites
            foreach (var kvp in idToAssetAndParentIds)
            {
                string parentIdsStr = kvp.Value.parentIds;
                if (string.IsNullOrWhiteSpace(parentIdsStr))
                    continue;

                string[] parentIds = parentIdsStr.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (parentIds.Length == 0)
                    continue;

                var prereqList = new List<SkillNodeData>();
                foreach (string pid in parentIds)
                {
                    if (idToAssetAndParentIds.TryGetValue(pid, out var parentEntry))
                        prereqList.Add(parentEntry.asset);
                }

                SetPrerequisites(kvp.Value.asset, prereqList);
            }

            // Pass 3: Icons
            foreach (var kvp in idToAssetAndParentIds)
            {
                string iconName = GetIconNameForRow(lines, kvp.Key, header, columnIndices, lines.Length);
                if (string.IsNullOrWhiteSpace(iconName))
                    continue;

                Sprite sprite = LoadSprite(iconName);
                if (sprite != null)
                    SetIcon(kvp.Value.asset, sprite);
            }

            foreach (var kvp in idToAssetAndParentIds)
            {
                EditorUtility.SetDirty(kvp.Value.asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SkillImporter] Imported {idToAssetAndParentIds.Count} skills from {CsvPath}");
        }

        private static void EnsureTargetFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Resources"))
                AssetDatabase.CreateFolder("Assets/Data", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Resources/Skills"))
                AssetDatabase.CreateFolder("Assets/Data/Resources", "Skills");
        }

        private static int[] GetColumnIndices(string[] header)
        {
            var indices = new int[CsvColumns.Length];
            for (int i = 0; i < CsvColumns.Length; i++)
            {
                int idx = System.Array.FindIndex(header, c => string.Equals(c?.Trim(), CsvColumns[i], System.StringComparison.OrdinalIgnoreCase));
                if (idx < 0 && i < 2)
                    return null;
                indices[i] = idx >= 0 ? idx : -1;
            }
            return indices;
        }

        private static string GetIconNameForRow(string[] lines, string rowId, string[] header, int[] columnIndices, int lineCount)
        {
            int iconCol = columnIndices[7];
            if (iconCol < 0) return null;
            for (int i = 1; i < lines.Length && i < lineCount; i++)
            {
                string[] values = ParseCsvLine(lines[i]);
                if (GetValue(values, columnIndices, 0) == rowId)
                    return GetValue(values, columnIndices, 7);
            }
            return null;
        }

        private static string GetValue(string[] values, int[] columnIndices, int columnIndex)
        {
            int idx = columnIndices[columnIndex];
            if (idx < 0 || idx >= values.Length) return null;
            string s = values[idx]?.Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static SkillNodeData FindOrCreateSkillAsset(string id)
        {
            string folder = TargetFolder + "/";
            string targetPath = folder + id + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<SkillNodeData>(targetPath);
            if (existing != null)
                return existing;

            var asset = ScriptableObject.CreateInstance<SkillNodeData>();
            AssetDatabase.CreateAsset(asset, targetPath);
            return asset;
        }

        private static void ApplySkillData(SkillNodeData asset, string id, string displayName, string description,
            int maxLevel, int baseCost, float costMultiplier)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("id").stringValue = id ?? "";
            so.FindProperty("displayName").stringValue = displayName ?? "";
            so.FindProperty("description").stringValue = description ?? "";

            var levelsProp = so.FindProperty("levels");
            levelsProp.arraySize = Mathf.Max(1, maxLevel);
            for (int i = 0; i < levelsProp.arraySize; i++)
            {
                var elem = levelsProp.GetArrayElementAtIndex(i);
                int cost = Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, i));
                elem.FindPropertyRelative("cost").intValue = Mathf.Max(0, cost);
                elem.FindPropertyRelative("effectValue").floatValue = 1f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPrerequisites(SkillNodeData asset, List<SkillNodeData> prerequisites)
        {
            var so = new SerializedObject(asset);
            var listProp = so.FindProperty("prerequisites");
            listProp.arraySize = prerequisites.Count;
            for (int i = 0; i < prerequisites.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = prerequisites[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSprite(string iconName)
        {
            if (string.IsNullOrWhiteSpace(iconName)) return null;
            string name = iconName.Trim();
            string path = IconFolder + "/" + name;
            if (!path.EndsWith(".png") && !path.EndsWith(".asset"))
                path += ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return null;
        }

        private static void SetIcon(SkillNodeData asset, Sprite sprite)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("icon").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string[] ParseCsvLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return new string[0];
            var list = new List<string>();
            int i = 0;
            while (i < line.Length)
            {
                if (line[i] == '"')
                {
                    i++;
                    int start = i;
                    while (i < line.Length && line[i] != '"') i++;
                    list.Add(line.Substring(start, i - start));
                    if (i < line.Length) i++;
                }
                else
                {
                    int start = i;
                    while (i < line.Length && line[i] != ',') i++;
                    list.Add(line.Substring(start, i - start).Trim().Trim('"'));
                    if (i < line.Length) i++;
                }
            }
            return list.ToArray();
        }

        private static int ParseInt(string s, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            return int.TryParse(s.Trim(), out int v) ? v : defaultValue;
        }

        private static float ParseFloat(string s, float defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            return float.TryParse(s.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : defaultValue;
        }
    }
}
