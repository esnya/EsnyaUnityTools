using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

#if VRC_SDK_VRCSDK3 && !UDON
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace EsnyaFactory
{
    public class SimpleToggleLayerGenerator : LayerGenerator.Generator
    {
        public string parameter;
        public bool defaultState;
        public bool writeDefaultValues;
        public Motion trueMotion;
        public Motion falseMotion;
        public float duration = 0.25f;

#if VRC_SDK_VRCSDK3 && !UDON
        public VRCAvatarDescriptor avatarDescriptor;
        public bool installExpressionsMenu;
        public string menuName;
        public Texture2D menuIcon;
        public bool saveState;
#endif

        public override string GetName()
        {
            return "Simple Toggle";
        }

        public override VisualElement CreateGUI()
        {
            return Resources.Load<VisualTreeAsset>("UI/SimpleToggleLayerGenerator").CloneTree(null);
        }

        static AnimatorControllerParameter MakeAnimatorControllerParameter(AnimatorController animatorController, string name)
        {
            EditorUtility.SetDirty(animatorController);

            if (animatorController.MakeUniqueParameterName(name) == name) {
                var newParameter = new AnimatorControllerParameter() {
                    name = name,
                };
                animatorController.AddParameter(newParameter);

                return newParameter;
            }
            var parameter = animatorController.parameters.First(p => p.name == name);

            return parameter;
        }

        static AnimatorState MakeAnimatorState(AnimatorStateMachine stateMachine, Vector3 position, ref List<Object> objects)
        {
            EditorUtility.SetDirty(stateMachine);

            var animatorState = new AnimatorState();
            objects.Add(animatorState);
            stateMachine.AddState(animatorState, position);

            return animatorState;
        }

        static AnimatorStateTransition MakeAnimatorStateTransition(AnimatorState animatorState, ref List<Object> objects)
        {
            EditorUtility.SetDirty(animatorState);

            var animatorStateTransition = new AnimatorStateTransition() {};
            objects.Add(animatorStateTransition);
            animatorState.AddTransition(animatorStateTransition);

            return animatorStateTransition;
        }
#if VRC_SDK_VRCSDK3 && !UDON
        static VRCExpressionParameters MakeExpressionParameters(VRCAvatarDescriptor avatarDescriptor, ref List<Object> objects)
        {
            EditorUtility.SetDirty(avatarDescriptor);
            avatarDescriptor.customExpressions = true;

            if (avatarDescriptor.expressionParameters == null) {
                var defaultAsset = AssetDatabase.LoadAssetAtPath<VRCExpressionParameters>("Assets/VRCSDK/Examples3/Expressions Menu/DefaultExpressionParameters.asset");
                avatarDescriptor.expressionParameters = Object.Instantiate(defaultAsset);
                avatarDescriptor.expressionParameters.name = $"Expression Parameters for {avatarDescriptor.gameObject.name}";
                objects.Add(avatarDescriptor.expressionParameters);
            }

            return avatarDescriptor.expressionParameters;
        }

        static VRCExpressionParameters.Parameter MakeExpressionParameter(VRCExpressionParameters expressionParameters, string name)
        {
            var parameter = expressionParameters.parameters.FirstOrDefault(p => p.name == name);
            if (parameter != null) return parameter;

            EditorUtility.SetDirty(expressionParameters);
            var emptyParameter = expressionParameters.parameters.FirstOrDefault(p => p == null || string.IsNullOrEmpty(p.name));
            if (emptyParameter != null) {
                emptyParameter.name = name;
                return emptyParameter;
            }

            var newParameter = new VRCExpressionParameters.Parameter() {
                name = name,
            };
            expressionParameters.parameters = expressionParameters.parameters.Append(newParameter).ToArray();
            return newParameter;
        }

        static VRCExpressionsMenu MakeExpressionsMenu(VRCAvatarDescriptor avatarDescriptor, ref List<Object> objects)
        {
            EditorUtility.SetDirty(avatarDescriptor);
            avatarDescriptor.customExpressions = true;

            if (avatarDescriptor.expressionsMenu == null) {
                avatarDescriptor.expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                avatarDescriptor.expressionsMenu.name = $"Expressions Menu for {avatarDescriptor.gameObject.name}";
                objects.Add(avatarDescriptor.expressionsMenu);
            }

            return avatarDescriptor.expressionsMenu;
        }

        static VRCExpressionsMenu.Control MakeExpressionsMenuControl(VRCExpressionsMenu expressionsMenu, string parameter, VRCExpressionsMenu.Control.ControlType type, float value)
        {
            EditorUtility.SetDirty(expressionsMenu);
            var control = expressionsMenu.controls.FirstOrDefault(c => c.parameter?.name == parameter && c.type == type && c.value == value);
            if (control != null) return control;

            var newControl = new VRCExpressionsMenu.Control() {
                parameter = new VRCExpressionsMenu.Control.Parameter() {
                    name = parameter,
                },
                type = type,
                value = value,
            };
            expressionsMenu.controls.Add(newControl);

            return newControl;
        }
#endif

        public override IEnumerable<Object> Generate(AnimatorController animatorController, AnimatorStateMachine stateMachine)
        {
            ExAnimatorUtility.ClearStateMachine(stateMachine);

            var objects = new List<Object>();
#if VRC_SDK_VRCSDK3 && !UDON
            if (installExpressionsMenu) {
                var expressionParameters = MakeExpressionParameters(avatarDescriptor, ref objects);
                var expressionParameter = MakeExpressionParameter(expressionParameters, parameter);
                expressionParameter.valueType = VRCExpressionParameters.ValueType.Bool;
                expressionParameter.defaultValue = defaultState ? 1 : 0;

                var expressionsMenu = MakeExpressionsMenu(avatarDescriptor, ref objects);
                var expressionsMenuControl = MakeExpressionsMenuControl(expressionsMenu, parameter, VRCExpressionsMenu.Control.ControlType.Toggle, 1);
                expressionsMenuControl.name = menuName;
                expressionsMenuControl.icon = menuIcon;
            }
#endif

            var animatorControllerParameter = MakeAnimatorControllerParameter(animatorController, parameter);
            animatorControllerParameter.type = AnimatorControllerParameterType.Bool;
            animatorControllerParameter.defaultBool = defaultState;

            var falseState = MakeAnimatorState(stateMachine, new Vector3(500, -100, 0), ref objects);
            falseState.name = "false";
            falseState.writeDefaultValues = writeDefaultValues;
            falseState.motion = falseMotion;

            var trueState = MakeAnimatorState(stateMachine, new Vector3(500, 100, 0), ref objects);
            trueState.name = "true";
            trueState.writeDefaultValues = writeDefaultValues;
            trueState.motion = trueMotion;

            stateMachine.defaultState = defaultState ? trueState : falseState;

            var onTrueTransition = MakeAnimatorStateTransition(falseState, ref objects);
            onTrueTransition.name = "On True";
            onTrueTransition.duration = duration;
            onTrueTransition.hasExitTime = false;
            onTrueTransition.destinationState = trueState;
            onTrueTransition.conditions = new AnimatorCondition[] { new AnimatorCondition() { mode = AnimatorConditionMode.If, parameter = parameter }};

            var onFalseTransition = MakeAnimatorStateTransition(trueState, ref objects);
            onFalseTransition.name = "On False";
            onFalseTransition.duration = duration;
            onFalseTransition.hasExitTime = false;
            onFalseTransition.destinationState = falseState;
            onFalseTransition.conditions = new AnimatorCondition[] { new AnimatorCondition() { mode = AnimatorConditionMode.IfNot, parameter = parameter }};

            return objects;
        }
    }
}
