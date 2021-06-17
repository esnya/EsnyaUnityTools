using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public enum RendererTool
    {
        MaterialReplace,
        AnchorOverride,
    }

    public class RendererTools : EditorWindow
    {

        public RendererTool tool;

        public GameObject rootObject;
        public bool includeChlidren = true;
        public bool includeInactive = true;

        public Transform anchorOverride;

        public Material materialReplaceOnly;
        public Material materialReplaceBy;

        private Vector2 scrollPosition;


        [MenuItem("EsnyaTools/Renderer Tools")]
        private static void ShowWindow()
        {
            var window = GetWindow<RendererTools>();
            window.Show();
        }

        private void Replace(Renderer renderer)
        {
            Undo.RecordObject(renderer, "Replace");
            switch (tool)
            {
                case RendererTool.MaterialReplace:
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => {
                        return (materialReplaceOnly == null || m != materialReplaceOnly) ? m : materialReplaceBy;
                    }).ToArray();
                    break;
                case RendererTool.AnchorOverride:
                    renderer.probeAnchor = anchorOverride;
                    break;
            }
        }

        private bool RendererFilter(Renderer renderer)
        {
            if (includeInactive && !renderer.gameObject.activeInHierarchy) return false;

            switch (tool)
            {
                case RendererTool.MaterialReplace:
                    return materialReplaceOnly == null || renderer.sharedMaterials.Contains(materialReplaceOnly);
                case RendererTool.AnchorOverride:
                    return renderer.probeAnchor != anchorOverride;
                default:
                    return true;
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Renderer Tools");
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
                    case RendererTool.MaterialReplace:
                        if (includeChlidren) EEUI.ObjectFieldWithSelection("Replace Only", ref materialReplaceOnly, true);
                        break;
                }

                EditorGUILayout.Space();

                switch (tool)
                {
                    case RendererTool.MaterialReplace:
                        EEUI.ObjectFieldWithSelection("Replace By", ref materialReplaceBy, true);
                        break;
                    case RendererTool.AnchorOverride:
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
