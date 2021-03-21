#if UDON && VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Udon;

namespace EsnyaFactory
{
    [Serializable]
    public class UdonGather
    {
        [SerializeField] private static string directory;
        private static IEnumerable<AbstractUdonProgramSource> EnumerateSelectedSerializedProgramAssets()
        {
            return Selection.objects
                .Select(o => o as AbstractUdonProgramSource)
                .Where(a => a?.SerializedProgramAsset != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(a?.SerializedProgramAsset)));
        }

        [MenuItem("Assets/EsnyaTools/Gather Udon")]
        [MenuItem("Assets/EsnyaTools/Gather Udon")]
        private static void GatherUdon()
        {
            var sources = EnumerateSelectedSerializedProgramAssets().ToList();
            directory = EditorUtility.SaveFolderPanel("Gather Udon Serialized Program Assets", directory, "");

            if (string.IsNullOrEmpty(directory)) return;

            var targets = sources
                .Select(source =>
                {
                    var asset = source.SerializedProgramAsset;
                    var src = AssetDatabase.GetAssetPath(asset);

                    var dst = $"{directory}/{Path.GetFileName(src)}";

                    return (src, dst);
                })
                .Where(t => t.src != t.dst);
            foreach (var (src, dst) in targets)
            {
                MoveOrReplace(src, dst);
                MoveOrReplace($"{src}.meta", $"{dst}.meta");
            };

            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/EsnyaTools/Gather Udon", true)]
        [MenuItem("Assets/EsnyaTools/Refine Udon", true)]
        private static bool GatharUdonValid()
        {
            return EnumerateSelectedSerializedProgramAssets().Any();
        }

        private static void MoveOrReplace(string src, string dst)
        {
            try
            {
                Debug.Log($"Moving {src} to {dst}");
                if (File.Exists(dst)) File.Delete(dst);
                File.Move(src, dst);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        [MenuItem("Assets/EsnyaTools/Refine Udon")]
        [MenuItem("Assets/EsnyaTools/Refine Udon")]
        private static void RefineUdon()
        {
            directory = EditorUtility.OpenFolderPanel("Refine Udon Serialized Program Assetas", directory, "");
            if (string.IsNullOrEmpty(directory)) return;

            var sourcePaths = EnumerateSelectedSerializedProgramAssets().Select(source => AssetDatabase.GetAssetPath(source.SerializedProgramAsset)).ToList();
            var paths = AssetDatabase.FindAssets("t:AbstractSerializedUdonProgramAsset", new[] { directory.Replace(Application.dataPath, "Assets") })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !sourcePaths.Where(sourcePath => sourcePath == path).Any());
            foreach (var src in paths)
            {
                var dst = $"Assets/SerializedUdonPrograms/{Path.GetFileName(src)}";
                MoveOrReplace(src, dst);
                MoveOrReplace($"{src}.meta", $"{dst}.meta");
            }
        }
    }
}
#endif
