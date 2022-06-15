using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using Debug = UnityEngine.Debug;

namespace EsnyaFactory
{
    public class Npm
    {
        public enum VersionIncrementLevel
        {
            major,
            minor,
            patch,
            premajor,
            preminor,
            prepatch,
            prerelease,
            fromGit
        }

        private static readonly Regex PreIdPattern = new Regex("^[A-Za-z-][0-9A-Za-z-]*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex ArgumentPattern = new Regex("^/?[A-Za-z0-9_+-]+$", RegexOptions.Compiled | RegexOptions.Multiline);

        private static string BuildArguments(IEnumerable<string> arguments)
        {
            return string.Join(" ", arguments.Where(a => ArgumentPattern.IsMatch(a)));
        }

        private static void Exec(string command, IEnumerable<string> arguments, string workingDirectory = ".")
        {
            var process = Process.Start(new ProcessStartInfo()
            {
                FileName = command,
                Arguments = BuildArguments(arguments),
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = System.Text.Encoding.ASCII,
                StandardOutputEncoding = System.Text.Encoding.ASCII,
            });
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null) Debug.LogError(e.Data);
            };
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null) Debug.Log(e.Data);
            };
            process.EnableRaisingEvents = true;
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

        private static void ExecCommand(string command, IEnumerable<string> arguments, string workingDirectory = ".")
        {
            Debug.Log($"{command} {BuildArguments(arguments)}");
#if UNITY_EDITOR_WIN
            Exec("cmd", arguments.Prepend(command).Prepend("/C"), workingDirectory);
#else
            Exec(command, arguments, workingDirectory);
#endif
        }

        public static void IncrementVersion(VersionIncrementLevel releaseType, bool force = false, string preId = null, string workingDirectory = ".")
        {
            var isValidPreId = !string.IsNullOrEmpty(preId) && PreIdPattern.IsMatch(preId);
            var arguments = new[] {
                "version",
                releaseType == VersionIncrementLevel.fromGit ? "from-git" : releaseType.ToString(),
                force ? "--force" : null,
                isValidPreId ? "--preid" : null,
                isValidPreId ? preId : null,
            }
            .Where(s => s != null);
            ExecCommand("npm", arguments, workingDirectory);
        }
    }
}
