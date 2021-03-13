using System.Linq;
using System.IO;
#if UDON && VRC_SDK_VRCSDK3
using System.IO;
using UnityEngine;
using UnityEditor;
using VRC.Udon;

namespace EsnyaFactory
{
    public class UdonGather {

        [MenuItem("EsnyaTools/Gather Udon")]
        private static void GatherUdon() {
            var sources = Selection.objects
                .Select(o => o as AbstractUdonProgramSource)
                .Where(a => a?.SerializedProgramAsset != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(a?.SerializedProgramAsset)))
                .ToList();
            var directory = EditorUtility.SaveFolderPanel("Gather Udon Serialized Program Assets", "", "");

            if (string.IsNullOrEmpty(directory)) return;

            sources.ForEach(source => {
                var asset = source.SerializedProgramAsset;
                var src = AssetDatabase.GetAssetPath(asset);

                var dst = $"{directory}/{Path.GetFileName(src)}";
                MoveOrReplace(src, dst);
                MoveOrReplace($"{src}.meta", $"{dst}.meta");
            });

            AssetDatabase.Refresh();
        }

        private static void MoveOrReplace(string src, string dst) {
            Debug.Log($"Moving {src} to {dst}");
            if (File.Exists(dst)) File.Delete(dst);
            File.Move(src, dst);
        }
    }
}
#endif
