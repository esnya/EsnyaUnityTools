using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EsnyaFactory
{
    public class SelectionTool : EditorWindow
    {

        [MenuItem("EsnyaTools/Selection Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<SelectionTool>();
            window.Show();

        }

        private SerializedObject serializedObject;
        public bool includeInactive = false;

        private void OnEnable()
        {
            titleContent = new GUIContent("Selection Tool");
            serializedObject = new SerializedObject(this);
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Select Children"))
            {
                Selection.objects = Selection.transforms.SelectMany(t => Enumerable.Range(0, t.childCount).Select(t.GetChild)).Select(t => t.gameObject).ToArray();
            }

            if (GUILayout.Button("Select Siblings"))
            {
                Selection.objects = Selection.transforms.SelectMany(t => Enumerable.Range(0, t.parent.childCount).Select(t.parent.GetChild)).Select(t => t.gameObject).ToArray();
            }

            if (GUILayout.Button("Select Parents"))
            {
                Selection.objects = Selection.transforms.Select(t => t.parent?.gameObject).Where(o => o != null).ToArray();
            }

            if (GUILayout.Button("Select By Mesh"))
            {
                var meshes = Selection.gameObjects.SelectMany(o => o.GetComponents<MeshFilter>()).Select(f => f.sharedMesh).Where(mesh => mesh != null).Distinct().ToImmutableDictionary(mesh => mesh.GetHashCode());
                Selection.objects = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MeshFilter>(includeInactive)).Select(f => f.sharedMesh).Where(mesh => mesh != null && meshes.ContainsKey(mesh.GetHashCode())).ToArray();
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(includeInactive)));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
