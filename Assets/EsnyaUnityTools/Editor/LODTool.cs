using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public class LODTool : EditorWindow
    {

        [MenuItem("EsnyaTools/LOD Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<LODTool>();
            window.titleContent = new GUIContent("LOD Tool");
            window.Show();

        }

        private SerializedObject serializedObject;
        public LODGroup src, dst;


        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(src)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(dst)));
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Copy LODs"))
            {
                var lods = src.GetLODs().Zip(dst.GetLODs(), (srcLOD, dstLOD) => {
                    return new LOD() {
                        fadeTransitionWidth = srcLOD.fadeTransitionWidth,
                        screenRelativeTransitionHeight = srcLOD.screenRelativeTransitionHeight,
                        renderers = dstLOD.renderers,
                    };
                }).ToArray();
                Undo.RecordObject(dst, "Copy LODs");
                dst.SetLODs(lods);
            }
        }
    }
}
