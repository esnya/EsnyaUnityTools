using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.VersionControl;
using UnityEngine;

namespace EsnyaFactory
{
    public class AnimationTool : EditorWindow
    {
        public AnimatorController animatorController;
        public AnimationClip animationClip;
        public string parameterName;
        public int layerIndex;
        public float transitionDuration = 0.25f;
        public float animationSpeed = 1.0f;

        private SerializedObject serializedObject;

        [MenuItem("EsnyaTools/Animation Tool")]
        private static void ShowWindow()
        {
            GetWindow<AnimationTool>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Animation Tool");
            serializedObject = new SerializedObject(this);
        }

        private void OnGUI()
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
             while (property.NextVisible(false))
             {
                if (property.name == nameof(parameterName))
                {
                    if (animatorController)
                    {
                        var parameterNames = animatorController.parameters.Select(p => p.name).ToList();
                        var currentIndex = Mathf.Max(parameterNames.IndexOf(parameterName), 0);
                        var selectedIndex = EditorGUILayout.Popup(property.displayName, currentIndex, parameterNames.ToArray());
                        parameterName = parameterNames.Skip(selectedIndex).FirstOrDefault();
                    }
                }
                else if (property.name == nameof(layerIndex))
                {
                    if (animatorController)
                    {
                        layerIndex = EditorGUILayout.Popup("Layer", layerIndex, animatorController.layers.Select(l => l.name).ToArray());
                    }
                }
                else {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Update Transition Duration"))
            {
                foreach (var state in animatorController.layers[layerIndex].stateMachine.states.Select(a => a.state))
                {
                    Undo.RecordObject(state, "Update Transition Duration");
                    foreach (var transition in state.transitions)
                    {
                        transition.duration = transitionDuration;
                    }
                    EditorUtility.SetDirty(state);
                }
            }

            if (GUILayout.Button("Update Animation Speed"))
            {
                foreach (var state in animatorController.layers[layerIndex].stateMachine.states.Select(a => a.state))
                {
                    Undo.RecordObject(state, "Update Animation Speed");
                    state.speed = animationSpeed * Mathf.Sign(state.speed);
                    EditorUtility.SetDirty(state);
                }
            }

            if (GUILayout.Button("Single Animation Layer"))
            {
                var layerName = parameterName;
                var layer = AddLayerWithWeiht(animatorController, layerName);

                var stateMachine = animatorController.layers[layerIndex].stateMachine;
                var state = stateMachine.AddState(animationClip.name);
                state.motion = animationClip;
                state.timeParameterActive = true;
                state.timeParameter = parameterName;

                stateMachine.defaultState = state;

                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("Simple Toggle Layer"))
            {
                var layerName = parameterName;
                var layer = AddLayerWithWeiht(animatorController, layerName);

                var stateMachine = layer.stateMachine;

                var offState = stateMachine.AddState("Off");
                offState.motion = animationClip;
                offState.speed = -1;

                var onState = stateMachine.AddState("On");
                onState.motion = animationClip;

                var onTransiton = offState.AddTransition(onState);
                onTransiton.AddCondition(AnimatorConditionMode.If, 0, parameterName);

                var offTransition = onState.AddTransition(offState);
                offTransition.AddCondition(AnimatorConditionMode.IfNot, 0, parameterName);

                onTransiton.hasExitTime = offTransition.hasExitTime = false;
                onTransiton.duration = offTransition.duration = 0;
            }
        }

        private static AnimatorControllerLayer AddLayerWithWeiht(AnimatorController animatorController, string name, float defaultWeight = 1.0f)
        {
            var layer = new AnimatorControllerLayer()
            {
                name = name,
                defaultWeight = defaultWeight,
                stateMachine = new AnimatorStateMachine()
                {
                    name = name,
                    hideFlags = HideFlags.HideInHierarchy,
                },
            };
            animatorController.AddLayer(layer);

            if (AssetDatabase.GetAssetPath(animatorController) != "")
            {
                AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(animatorController));
            }

            return layer;
        }

        [MenuItem("Assets/EsnyaTools/Auto Mask")]
        private static void AutoMaskMenu()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var clip in Selection.objects.Where(o => o is AnimationClip).Select(o => o as AnimationClip))
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as ModelImporter;
                    AutoMask(importer, clip);
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        [MenuItem("Assets/EsnyaTools/Auto Mask", true)]
        private static bool AutoMaskMenuValidate() => Selection.objects.Where(o => o is AnimationClip).Any(o => IsChildOfModel(o as AnimationClip));


        [MenuItem("Assets/EsnyaTools/Unmask")]
        private static void UnmaskMenu()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var clip in Selection.objects.Where(o => o is AnimationClip).Select(o => o as AnimationClip))
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as ModelImporter;
                    AutoMask(importer, clip, true);
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
        [MenuItem("Assets/EsnyaTools/Unmask", true)]
        private static bool UnmaskMenuValidate() => Selection.objects.Where(o => o is AnimationClip).Any(o => IsChildOfModel(o as AnimationClip));

        private static bool IsChildOfModel(AnimationClip clip)
        {
            if (!clip) return false;

            var path = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(path)) return false;

            return AssetImporter.GetAtPath(path) is ModelImporter;
        }

        public static void AutoMask(ModelImporter modelImporter, AnimationClip clip, bool unmask = false)
        {
            var serializedImporter = new SerializedObject(modelImporter);

            var clipAnimationIndex = modelImporter.clipAnimations.Select(a => a.name).TakeWhile(a => a != clip.name).Count();
            var clipAnimationProperty = serializedImporter.FindProperty("m_ClipAnimations").GetArrayElementAtIndex(clipAnimationIndex);
            clipAnimationProperty.FindPropertyRelative(nameof(ModelImporterClipAnimation.maskType)).intValue = (int)(unmask ? ClipAnimationMaskType.None : ClipAnimationMaskType.CreateFromThisModel);

            var maskProperty = clipAnimationProperty.FindPropertyRelative("transformMask");

            if (unmask)
            {
                maskProperty.arraySize = 0;
            }
            else
            {
                var mask = AnimationUtility.GetCurveBindings(clip).Select((binding) =>
                {
                    var path = binding.path;
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    var variations = curve.keys.Select(a => a.value).Distinct().Count();
                    return (path, variations);
                }).GroupBy(b => b.path, (path, group) =>
                {
                    var value = group.Select(a => a.variations).Any(i => i > 1);
                    return (path, value);
                }).ToArray();

                maskProperty.arraySize = mask.Length;
                foreach (var (path, value, i) in mask.Select((a, i) => (a.path, a.value, i)))
                {
                    var elementProperty = maskProperty.GetArrayElementAtIndex(i);
                    elementProperty.FindPropertyRelative("m_Path").stringValue = path;
                    elementProperty.FindPropertyRelative("m_Weight").floatValue = (value || unmask) ? 1.0f : 0.0f;
                }
            }
            serializedImporter.ApplyModifiedProperties();
        }
    }
}
