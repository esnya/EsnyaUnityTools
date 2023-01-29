using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.Udon.Editor.ProgramSources;

namespace EsnyaFactory
{
    [Serializable]
    public class CustomUdonProgramFolder
    {
        [InitializeOnLoadMethod]
        public static void RegistterCallbacks()
        {
            AssemblyReloadEvents.afterAssemblyReload += Invoke;
        }

        private const string SerializedUdonPrograms = "SerializedUdonPrograms";
        private static Dictionary<string, string> customUdonProgramDirectoryCache = new Dictionary<string, string>();
        private static string GetCustomUdonProgramFolder(string path)
        {
            if (path == "Assets" || string.IsNullOrEmpty(path)) return $"Assets/{SerializedUdonPrograms}";

            if (!customUdonProgramDirectoryCache.ContainsKey(path))
            {
                var directoryName = Path.GetDirectoryName(path).Replace('\\', '/');
                var uasmDirectory = $"{directoryName}/{SerializedUdonPrograms}";
                customUdonProgramDirectoryCache[path] = Directory.Exists(uasmDirectory) ? uasmDirectory : GetCustomUdonProgramFolder(directoryName);
            }

            return customUdonProgramDirectoryCache[path];

        }

        private static void MoveOrReplaceToDirectory(string src, string dstDirectory)
        {
            var dst = $"{dstDirectory}/{Path.GetFileName(src)}";
            Debug.Log($"Moving {src} to {dst}");
            if (File.Exists(dst)) File.Delete(dst);
            File.Move(src, dst);
        }

        [MenuItem("EsnyaTools/Custom Udon Program Folder/Force Refresh")]
        public static void Invoke()
        {
            if (EsnyaUdonToolsSettings.Instance.customUdonProgramFolder)
            {
                var toMoveList = AssetDatabase.FindAssets("t:UdonAssemblyProgramAsset", new[] { "Assets", "Packages" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(path => (src: AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<UdonAssemblyProgramAsset>(path).SerializedProgramAsset), dst: GetCustomUdonProgramFolder(path)))
                    .Where(t => !t.src.StartsWith(t.dst))
                    .ToArray();

                Debug.Log($"Moving {toMoveList.Length} UdonProgramAsset(s)");

                if (toMoveList.Length > 0)
                {
                    AssetDatabase.StartAssetEditing(); try
                    {
                        foreach (var (src, dst) in toMoveList)
                        {
                            try
                            {
                                MoveOrReplaceToDirectory(src, dst);
                                MoveOrReplaceToDirectory($"{src}.meta", dst);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                            }
                        }
                        AssetDatabase.Refresh();
                    }
                    finally
                    {
                        AssetDatabase.StopAssetEditing();
                    }
                }

            }
        }
    }
}
