using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
                Undo.RecordObjects(Selection.gameObjects, "Rename Tool");
                foreach (var gameObject in Selection.gameObjects.OrderBy(o => o.transform.GetSiblingIndex()))
                {
                    gameObject.name = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(gameObject.transform.parent, gameObject.name);
                }
            }
        }
    }
}
