using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public class ColliderTool : EditorWindow
    {

        [MenuItem("EsnyaTools/Collider Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<ColliderTool>();
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Collider Tool");
        }

        private void OnGUI()
        {
            var colliders = Selection.gameObjects.SelectMany(o => o.GetComponentsInChildren<Collider>()).ToArray();

            if (colliders.Length == 0)
            {
                EditorGUILayout.HelpBox("Select GameObject(s) contains Collider(s).", MessageType.Info);
                return;
            }
            var serialized = new SerializedObject(colliders.ToArray());

            EditorGUILayout.PropertyField(serialized.FindProperty("m_IsTrigger"));
            serialized.ApplyModifiedProperties();
        }
    }
}
