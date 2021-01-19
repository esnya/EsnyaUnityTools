using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace EsnyaFactory
{
    class SimpleSwitchCaseGenerator : StateMachineGenerator {
        bool writeDefaults;
        string parameter;
        string normalizedTime;
        int maxValue = 0;
        float transitionDuration = 0.25f;
        Motion entryClip;
        Motion exitClip;
        List<ObjectField> caseClipFields;

        public SimpleSwitchCaseGenerator()
        {
            name = "Simple Switch Case";
        }

        VisualElement GenerateCaseElements() {
            var box = new Box();

            if (caseClipFields == null) caseClipFields = new List<ObjectField>();

            caseClipFields = caseClipFields.Take(maxValue + 1).ToList();

            while (caseClipFields.Count < maxValue + 1)
            {
                caseClipFields.Add(new ObjectField() {
                    objectType = typeof(Motion),
                });
            }

            int i = 0;
            foreach (var f in caseClipFields) {
                box.Add(new FieldLabel($"{i++}"));
                box.Add(f);
            }

            return box;
        }

        public override VisualElement GetPropertyElements() {
            var box = new Box();
            box.style.paddingLeft = Constants.spacing;
            box.style.paddingRight = Constants.spacing;
            box.style.paddingBottom = Constants.spacing;

            var caseBox = new Box();

            box.Add(new FieldLabel("Parameter Name"));
            var textField = new TextField();
            box.Add(textField);
            textField.OnValueChanged(e => parameter = e.newValue);

            box.Add(new FieldLabel("Notmalized Time"));
            textField = new TextField();
            box.Add(textField);
            textField.OnValueChanged(e => normalizedTime = e.newValue);

            box.Add(new FieldLabel("Max Value"));
            var intField = new IntegerField();
            box.Add(intField);
            intField.OnValueChanged(e => {
                maxValue = e.newValue;
                caseBox.Clear();
                caseBox.Add(GenerateCaseElements());
            });

            box.Add(new FieldLabel("Transition Duration"));
            var floatField = new FloatField();
            box.Add(floatField);
            floatField.value = transitionDuration;
            floatField.OnValueChanged(e => transitionDuration = e.newValue);

            box.Add(new FieldLabel("Write Defaults"));
            var toggle = new Toggle();
            box.Add(toggle);
            toggle.OnValueChanged(e => writeDefaults = e.newValue);

            box.Add(new FieldLabel("Clips"));
            caseBox.Add(GenerateCaseElements());
            box.Add(caseBox);

            box.Add(new FieldLabel("Entry Clip"));
            var objectField = new ObjectField();
            box.Add(objectField);
            objectField.objectType = typeof(Motion);
            objectField.OnValueChanged(e => entryClip = e.newValue as Motion);

            box.Add(new FieldLabel("Exit Clip"));
            objectField = new ObjectField();
            box.Add(objectField);
            objectField.objectType = typeof(Motion);
            objectField.OnValueChanged(e => exitClip = e.newValue as Motion);

            return box;
        }

        public override IEnumerable<Object> Generate(AnimatorController animatorController, AnimatorStateMachine stateMachine)
        {
            StateMachineBuilderUtility.AddParameterIfNotExists(animatorController, parameter, AnimatorControllerParameterType.Int);

            var useNormalizedTime = !string.IsNullOrEmpty(normalizedTime);
            if (useNormalizedTime)
            {
                StateMachineBuilderUtility.AddParameterIfNotExists(animatorController, normalizedTime, AnimatorControllerParameterType.Float);
            }

            StateMachineBuilderUtility.ClearStateMachine(stateMachine);

            var entryState = new AnimatorState() {
                name = "Entry",
                motion = entryClip,
                writeDefaultValues = writeDefaults,
            };
            stateMachine.AddState(entryState, new Vector3(0, 0, 0));

            var exitState = new AnimatorState() {
                name = "Exit",
                motion = exitClip,
                writeDefaultValues = writeDefaults,
            };
            stateMachine.AddState(exitState, new Vector3(1000, 0, 0));
            var exitTransition = exitState.AddExitTransition();
            exitTransition.name = "Exit Transition";
            exitTransition.hasExitTime = true;
            exitTransition.hasFixedDuration = true;
            exitTransition.duration = transitionDuration;
            exitTransition.exitTime = 0.01f;

            var objects = new List<Object>() {
                entryState,
                exitState,
                exitTransition,
            };

            for (int i = 0; i <= maxValue; i++)
            {
                var state = new AnimatorState() {
                    name = $"{i}",
                    motion = caseClipFields[i].value as Motion,
                    writeDefaultValues = writeDefaults,
                    timeParameter = normalizedTime,
                    timeParameterActive = useNormalizedTime,
                };
                objects.Add(state);
                stateMachine.AddState(state, new Vector3(500, i * 100, 0));

                var entryTransition = new AnimatorStateTransition() {
                    name = $"Enter into {i}",
                    destinationState = state,
                    conditions = new [] {
                        new AnimatorCondition() {
                            mode = AnimatorConditionMode.Equals,
                            parameter = parameter,
                            threshold = i,
                        },
                    },
                    duration = transitionDuration,
                    hasExitTime = false,
                    hasFixedDuration = true,
                    offset = 0,
                };
                objects.Add(entryTransition);
                entryState.AddTransition(entryTransition);
                
                var leaveTransiton = new AnimatorStateTransition() {
                    name = $"Leave from {i}",
                    destinationState = exitState,
                    conditions = new [] {
                        new AnimatorCondition() {
                            mode = AnimatorConditionMode.NotEqual,
                            parameter = parameter,
                            threshold = i,
                        },
                    },
                    duration = transitionDuration,
                    hasExitTime = false,
                    hasFixedDuration = true,
                    offset = 0,
                };
                objects.Add(leaveTransiton);
                state.AddTransition(leaveTransiton);
            }

            return objects;
        }
    }
}
