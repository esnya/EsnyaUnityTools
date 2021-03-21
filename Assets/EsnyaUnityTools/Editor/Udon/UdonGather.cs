#if UDON && VRC_SDK_VRCSDK3
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using VRC.Udon;

namespace EsnyaFactory
{
    public class UdonGather {
        private static IEnumerable<AbstractUdonProgramSource> EnumerateSelectedSerializedProgramAssets() {
            return Selection.objects
                .Select(o => o as AbstractUdonProgramSource)
                .Where(a => a?.SerializedProgramAsset != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(a?.SerializedProgramAsset)));
        }

        [MenuItem("Assets/EsnyaTools/Gather Udon")][MenuItem("Assets/EsnyaTools/Gather Udon")]
        private static void GatherUdon() {
            var sources = EnumerateSelectedSerializedProgramAssets().ToList();
            var directory = EditorUtility.SaveFolderPanel("Gather Udon Serialized Program Assets", "", "");

            if (string.IsNullOrEmpty(directory)) return;

            var targets = sources
                .Select(source => {
                    var asset = source.SerializedProgramAsset;
                    var src = AssetDatabase.GetAssetPath(asset);

                    var dst = $"{directory}/{Path.GetFileName(src)}";

                    return (src, dst);
                })
                .Where(t => t.src != t.dst);
            foreach (var (src, dst) in targets) {
                MoveOrReplace(src, dst);
                MoveOrReplace($"{src}.meta", $"{dst}.meta");
            };

            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/EsnyaTools/Gather Udon", true)]
        private static bool GatharUdonValid() {
            return EnumerateSelectedSerializedProgramAssets().Any();
        }

        private static void MoveOrReplace(string src, string dst) {
            try {
                Debug.Log($"Moving {src} to {dst}");
                if (File.Exists(dst)) File.Delete(dst);
                File.Move(src, dst);
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }
    }
}
#endif
