using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VRC.Core;
using VRC.SDKBase.Editor.BuildPipeline;

namespace EsnyaFactory
{
    public class WorldIdManager : EditorWindow, IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 200;

        [MenuItem("EsnyaTools/World ID Manager")]
        public static void ShowWindow() => GetWindow<WorldIdManager>().Show();

        private SerializedObject serializedObject;
        public PackageInfo packageInfo;
        public PipelineManager pipelineManager;
        public bool hasPackage;
        public string worldId;
        public bool isRelease;
        public PrereleaseInfo[] prereleases;
        public string releaseChannel;

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);

            titleContent = new GUIContent("World ID Manager");
            Resources.Load<VisualTreeAsset>("WorldIdManager/WorldIdManager").CloneTree(rootVisualElement);
            rootVisualElement.Bind(serializedObject);

            rootVisualElement.Q<Button>("reload").clicked += OnReload;
            rootVisualElement.Q<Button>("detach").clicked += OnDetach;
            rootVisualElement.Q<Button>("change-channel").clicked += OnChangeChannel;
            rootVisualElement.Q<Button>("assign-channel").clicked += OnAssignChannel;
            rootVisualElement.Q<Button>("create-package").clicked += OnCreatePackage;
            rootVisualElement.Q<Button>("manual-increment-version").clicked += ReleaseVersion;

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            FindPackage(SceneManager.GetActiveScene());
        }

        private void OnReload()
        {
            FindPackage(SceneManager.GetActiveScene());
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnFocus()
        {
            UpdateWorldInfo();
        }

        private void OnActiveSceneChanged(Scene curr,
                                          Scene next)
        {
            FindPackage(next);
        }

        private void OnDetach()
        {
            if (pipelineManager == null) return;

            Undo.RecordObject(pipelineManager, "Detach World ID");
            pipelineManager.blueprintId = null;

            UpdateWorldInfo();
        }

        private static EditorWindow ShowDialog(string title, string uxml, Action<EditorWindow> action = null, Action<EditorWindow> onInit = null, bool modal = false)
        {
            var window = CreateInstance<EditorWindow>();
            window.titleContent = new GUIContent(title);

            Resources.Load<VisualTreeAsset>(uxml).CloneTree(window.rootVisualElement);

            var okButton = window.rootVisualElement.Q<Button>("ok");
            if (okButton != null)
            {
                if (action == null) okButton.visible = false;
                else
                {
                    okButton.clicked += () =>
                    {
                        action(window);
                        window.Close();
                    };
                }
            }
            window.rootVisualElement.Q<Button>("cancel").clicked += () => window.Close();

            onInit?.Invoke(window);

            if (modal) window.ShowModalUtility();
            else window.ShowUtility();

            return window;
        }

        private void OnChangeChannel()
        {
            if (pipelineManager == null) return;

            var window = ShowDialog("Change Channel", "WorldIdManager/ChangeChannel");
            var container = window.rootVisualElement.Q("channels");
            var channels = (packageInfo.prereleases ?? Enumerable.Empty<PrereleaseInfo>())
                .Select(p => (p.channel, p.worldId))
                .Prepend((channel: "release", packageInfo.worldId));

            foreach (var (channel, worldId) in channels)
            {
                var button = new Button(() =>
                {
                    window.Close();
                    ChangeChannel(channel);
                })
                {
                    text = $"{channel}: {worldId}",
                };
                button.SetEnabled(channel != releaseChannel);
                container.Add(button);
            }
        }

        private void OnAssignChannel()
        {
            if (pipelineManager == null) return;

            ShowDialog("Assign Channel", "WorldIdManager/AssignChannel", window =>
            {
                var channel = window.rootVisualElement.Q<TextField>("channel").value;
                AssignChannel(channel);
            });
        }

        private void OnCreatePackage()
        {
            serializedObject.ApplyModifiedProperties();
            packageInfo.Save();
            FindPackage(SceneManager.GetActiveScene());
        }

        private void ToggleRootClass(string className, bool enabled)
        {
            if (enabled) rootVisualElement.AddToClassList(className);
            else rootVisualElement.RemoveFromClassList(className);
        }

        private string GetSceneDirectory(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.path)) return null;
            try
            {
                return Path.GetDirectoryName(scene.path);
            }
            finally
            {
            }
            return null;
        }

        private void FindPackage(Scene scene)
        {
            var sceneDirectory = GetSceneDirectory(scene);
            packageInfo = string.IsNullOrEmpty(sceneDirectory) ? null : PackageInfo.Find(sceneDirectory);
            pipelineManager = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<PipelineManager>(true))?.FirstOrDefault();

            hasPackage = packageInfo != null;
            ToggleRootClass("hasPackage", hasPackage);

            if (!hasPackage)
            {
                packageInfo = new PackageInfo()
                {
                    name = new string(scene.name.ToLower().Select(c => !(c >= 'a' && c <= 'z' || c >= '0' && c <= '9') ? '-' : c).ToArray()).Trim('-'),
                    rootDirectory = sceneDirectory,
                    worldId = pipelineManager?.blueprintId,
                };
                serializedObject.Update();
            }

            UpdateWorldInfo();
        }

        private void UpdateWorldInfo()
        {
            worldId = pipelineManager?.blueprintId;
            isRelease = packageInfo?.IsRerelase(worldId) ?? false;
            releaseChannel = isRelease ? "release" : packageInfo?.GetPrereleaseChannel(worldId);

            ToggleRootClass("hasWorldId", !string.IsNullOrEmpty(worldId));
            ToggleRootClass("hasReleaseChannel", !string.IsNullOrEmpty(releaseChannel));
        }

        private void AssignChannel(string channel)
        {
            UpdateWorldInfo();

            if (pipelineManager == null) return;

            if (string.IsNullOrEmpty(worldId))
            {
                Undo.RecordObject(pipelineManager, "Assign World ID");
                pipelineManager.AssignId();
                UpdateWorldInfo();
            }

            if (channel == "release")
            {
                packageInfo.worldId = worldId;
            }
            else
            {
                packageInfo.prereleases = (packageInfo.prereleases ?? Enumerable.Empty<PrereleaseInfo>())
                    .Append(new PrereleaseInfo() { channel = channel, worldId = worldId })
                    .ToArray();
            }
            packageInfo.Save();

            serializedObject.Update();

            UpdateWorldInfo();
        }

        private void ChangeChannel(string channel)
        {
            UpdateWorldInfo();

            if (pipelineManager == null || packageInfo == null) return;

            var worldId = channel == "release" ? packageInfo.worldId : packageInfo.prereleases?.FirstOrDefault(p => p.channel == channel)?.worldId;
            if (string.IsNullOrEmpty(worldId)) return;

            Undo.RecordObject(pipelineManager, "Change World ID");
            pipelineManager.blueprintId = worldId;

            serializedObject.Update();

            UpdateWorldInfo();
        }

        private bool IsRelease() => packageInfo?.IsRerelase(worldId) ?? false;
        private bool IsPrerelease() => packageInfo?.IsPrerelease(worldId) ?? false;

        private void ReleaseVersion()
        {
            FindPackage(SceneManager.GetActiveScene());

            if (packageInfo == null || !packageInfo.IsRerelase(worldId) && !packageInfo.IsPrerelease(worldId)) return;

            Npm.VersionIncrementLevel releaseType = Npm.VersionIncrementLevel.patch;
            ShowDialog("Release Version", "WorldIdManager/IncrementVersion", null,
            window =>
            {
                void SelectVersionIncrementLevel(Npm.VersionIncrementLevel value)
                {
                    releaseType = value;
                    window.Close();
                }
                window.rootVisualElement.Query<Button>().ForEach(button =>
                {
                    if (Enum.TryParse<Npm.VersionIncrementLevel>(button.name, out var value))
                    {
                        button.clicked += () => SelectVersionIncrementLevel(value);
                    }
                });
                window.rootVisualElement.Q("release")?.SetEnabled(IsRelease());
                window.rootVisualElement.Q("pre")?.SetEnabled(IsPrerelease());
            }, true);

            packageInfo.IncrementVersion(worldId, releaseType);
        }

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            try
            {
                if (!EsnyaUdonToolsSettings.Instance.autoNpmPackageVersion) return true;
                if (requestedBuildType != VRCSDKRequestedBuildType.Scene) return true;

                FindPackage(SceneManager.GetActiveScene());
                // if (string.IsNullOrEmpty(worldId)) return true;
                // EsnyaUdonToolsSettings.Instance.lastBuiltWorldId = worldId;
                // EsnyaUdonToolsSettings.Instance.Save();
                ReleaseVersion();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return true;
        }
    }
}
