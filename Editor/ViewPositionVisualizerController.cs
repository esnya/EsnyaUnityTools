#if VRC_SDK_VRCSDK3 && !UDON
namespace EsnyaFactory {
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    [CustomEditor(typeof(ViewPositionVisualizer))]
    public class ViewPositionVisualizerEditor : Editor {
        public static void DrawCustomEditor(ViewPositionVisualizer visualizer) {
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            DrawCustomEditor(target as ViewPositionVisualizer);
        }
    }

    public class ViewPositionVisualizerController : EditorWindow {

        [MenuItem("EsnyaTools/View Position Visualizer")]
        [MenuItem("CONTEXT/VRCAvatarDescriptor/View Position Visualizer")]
        [MenuItem("CONTEXT/ViewPositionVisualizer/View Position Visualizer")]
        [MenuItem("GameObject/EsnyaTools/View Position Visualizer", false, 20)]
        private static void ShowWindow(MenuCommand menuCommand) {
            var window = GetWindow<ViewPositionVisualizerController>();

            if (menuCommand.context is ViewPositionVisualizer) {
                window.visualizer = menuCommand.context as ViewPositionVisualizer;
            } else if (menuCommand.context is VRCAvatarDescriptor) {
                var avatar = menuCommand.context as VRCAvatarDescriptor;
                var visualizers = GameObject.FindObjectsOfType<ViewPositionVisualizer>();
                window.visualizer = visualizers.FirstOrDefault(v => v.avatar == avatar)
                    ?? visualizers.FirstOrDefault(v => v.avatar == null)
                    ?? ViewPositionVisualizer.AddVisualizerObject(avatar);
            }
            window.Show();
        }

        // [MenuItem("GameObject/View Position Visualizer", true)]
        // private static bool ValidateMenu() {
        //     var target = Selection.activeGameObject?.GetComponent<ViewPositionVisualizer>() ?? Selection.activeGameObject?.GetComponent<ViewPositionVisualizer>();
        //     return target != null;
        // }

        public ViewPositionVisualizer visualizer;

        void OnEnable()
        {
            titleContent = new GUIContent("View Position Visualizer");
        }

        private void OnGUI() {
            EditorGUILayout.BeginHorizontal();
            visualizer = EditorGUILayout.ObjectField("Visualizer Object", visualizer, typeof(ViewPositionVisualizer), true, GUILayout.ExpandWidth(true)) as ViewPositionVisualizer;
            if (GUILayout.Button("Add Visualizer Object")) {
                visualizer = ViewPositionVisualizer.AddVisualizerObject(visualizer?.avatar);
            }
            EditorGUILayout.EndHorizontal();

            if (visualizer == null) return;

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            visualizer.avatar = EditorGUILayout.ObjectField("Avatar", visualizer.avatar, typeof(VRCAvatarDescriptor), true, GUILayout.ExpandWidth(true)) as VRCAvatarDescriptor;
            if (GUILayout.Button("From Scene Seletion")) {
                visualizer.avatar = Selection.activeGameObject.GetComponent<VRCAvatarDescriptor>();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(AnimationMode.InAnimationMode() || visualizer.headLocalViewPosition == Vector3.zero);
            if (GUILayout.Button("Refresh")) {
                visualizer.headLocalViewPosition = Vector3.zero;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            visualizer.upright = EditorGUILayout.Slider("Upright", visualizer.upright, 0.0f, 1.0f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Prone")) {
                visualizer.upright = 0.28f;
            }
            if (GUILayout.Button("Crouching")) {
                visualizer.upright = 0.65f;
            }
            if (GUILayout.Button("Standing")) {
                visualizer.upright = 1.0f;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
