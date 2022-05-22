using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

namespace EsnyaUnityTools
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PrefabSubset))]
    public class PrefabSubsetEditor : Editor
    {
        public static void Update(PrefabSubset ps)
        {
            if (ps.subsets == null) return;
            if (ps.basePrefab == null) return;

            EditorUtility.DisplayProgressBar("Prefab Subsets", "Updating", 0);

            var basePrefabPath = AssetDatabase.GetAssetPath(ps.basePrefab);
            var psDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(ps));
            foreach (var (subset, i) in ps.subsets.Select((subset, i) => (subset, i)))
            {
                EditorUtility.DisplayProgressBar("Prefab Subsets", $"Updating {subset.name}", (float)i / ps.subsets.Count);

                var prefabPath = (subset.prefab == null ? null : AssetDatabase.GetAssetPath(subset.prefab))
                    ?? AssetDatabase.GenerateUniqueAssetPath($"{psDirectory}/{subset.name}.prefab");

                var baseObject = PrefabUtility.LoadPrefabContents(basePrefabPath);
                var rootObject = baseObject.transform.Find(subset.path)?.gameObject ?? new GameObject();

                var tmpPrefabPath = AssetDatabase.GenerateUniqueAssetPath($"{psDirectory}/{System.Guid.NewGuid()}.prefab");
                PrefabUtility.SaveAsPrefabAsset(rootObject, tmpPrefabPath);
                AssetDatabase.Refresh();

                // Debug.Log($"prefabPath: {prefabPath} ({File.Exists(prefabPath)}), tmpPrefabPath: {tmpPrefabPath}");
                if (File.Exists(prefabPath)) File.Delete(prefabPath);
                File.Move(tmpPrefabPath, prefabPath);
                AssetDatabase.Refresh();

                if (subset.prefab == null)
                {
                    subset.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    EditorUtility.SetDirty(ps);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Add Subset"))
            {
                (target as PrefabSubset).subsets.Add(new PrefabSubset.Subset());
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Update"))
            {
                foreach (var ps in targets.Select(t => t as PrefabSubset).Where(ps => ps != null)) Update(ps);
            }
        }
    }
/*
    public class PrefabSubsetProsessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var prefabSubsets = importedAssets.Select(AssetDatabase.LoadAssetAtPath<PrefabSubset>).Where(ps => ps != null);
            foreach (var ps in prefabSubsets)
            {
                Debug.Log(ps);
            }
        }
    }*/
}
