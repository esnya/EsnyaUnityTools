using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public class DependencyTree : EditorWindow
    {

        [MenuItem("EsnyaTools/Dependency Tree")]
        private static void ShowWindow()
        {
            var window = GetWindow<DependencyTree>();
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Dependency Tree");
            Selection.selectionChanged += ScanSelected;
            ScanSelected();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= ScanSelected;
        }

        private void ScanSelected()
        {
            // foldoutValues.Clear();
            dictionary.Clear();

            rootAssets = Selection.objects.Select(o => AssetDatabase.GetAssetPath(o)).Where(path => path != null).ToList();
            foreach (var assetPath in rootAssets)
            {
                Scan(assetPath);
                foldoutValues[assetPath] = true;
            }

            Repaint();
        }

        private void Scan(string assetPath)
        {
            foreach (var dependencyPath in AssetDatabase.GetDependencies(assetPath, false))
            {
                dictionary[assetPath] = (dictionary.ContainsKey(assetPath) ? dictionary[assetPath] : Enumerable.Empty<string>()).Append(dependencyPath).Distinct().OrderBy(p => p).ToList();
            }
        }

        private void AssetField(string assetPath, string parentFieldPath = null)
        {
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        if (dictionary.ContainsKey(assetPath))
                        {
                            var fieldPath = parentFieldPath != null ? $"{parentFieldPath}:{assetPath}" : assetPath;
                            if (foldoutValues[fieldPath] = EditorGUILayout.Foldout(foldoutValues.ContainsKey(fieldPath) && foldoutValues[fieldPath], assetPath))
                            {
                                using (new EditorGUI.IndentLevelScope())
                                {
                                    foreach (var dependencyPath in dictionary[assetPath])
                                    {
                                        if (changeCheck.changed) Scan(dependencyPath);
                                        AssetField(dependencyPath, fieldPath);
                                    }
                                }
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField(assetPath);
                        }
                    }

                    if (GUILayout.Button("Ping Asset", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(assetPath));
                    }
                }
            }
        }

        [SerializeField] private List<string> rootAssets;
        [SerializeField] private readonly Dictionary<string, bool> foldoutValues = new Dictionary<string, bool>();
        [SerializeField] private readonly Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
        [SerializeField] private Vector2 scrollPosition;
        private void OnGUI()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollScope.scrollPosition;
                foreach (var assetPath in rootAssets) AssetField(assetPath);
            }
        }
    }
}
