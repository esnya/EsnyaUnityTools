using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EsnyaFactory
{
    public class RenameTool : EditorWindow
    {

        [MenuItem("EsnyaTools/Rename Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<RenameTool>();
            window.titleContent = new GUIContent("Rename Tool");
            window.Show();

        }

        private SerializedObject serializedObject;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Unique Name For Sibling"))
            {
                UniqueNameForSiblings(Selection.gameObjects);
            }

            if (GUILayout.Button("Scan Name Conflictions"))
            {
                DupScan();
            }

            if (dupes != null)
            {
                if (dupes.Length == 0)
                {
                    EditorGUILayout.HelpBox("No conflicts", MessageType.Info);
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.HelpBox($"{dupes.Length} GameObject(s) has conflicted name", MessageType.Info);
                        if (GUILayout.Button("Fix"))
                        {
                            UniqueNameForSiblings(dupes);
                            dupes = null;
                        }
                    }
                }
            }
        }

        private void UniqueNameForSiblings(GameObject[] targets)
        {
            Undo.RecordObjects(targets, "Rename Tool");
            foreach (var (gameObject, index) in targets.OrderBy(o => o.transform.GetSiblingIndex()).Select((gameObject, index) => (gameObject, index)))
            {
                EditorUtility.DisplayProgressBar("Rename Tool", $"{gameObject.name}", (float)index / targets.Length);
                gameObject.name = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(gameObject.transform.parent, gameObject.name);
            }
            EditorUtility.ClearProgressBar();
        }

        private GameObject[] dupes;
        private void DupScan()
        {
            dupes = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                    .SelectMany(o => o.GetComponentsInChildren<Transform>())
                    .Select(t => t.gameObject)
                    .GroupBy(o => o.name)
                    .Select(g => g.ToArray())
                    .Where(a => a.Length > 2 && a.Select(o => o.transform.parent).Distinct().Count() < a.Length)
                    .SelectMany(g => g)
                    .Distinct()
                    .ToArray();
        }
    }
}
