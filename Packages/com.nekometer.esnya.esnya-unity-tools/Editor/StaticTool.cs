using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public class StaticTool : EditorWindow
    {

        [MenuItem("EsnyaTools/Static Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<StaticTool>();
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Static Tool");
        }

        private void OnGUI()
        {
            if (!Selection.activeGameObject) return;

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                var staticFlags = (StaticEditorFlags)EditorGUILayout.EnumFlagsField(UnityEditor.GameObjectUtility.GetStaticEditorFlags(Selection.activeGameObject));
                if (changeCheck.changed || GUILayout.Button("Force Override"))
                {
                    foreach (var o in Selection.gameObjects) UnityEditor.GameObjectUtility.SetStaticEditorFlags(o, staticFlags);
                }
                if (GUILayout.Button("Apply to Children"))
                {
                    foreach (var o in Selection.activeGameObject.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject)) UnityEditor.GameObjectUtility.SetStaticEditorFlags(o, staticFlags);
                }
            }
        }
    }
}
