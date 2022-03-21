using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace EsnyaFactory
{
    public class TransformTool : EditorWindow
    {
        [MenuItem("EsnyaTools/Transform Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<TransformTool>();
            window.Show();

        }

        [Header("Snap")]
        public SerializedObject serializedObject;
        public float maxDistance = 1.0f;
        public LayerMask layerMask = 0x801;
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;


        [Header("Align")]
        public int count = 2;
        public Vector3 step = Vector3.right;
        public Vector3 offset = Vector3.zero;

        private void OnEnable()
        {
            titleContent = new GUIContent("Transform Tool");
            serializedObject = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(maxDistance)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(layerMask)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(queryTriggerInteraction)));

            if (GUILayout.Button("Snap To Ground"))
            {
                var transforms = Selection.transforms;
                foreach (var transform in transforms)
                {
                    Undo.RecordObject(transform, "Snap To Ground");
                    var hitUp = Physics.Raycast(transform.position + Vector3.up * maxDistance, Vector3.down, out var up, maxDistance, layerMask, queryTriggerInteraction);
                    if (hitUp) transform.position = up.point;
                    else
                    {
                        var hitDown = Physics.Raycast(transform.position, Vector3.down, out var down, maxDistance, layerMask, queryTriggerInteraction);
                        if (hitDown) transform.position = down.point;
                    }
                }
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(count)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(step)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(offset)));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Duplicate Aligned x", EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(false)))
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(count)), GUIContent.none);
            }

            serializedObject.ApplyModifiedProperties();
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
