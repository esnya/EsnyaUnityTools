using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory.EsnyaUnityTools
{
    public class FFMpegNormalize
    {

        [MenuItem("Assets/EsnyaTools/Normalize (ffmpeg-normalize)", false)]
        private static void NormalizeSelected()
        {
            foreach (var c in Selection.objects.Where(o => o.GetType() == typeof(AudioClip)).Select(o => o as AudioClip))
            {
                Normalize(c);
            }
        }


        [MenuItem("Assets/EsnyaTools/Normalize (ffmpeg-normalize)", true)]
        private static bool IsNormalizeTarget()
        {
            return Selection.objects.Any(o => o.GetType() == typeof(AudioClip));
        }

        private static string GetOrCreateOriginal(string path)
        {
            var directory = Path.GetDirectoryName(path);
            var filename = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);

            var original = Path.Combine(directory, $".{filename}.orig{extension}");

            if (!File.Exists(original)) File.Copy(path, original);

            return original;
        }

        private static void Normalize(AudioClip audioClip)
        {
            var path = $"{new Regex("/Assets/?$").Replace(Application.dataPath, "")}/{AssetDatabase.GetAssetPath(audioClip)}";
            var dst = path;
            var src = GetOrCreateOriginal(path);
            var ext = Path.GetExtension(path);
            var nt = "peak";
            var target = 0;

            try
            {
                Exec("python3", $"-m ffmpeg_normalize -f \"{src}\" -ext \"{ext}\" -nt \"{nt}\" -t \"{target}\" -o \"{dst}\"");
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                if (EditorUtility.DisplayDialog("Error", "ffmpeg-normalize is not installed. Do you want to install now? (Python 3.x required)", "Install", "Cancel"))
                {
                    Exec("pip3", "install ffmpeg-normalize");
                    Normalize(audioClip);
                }
            }
        }

        private static void Exec(string command, string arguments)
        {
            Debug.Log($"{command} {arguments}");
            var process = System.Diagnostics.Process.Start(command, arguments);
            process.WaitForExit();
        }
    }
}
