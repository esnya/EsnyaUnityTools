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
        private LOD[] lods;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
        }

        private void OnGUI()
        {
            var lodGroup = Selection.activeGameObject?.GetComponent<LODGroup>();
            if (!lodGroup)
            {
                EditorGUILayout.HelpBox("Select LOD Group", MessageType.Info);
                return;
            }

            var lodGroups = Selection.gameObjects.SelectMany(o => o.GetComponents<LODGroup>());

            if (GUILayout.Button("Load LODs from Selected"))
            {
                lods = lodGroup.GetLODs();
            }

            if (GUILayout.Button("Apply LODs to Selected"))
            {
                foreach (var copyTo in lodGroups)
                {
                    Undo.RecordObject(copyTo, "Apply LODs ");
                    copyTo.SetLODs(
                        lods.Zip(copyTo.GetLODs(), (srcLOD, dstLOD) =>
                        {
                            return new LOD()
                            {
                                fadeTransitionWidth = srcLOD.fadeTransitionWidth,
                                screenRelativeTransitionHeight = srcLOD.screenRelativeTransitionHeight,
                                renderers = dstLOD.renderers,
                            };
                        }).ToArray()
                    );
                }
            }

            if (GUILayout.Button("Recalculate Bounds"))
            {
                foreach (var g in lodGroups) g.RecalculateBounds();
            }

            if (GUILayout.Button("Revert to Prefab"))
            {
                var count = 0;
                foreach (var g in lodGroups.Where(PrefabUtility.IsPartOfAnyPrefab))
                {
                    var serializedLODGroup = new SerializedObject(g);
                    var mods = PrefabUtility.GetPropertyModifications(g).Where(m => m.target is LODGroup);
                    foreach (var mod in mods)
                    {
                        PrefabUtility.RevertPropertyOverride(serializedLODGroup.FindProperty(mod.propertyPath), InteractionMode.AutomatedAction);
                        count++;
                    }
                }

                Debug.Log($"{count} overrides reverted.");
            }

            if (lods != null)
            {
                var orderdIndexes = lods.Select((lod, i) => (lod, i)).OrderBy(t => -t.lod.screenRelativeTransitionHeight).Select(t => t.i).ToArray();
                for (var i = 0; i < lods.Length; i++)
                {
                    EditorGUILayout.LabelField($"LOD{i + 1}");
                    var index = orderdIndexes[i];
                    lods[index].fadeTransitionWidth = EditorGUILayout.FloatField("Fade Transition Width", lods[index].fadeTransitionWidth);
                    lods[index].screenRelativeTransitionHeight = EditorGUILayout.Slider("Screen Relative Transition Height", lods[index].screenRelativeTransitionHeight, 0, 1);
                }
            }
        }
    }
}
