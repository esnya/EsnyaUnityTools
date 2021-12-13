using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public class AlignTool : EditorWindow
    {

        [MenuItem("EsnyaTools/Align Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<AlignTool>();
            window.titleContent = new GUIContent("Align Tool");
            window.Show();

        }

        private SerializedObject serializedObject;

        public int count = 2;
        public Vector3 step = Vector3.right;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(count)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(step)));
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Duplicate Aligned"))
            {
                for (var i = 1; i < count; i++)
                {
                    foreach (var gameObject in Selection.gameObjects.OrderBy(o => o.transform.GetSiblingIndex()))
                    {
                        var duplicated = PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) ? ClonePrefab(gameObject) : Instantiate(gameObject);
                        duplicated.name = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(gameObject.transform.parent, gameObject.name);
                        duplicated.transform.parent = gameObject.transform.parent;
                        duplicated.transform.localPosition = gameObject.transform.localPosition + step * i;
                        duplicated.transform.localRotation = gameObject.transform.localRotation;
                        duplicated.transform.localScale = gameObject.transform.localScale;
                        Undo.RegisterCreatedObjectUndo(duplicated, "Duplicate Aligned");
                    }
                }
            }
        }

        private static GameObject ClonePrefab(GameObject prefabInstance)
        {
            var mods = PrefabUtility.GetPropertyModifications(prefabInstance);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstance));
            var duplicated = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            PrefabUtility.SetPropertyModifications(duplicated, mods);
            return duplicated;
        }

    }
}
