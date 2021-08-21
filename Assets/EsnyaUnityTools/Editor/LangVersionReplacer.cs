#pragma warning disable IDE0051

using UnityEditor;
using UnityEngine;
using System.IO;

namespace EsnyaFactory
{
    public class LangVersionReplacer : AssetPostprocessor
    {
        [InitializeOnLoadMethod]
        private static void ReplaceLangVersions()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);

            foreach (var path in Directory.EnumerateFiles(projectRoot, "*.csproj"))
            {
                var content = File.ReadAllText(path);
#if UNITY_2020_2_OR_NEWER
                content = content.Replace("<LangVersion>latest</LangVersion>", "<LangVersion>8.0</LangVersion>");
#elif UNITY_2019_2_OR_NEWER
                content = content.Replace("<LangVersion>latest</LangVersion>", "<LangVersion>7.3</LangVersion>");
#endif
                File.WriteAllText(path, content);
            }
        }

        private static void OnGeneratedCSProjectFiles()
        {
            ReplaceLangVersions();
        }
    }
}
