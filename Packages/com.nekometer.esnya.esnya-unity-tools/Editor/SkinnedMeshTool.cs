using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EsnyaTools
{
    public class SkinnedMeshTool : EditorWindow
    {

        [MenuItem("EsnyaTools/Skinned Mesh Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<SkinnedMeshTool>();
            window.Show();
        }

        private Button ConvertButton => rootVisualElement.Q<Button>("convert");
        private Button BakeButton => rootVisualElement.Q<Button>("bake");
        private Button SaveButton => rootVisualElement.Q<Button>("save");

        private void Awake()
        {
            titleContent = new GUIContent("Skinned Mesh Tool");

            var uiAsset = Resources.Load<VisualTreeAsset>("SkinnedMeshTool");
            var ui = uiAsset.CloneTree();

            rootVisualElement.Add(ui);

            ConvertButton.clicked += () =>
            {
                foreach (var r in skinnedMeshRenderers) ConvertToMeshRenderer(r);
            };

            BakeButton.clicked += () =>
            {
                foreach (var r in skinnedMeshRenderers) BakeIntoMeshRenderer(r);
            };

            SaveButton.clicked += () =>
            {
                foreach (var mesh in skinnedMeshRenderers.Select(r => r.sharedMesh).Distinct())
                {
                    SaveMeshAs(mesh);
                }
            };

            OnSelectionChanged();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private SkinnedMeshRenderer[] skinnedMeshRenderers;
        private void OnSelectionChanged()
        {
            skinnedMeshRenderers = Selection.gameObjects.SelectMany(o => o.GetComponents<SkinnedMeshRenderer>()).ToArray();
            var isValid = skinnedMeshRenderers.Length > 0;
            ConvertButton?.SetEnabled(isValid);
            BakeButton?.SetEnabled(isValid);
            SaveButton?.SetEnabled(isValid);
        }

        public static void ConvertToMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            var meshFilter = skinnedMeshRenderer.gameObject.AddComponent<MeshFilter>();
            Undo.RegisterCreatedObjectUndo(meshFilter, "Convert to Mesh Renderer");

            var meshRenderer = skinnedMeshRenderer.gameObject.AddComponent<MeshRenderer>();
            Undo.RegisterCreatedObjectUndo(meshFilter, "Convert to Mesh Renderer");

            meshFilter.sharedMesh = skinnedMeshRenderer.sharedMesh;
            meshRenderer.sharedMaterials = skinnedMeshRenderer.sharedMaterials;
            meshRenderer.shadowCastingMode = skinnedMeshRenderer.shadowCastingMode;
            meshRenderer.receiveShadows = skinnedMeshRenderer.receiveShadows;
            meshRenderer.lightProbeUsage = skinnedMeshRenderer.lightProbeUsage;
            meshRenderer.reflectionProbeUsage = skinnedMeshRenderer.reflectionProbeUsage;
            meshRenderer.probeAnchor = skinnedMeshRenderer.probeAnchor;
            meshRenderer.motionVectorGenerationMode = skinnedMeshRenderer.motionVectorGenerationMode;
            meshRenderer.allowOcclusionWhenDynamic = skinnedMeshRenderer.allowOcclusionWhenDynamic;

            Undo.DestroyObjectImmediate(skinnedMeshRenderer);
        }

        public static void BakeIntoMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            var bakedMesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(bakedMesh);
            bakedMesh.name = $"{skinnedMeshRenderer.sharedMesh.name}-Baked";

            skinnedMeshRenderer.sharedMesh = bakedMesh;
            ConvertToMeshRenderer(skinnedMeshRenderer);
        }

        public static string SaveMeshAs(Mesh mesh, string directory)
        {
            var path = EditorUtility.SaveFilePanel("Save Mesh As", directory, $"{mesh.name}", ".asset");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            return path;
        }

        [SerializeField] private string saveDirectory = "Assets";
        private void SaveMeshAs(Mesh mesh)
        {
            var newPath = SaveMeshAs(mesh, saveDirectory);
            if (!string.IsNullOrEmpty(newPath)) saveDirectory = Path.GetDirectoryName(newPath);
        }
    }
}
