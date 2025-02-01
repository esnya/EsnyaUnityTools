using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaTools
{
    public class TerrainTools : EditorWindow
    {

        [MenuItem("EsnyaTools/Terrain Tools")]
        private static void ShowWindow()
        {
            var window = GetWindow<TerrainTools>();
            window.titleContent = new GUIContent("Terrain Tools");
            window.Show();
        }

        private static IEnumerable<Terrain> SelectedTerrains()
        {
            return Selection.gameObjects.SelectMany(o => o.GetComponentsInChildren<Terrain>(true)).Distinct();
        }

        private static IEnumerable<TerrainData> SelectedTerrainData()
        {
            return SelectedTerrains().Select(t => t.terrainData);
        }

        private GameObject src;
        private GameObject dst;
        private void OnTreeReplacerGUI()
        {
            EditorGUILayout.LabelField("Replace Trees", EditorStyles.boldLabel);
            src = EditorGUILayout.ObjectField("Replace from", src, typeof(GameObject), false) as GameObject;
            dst = EditorGUILayout.ObjectField("Replace to", dst, typeof(GameObject), false) as GameObject;
            if (GUILayout.Button("Replace"))
            {
                foreach (var terrainData in SelectedTerrainData())
                {
                    terrainData.treePrototypes = terrainData.treePrototypes.Select(p =>
                    {
                        if (p.prefab != src) return p;
                        p.prefab = dst;
                        return p;
                    }).ToArray();
                    EditorUtility.SetDirty(terrainData);
                }
                AssetDatabase.SaveAssets();
            }
        }

        private AnimationCurve heightRemappingCurve = AnimationCurve.Linear(0, 0, 1, 1);
        private float terrainHeight = 600;
        private void OnHeightRemapperGUI()
        {
            EditorGUILayout.LabelField("Remap Height", EditorStyles.boldLabel);
            heightRemappingCurve = EditorGUILayout.CurveField(heightRemappingCurve);
            if (GUILayout.Button("Remap"))
            {
                var selected = SelectedTerrainData().ToArray();
                try
                {
                    var i = 0;
                    Undo.RecordObjects(selected, "Remap Height");
                    AssetDatabase.StartAssetEditing();
                    foreach (var terrainData in selected)
                    {
                        EditorUtility.DisplayProgressBar("Remap Height", terrainData.name, (float)(i++) / selected.Length);
                        var size = terrainData.heightmapResolution;
                        var heights = terrainData.GetHeights(0, 0, size, size);
                        for (var y = 0; y < size; y++)
                        {
                            for (var x = 0; x < size; x++)
                            {
                                heights[y, x] = Mathf.Clamp01(heightRemappingCurve.Evaluate(heights[y, x]));
                            }
                        }
                        terrainData.SetHeights(0, 0, heights);
                        terrainData.DirtyHeightmapRegion(new RectInt(0, 0, size, size), TerrainHeightmapSyncControl.HeightOnly);
                        terrainData.SyncHeightmap();
                        EditorUtility.SetDirty(terrainData);
                    }
                    AssetDatabase.SaveAssets();
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.StopAssetEditing();
                }
            }

            terrainHeight = EditorGUILayout.FloatField("Heitght", terrainHeight);
            if (GUILayout.Button("Set Height"))
            {
                foreach (var terrainData in SelectedTerrainData())
                {
                    terrainData.size = new Vector3(terrainData.size.x, terrainHeight, terrainData.size.z);
                    EditorUtility.SetDirty(terrainData);
                }
                AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            OnTreeReplacerGUI();
            EditorGUILayout.Space();
            OnHeightRemapperGUI();
        }
    }
}
