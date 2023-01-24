using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace EsnyaTools
{
    public class TreeBatchReplacer : EditorWindow
    {

        [MenuItem("EsnyaTools/Tree Batch Replacer")]
        private static void ShowWindow()
        {
            var window = GetWindow<TreeBatchReplacer>();
            window.titleContent = new GUIContent("Tree Batch Replacer");
            window.Show();
        }

        private GameObject src;
        private GameObject dst;
        private void OnGUI()
        {
            src = EditorGUILayout.ObjectField("Replace from", src, typeof(GameObject), false) as GameObject;
            dst = EditorGUILayout.ObjectField("Replace to", dst, typeof(GameObject), false) as GameObject;
            if (GUILayout.Button("Replace"))
            {
                foreach (var terrain in Selection.gameObjects.SelectMany(o => o.GetComponentsInChildren<Terrain>(true)).Distinct())
                {
                    terrain.terrainData.treePrototypes = terrain.terrainData.treePrototypes.Select(p =>
                    {
                        if (p.prefab != src) return p;
                        p.prefab = dst;
                        return p;
                    }).ToArray();
                    EditorUtility.SetDirty(terrain.terrainData);
                }
                AssetDatabase.SaveAssets();
            }
        }
    }
}
