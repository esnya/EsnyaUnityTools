namespace EsnyaFactory
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    public class AssetRenamer : EditorWindow
    {
        [MenuItem("EsnyaTools/Asset Renamer")]
        [MenuItem("Assets/EsnyaTools/Asset Renamer")]
        public static void Show(MenuCommand menuCommand)
        {
            var w = GetWindow<AssetRenamer>();
            w.Show();
        }

        private DefaultAsset directory;
        private Dictionary<string, bool> assets = new Dictionary<string, bool>();
        private string pattern = "";
        private bool useRegex = false;
        private string replacement = "";

        public void OnEnable()
        {
            titleContent = new GUIContent("Asset Renamer");
        }

        void OnGUI()
        {
            directory = EEU.AssetDirectoryField("Directory", directory);

            if (directory != null) {
                var dirPath = AssetDatabase.GetAssetPath(directory);

                EEU.Horizontal(() => {
                    EEU.Button("Select All", () => {
                        assets.Keys.ToList().ForEach(key => {
                            assets[key] = true;
                        });
                    });
                    EEU.Button("Deselect All", () => {
                        assets.Keys.ToList().ForEach(key => {
                            assets[key] = false;
                        });
                    });
                });

                assets = AssetDatabase
                    .FindAssets("", new string[] { dirPath })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !AssetDatabase.IsValidFolder(path) && !path.Substring(dirPath.Length + 1).Contains('/'))
                    .Select((path, i) => {
                        var localPath = path.Substring(dirPath.Length + 1);
                        bool value = EditorGUILayout.ToggleLeft(localPath, assets.FirstOrDefault(p => p.Key == path).Value);
                        if (value) {
                            EditorGUILayout.LabelField("\t", DoReplace(localPath));
                        }
                        return new {
                            path,
                            value,
                        };
                    })
                    .ToDictionary(a => a.path, a => a.value);

                EditorGUILayout.Space();

                pattern = EditorGUILayout.TextField("Search", pattern);
                replacement = EditorGUILayout.TextField("Replace", replacement);
                useRegex = EditorGUILayout.ToggleLeft("Use Regex", useRegex);

                EEU.Button("Replace", () => {
                    assets
                        .Where(p => p.Value)
                        .Select(p => p.Key)
                        .ToList()
                        .ForEach(path => {
                            AssetDatabase.MoveAsset(path, DoReplace(path));
                        });
                });
            }
        }

        private string DoReplace(string input) {
            try {
                if (useRegex) {
                    var r = new Regex(pattern);
                    return r.Replace(input, replacement);
                }
                return input.Replace(pattern, replacement);
            } catch {
                return input;
            }
        }
    }
}
