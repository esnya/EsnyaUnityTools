using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

#if UNITY_3018
using UnityEngine.Experimental.UIElements;
#else
using UnityEngine.UIElements;
#endif

namespace EsnyaFactory
{
    public class SimpleSwitchCaseLayerGenerator : LayerGenerator.Generator
    {
        public string parameter;
        public string timeParameter;
        public bool writeDefaultValues;
        public float enterDuration = 0.25f;
        public float exitDuration = 0.25f;
        public Motion entryClip;
        public Motion[] stateClips;

        public bool useTimeParameter {
            get => !string.IsNullOrEmpty(timeParameter);
        }

        public override string GetName()
        {
            return "Simple Switch Case";
        }

        SerializedObject target;
        void Awake() {
            target = new SerializedObject(this);
        }

        public override VisualElement CreateGUI()
        {

#if UNITY_3018
            return  Resources.Load<VisualTreeAsset>("UI/SimpleSwitchCaseLayerGenerator").CloneTree(null);
#else
            return  Resources.Load<VisualTreeAsset>("UI/SimpleSwitchCaseLayerGenerator").CloneTree();
#endif
        }

        public override IEnumerable<Object> Generate(AnimatorController animatorController, AnimatorStateMachine stateMachine)
        {
            ExAnimatorUtility.AddParameterIfNotExists(animatorController, parameter, AnimatorControllerParameterType.Int);
            if (useTimeParameter)
            {
                ExAnimatorUtility.AddParameterIfNotExists(animatorController, timeParameter, AnimatorControllerParameterType.Float);
            }

            ExAnimatorUtility.ClearStateMachine(stateMachine);

            var stateTemplate = new AnimatorState() {
                writeDefaultValues = writeDefaultValues,
                timeParameter = timeParameter,
                timeParameterActive = false,
            };

            var transitionTemplate = new AnimatorStateTransition() {
                hasFixedDuration = true,
                hasExitTime = false,
            };

            var entryState = Object.Instantiate(stateTemplate);
            entryState.name = "Entry";
            entryState.motion = entryClip;
            stateMachine.AddState(entryState, new Vector3(250, 0, 0));

            var objects = new List<Object>() {
                entryState,
            };

            for (int i = 0; i < stateClips.Length; i++) {
                var state = Object.Instantiate(stateTemplate);
                state.name = $"{i}: {stateClips[i]?.name}";
                state.timeParameterActive = useTimeParameter;
                state.motion = stateClips[i];
                objects.Add(state);
                stateMachine.AddState(state, new Vector3(500, i * 100, 0));

                var entryTransition = Object.Instantiate(transitionTemplate);
                entryTransition.name = $"Enter into {i}";
                entryTransition.destinationState = state;
                entryTransition.duration = enterDuration;
                entryTransition.conditions = new AnimatorCondition[] {
                    new AnimatorCondition() {
                        mode = AnimatorConditionMode.Equals,
                        parameter = parameter,
                        threshold = i,
                    },
                };
                objects.Add(entryTransition);
                entryState.AddTransition(entryTransition);

                var exitTransition = Object.Instantiate(transitionTemplate);
                exitTransition.name = $"Leave from {i}";
                exitTransition.duration = exitDuration;
                exitTransition.isExit = true;
                exitTransition.conditions = new AnimatorCondition[] {
                    new AnimatorCondition() {
                        mode = AnimatorConditionMode.NotEqual,
                        parameter = parameter,
                        threshold = i,
                    },
                };
                objects.Add(exitTransition);
                state.AddTransition(exitTransition);
            }

            return objects;
        }
    }
}
