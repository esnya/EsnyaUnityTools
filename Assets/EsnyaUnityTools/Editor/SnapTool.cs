using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace EsnyaFactory
{
    public class SnapTool : EditorWindow
    {

        [MenuItem("EsnyaTools/Snap Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<SnapTool>();
            window.titleContent = new GUIContent("Snap Tool");
            window.Show();

        }

        public SerializedObject serializedObject;
        public float maxDistance = 1.0f;
        public LayerMask layerMask = 0x801;
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(maxDistance)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(layerMask)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(queryTriggerInteraction)));
            serializedObject.ApplyModifiedProperties();

            var transforms = Selection.transforms;

            EditorGUILayout.LabelField($"{transforms.Length} object(s) selected.");

            if (GUILayout.Button("Snap To Ground"))
            {
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
        }
    }
}
