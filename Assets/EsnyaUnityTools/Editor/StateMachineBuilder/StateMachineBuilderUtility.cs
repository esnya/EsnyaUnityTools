using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace EsnyaFactory
{
    public class StateMachineBuilderUtility
    {
#if VRC_SDK_VRCSDK3
        public static RuntimeAnimatorController GetOrCreatePlayableLayer(VRCAvatarDescriptor avatarDescriptor, VRCAvatarDescriptor.AnimLayerType type, RuntimeAnimatorController template)
        {
            avatarDescriptor.customizeAnimationLayers = true;

            var layer = avatarDescriptor.baseAnimationLayers.Where(l => l.type == type ).First();
            layer.isDefault = false;

            if (layer.animatorController == null) {
                layer.type = type;
                layer.animatorController = Object.Instantiate(template);

                avatarDescriptor.baseAnimationLayers = avatarDescriptor.baseAnimationLayers.Select(l => l.type == type ? layer : l).ToArray();
            }

            return layer.animatorController;
        }
#endif

        public static void AddParameterIfNotExists(AnimatorController animatorController, string name, AnimatorControllerParameterType type) {
            var uniqName = animatorController.MakeUniqueParameterName(name);
            if (uniqName != name) return;

            animatorController.AddParameter(name, type);

            EditorUtility.SetDirty(animatorController);
        }

        public static AnimatorControllerLayer GetOrAddLayer(AnimatorController animatorController, string name, float defaultWeight)
        {
            var layer = animatorController.layers.Where(l => l.name == name).FirstOrDefault();
            if (layer != null) return layer;
            
            var stateMachine = new AnimatorStateMachine() {
                name = name,
            };

            layer = new AnimatorControllerLayer() {
                name = name,
                defaultWeight = defaultWeight,
                stateMachine = stateMachine,
            };

            animatorController.AddLayer(layer);

            EditorUtility.SetDirty(animatorController);

            return layer;
        }

        static void SetupSimpleTransition(AnimatorStateTransition transition, string parameter, float threshold)
        {
            transition.canTransitionToSelf = false;
            transition.conditions = new AnimatorCondition[] {
                new AnimatorCondition() {
                    mode = AnimatorConditionMode.Equals,
                    parameter = parameter,
                    threshold = threshold,
                },
            };
            transition.duration = 0.0f;
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.offset = 0.0f;

            EditorUtility.SetDirty(transition);
        }

        public static void AddSimpleCurve(AnimationClip clip, string relativePath, System.Type type, string propertyName, float value)
        {
            var curve = new AnimationCurve();
            curve.AddKey(0, value);

            clip.SetCurve(relativePath, type, propertyName, curve);
            EditorUtility.SetDirty(clip);
        }

        public static AnimationClip CreateSimpleClip(string relativePath, System.Type type, string propertyName, float value)
        {
            var clip = new AnimationClip() {
                name = $"{relativePath}.{propertyName}={value}",
            };

            AddSimpleCurve(clip, relativePath, type, propertyName, value);

            return clip;
        }

        public static void ClearStateMachine(AnimatorStateMachine stateMachine)
        {
            foreach (var state in stateMachine.states) {
                stateMachine.RemoveState(state.state);
            }
            foreach (var child in stateMachine.stateMachines) {
                stateMachine.RemoveStateMachine(child.stateMachine);
            }
            EditorUtility.SetDirty(stateMachine);
        }
    }
}
