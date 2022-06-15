using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace EsnyaFactory
{
    [Serializable]
    public class PrereleaseInfo
    {
        public string channel;
        public string worldId;
    }

    [Serializable]
    public class PackageInfo
    {
        public string name;
        public string version = "1.0.0";
        public string worldId;
        public PrereleaseInfo[] prereleases;
        [NonSerialized] public string rootDirectory;
        public bool autoVersioning;

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

        public void IncrementVersion(string worldId, Npm.VersionIncrementLevel releaseType)
        {
            if (!Directory.Exists(rootDirectory))
            {
                Debug.LogError("Root Directory could not find. Skip releasing.");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                Debug.LogError("Android build target is not supported. Skip releasing.");
                return;
            }

            var preId = releaseType >= Npm.VersionIncrementLevel.premajor ? GetPrereleaseChannel(worldId) : null;
            Npm.IncrementVersion(releaseType, force: true, workingDirectory: rootDirectory, preId: preId);
        }

        public static PackageInfo Find(string directoryPath, bool recursiveUp = false)
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

            return recursiveUp ? Find(Path.GetDirectoryName(directoryPath)) : null;
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(this, true);
            File.WriteAllText(Path.Combine(rootDirectory, "package.json"), json);
        }
    }
}
