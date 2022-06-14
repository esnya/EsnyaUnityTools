using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.SDK3.Components;
using VRC.SDKBase.Editor.BuildPipeline;

using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace EsnyaFactory
{
    public class WorldAutoNpmVersion : IVRCSDKBuildRequestedCallback
    {
        [Serializable]
        public class PackageInfo
        {
            public string name;
            public string version;
            public string worldId;
            public PrereleaseInfo[] prereleases;
            [NonSerialized] public string rootDirectory;

            public bool IsRerelase(string worldId)
            {
                return worldId == this.worldId;
            }
            public bool IsPrerelease(string worldId)
            {
                return prereleases?.Any(p => p.worldId == worldId) ?? false;
            }
            public string GetPrereleaseChannel(string worldId)
            {
                return prereleases?.Where(p => p.channel.All(c => char.IsLetterOrDigit(c) || c == '-'))?.FirstOrDefault(p => p.worldId == worldId)?.channel;
            }

            private ReleaseType ReleaseTypeDialog()
            {
                return (ReleaseType)EditorUtility.DisplayDialogComplex("Release", "Release type", "Major", "Minor", "Patch"); ;
            }

            private void Exec(string command, string arguments)
            {
                Debug.Log($"{command} {arguments}");
                var process = Process.Start(new ProcessStartInfo()
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = Path.GetFullPath(rootDirectory),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardErrorEncoding = System.Text.Encoding.ASCII,
                    StandardOutputEncoding = System.Text.Encoding.ASCII,
                });
                process.ErrorDataReceived += (sender, e) => {
                    if (e.Data != null) Debug.LogError(e.Data);
                };
                process.OutputDataReceived += (sender, e) => {
                    if (e.Data != null) Debug.Log(e.Data);
                };
                process.EnableRaisingEvents = true;
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();
            }

            private string GetVersionArguments(string worldId)
            {
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) return $"version {version}+android  -f";
                if (IsRerelase(worldId)) return $"version {ReleaseTypeDialog()} -f ";
                if (IsPrerelease(worldId)) return $"version prerelease --preid {GetPrereleaseChannel(worldId)}  -f";
                return null;
            }

            private void ExecNpm(string arguments)
            {
#if UNITY_EDITOR_WIN
                Exec("cmd", $"/C npm {arguments}");
#else
                Exec("npm", arguments);
#endif
            }

            public void Release(string scenePath, string worldId)
            {
                if (!Directory.Exists(rootDirectory))
                {
                    Debug.LogError("Root Directory could not find. Skip releasing.");
                    return;
                }

                var npmVersionArguments = GetVersionArguments(worldId);
                if (string.IsNullOrEmpty(npmVersionArguments))
                {
                    Debug.LogWarning("Could not determinate release type. Skip releasing.");
                    return;
                }

                // Exec("git", $"add {Path.GetFullPath(scenePath)}");
                ExecNpm(npmVersionArguments);
            }
        }
        [Serializable]
        public class PrereleaseInfo
        {
            public string channel;
            public string worldId;
        }

        public enum ReleaseType
        {
            major,
            minor,
            patch,
            prerelease,
            unknown,
        }

        public int callbackOrder => 0;

        private static PackageInfo FindPackage(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath)) return null;

            var jsonPath = Path.Combine(directoryPath, "package.json");
            if (File.Exists(jsonPath))
            {
                var json = File.ReadAllText(jsonPath);
                var packageInfo = JsonUtility.FromJson<PackageInfo>(json);
                packageInfo.rootDirectory = directoryPath;
                return packageInfo;
            }

            return FindPackage(Path.GetDirectoryName(directoryPath));
        }

        [MenuItem("EsnyaTools/WorldAutoNpmVersion/Test")]
        public static void Test()
        {
            new WorldAutoNpmVersion().OnBuildRequested(VRCSDKRequestedBuildType.Scene);
        }

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            try
            {
                if (!EsnyaUdonToolsSettings.Instance.autoNpmPackageVersion) return true;

                if (requestedBuildType != VRCSDKRequestedBuildType.Scene) return true;

                var scene = SceneManager.GetActiveScene();
                var descriptor = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<VRCSceneDescriptor>(true)).FirstOrDefault();
                var blueprintId = descriptor?.GetComponent<PipelineManager>()?.blueprintId;

                if (string.IsNullOrEmpty(blueprintId)) return true;

                var packageInfo = FindPackage(Path.GetDirectoryName(scene.path));
                if (packageInfo == null) return true;

                packageInfo.Release(scene.path, blueprintId);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return true;
        }
    }
}