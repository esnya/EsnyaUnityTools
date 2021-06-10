using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public enum RendererUtilityTool
    {
        MaterialReplace,
        AnchorOverride,
    }

    public class RendererUtility : EditorWindow
    {

        public RendererUtilityTool tool;

        public GameObject rootObject;
        public bool includeChlidren = true;
        public bool includeInactive = true;

        public Transform anchorOverride;

        public Material materialReplaceOnly;
        public Material materialReplaceBy;

        private Vector2 scrollPosition;


        [MenuItem("EsnyaTools/Renderer Utility")]
        private static void ShowWindow()
        {
            var window = GetWindow<RendererUtility>();
            window.Show();
        }

        private void Replace(Renderer renderer)
        {
            Undo.RecordObject(renderer, "Replace");
            switch (tool)
            {
                case RendererUtilityTool.MaterialReplace:
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => {
                        return (materialReplaceOnly == null || m != materialReplaceOnly) ? m : materialReplaceBy;
                    }).ToArray();
                    break;
                case RendererUtilityTool.AnchorOverride:
                    renderer.probeAnchor = anchorOverride;
                    break;
            }
        }

        private bool RendererFilter(Renderer renderer)
        {
            if (includeInactive && !renderer.gameObject.activeInHierarchy) return false;

            switch (tool)
            {
                case RendererUtilityTool.MaterialReplace:
                    return materialReplaceOnly == null || renderer.sharedMaterials.Contains(materialReplaceOnly);
                case RendererUtilityTool.AnchorOverride:
                    return renderer.probeAnchor != anchorOverride;
                default:
                    return true;
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Renderer Utility");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            EEUI.Toolbar(ref tool);

            EEUI.Scroll(ref scrollPosition, () => {
                EditorGUILayout.Space();

                EEUI.ObjectFieldWithSelection("Root Object", ref rootObject, true);
                if (rootObject == null) return;

                EEUI.ValueField("Include Children", ref includeChlidren);
                if (includeChlidren) EEUI.ValueField("Include Inactive", ref includeInactive);

                var renderers = includeChlidren
                    ? rootObject.GetComponentsInChildren<Renderer>().Where(RendererFilter)
                    : Enumerable.Repeat(rootObject.GetComponent<Renderer>(), 1).Where(r => r != null);

                switch (tool)
                {
                    case RendererUtilityTool.MaterialReplace:
                        if (includeChlidren) EEUI.ObjectFieldWithSelection("Replace Only", ref materialReplaceOnly, true);
                        break;
                }

                EditorGUILayout.Space();

                switch (tool)
                {
                    case RendererUtilityTool.MaterialReplace:
                        EEUI.ObjectFieldWithSelection("Replace By", ref materialReplaceBy, true);
                        break;
                    case RendererUtilityTool.AnchorOverride:
                        EditorGUILayout.Space();
                        EEUI.ObjectFieldWithSelection("Anchor Override", ref anchorOverride, true);
                        break;
                }

                EditorGUILayout.Space();

                EEUI.Button("Replace All", () => {
                    foreach (var renderer in renderers) Replace(renderer);
                });

                EditorGUILayout.Space();

                EEUI.BoldLabel("Renderers");
                foreach (var renderer in renderers)
                {
                    var r = renderer;
                    EEUI.ObjectFieldWithAction(renderer.gameObject.name, ref r, true, "Replace", () => Replace(r));
                }
            });
        }
    }
}
