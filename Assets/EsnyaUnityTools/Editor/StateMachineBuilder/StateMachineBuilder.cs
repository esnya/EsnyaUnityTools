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
    public class StateMachineBuilder : EditorWindow
    {
        [MenuItem("EsnyaTools/State Machine Builder")]
        static void ShowWindow()
        {
            GetWindow<StateMachineBuilder>().Show();
        }

        void OnEnable()
        {
            titleContent = new GUIContent("State Machine Builder");

            var root = this.GetRootVisualContainer();
            root.style.paddingTop = Constants.spacing;
            root.style.paddingLeft = Constants.spacing;
            root.style.paddingRight = Constants.spacing;

            var controllerBox = new LabeledBox("Animator Controller");
            root.Add(controllerBox);

            var animatorControllerField = new ObjectField();
            controllerBox.Add(animatorControllerField);
            animatorControllerField.objectType = typeof(AnimatorController);

            var layerBox = new LabeledBox("Layer");
            root.Add(layerBox);

            PopupField<string> layerField = null;

            animatorControllerField.OnValueChanged(e => {
                var animatorController = e.newValue as AnimatorController;
                if (layerField != null) layerBox.Remove(layerField);
                if (animatorController != null)
                {
                    layerField = new PopupField<string>(animatorController.layers.Select(l => l.name).ToList(), 0);
                    layerBox.Add(layerField);
                }
            });

            root.Add(new FieldLabel("Generator"));
            var generators = new List<StateMachineGenerator>() {
                new SimpleSwitchCaseGenerator(),
            };
            var generatorField = new PopupField<string>(generators.Select(g => g.name).ToList(), 0);
            root.Add(generatorField);

            root.Add(new FieldLabel("Generator Parameters"));
            var generatorBox = new VisualElement();
            root.Add(generatorBox);

            generatorBox.Add(generators[generatorField.index].GetPropertyElements());

            generatorField.OnValueChanged(e => {
                generatorBox.Clear();
                generatorBox.Add(generators[generatorField.index].GetPropertyElements());
            });

            root.Add(new FieldLabel("Save Path"));
            var savePathField = new TextField();
            savePathField.value = "Assets";
            root.Add(savePathField);

            var generateButton = new Button(() => {
                var animatorController = animatorControllerField.value as AnimatorController;
                var layer = animatorController.layers[layerField.index];
                var objects = generators[generatorField.index].Generate(animatorController, layer.stateMachine);

                var newObjects = objects.Where(o => string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o))).ToList();

                if (newObjects.Count > 0) {
                    var id = System.Guid.NewGuid().ToString();
                    var path = $"{savePathField.value}/{id}.asset";

                    var asset = ScriptableObject.CreateInstance<ScriptableObject>();
                    asset.name = id;
                    AssetDatabase.CreateAsset(asset, path);
                    
                    foreach (var o in newObjects)
                    {
                        if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o))) continue;


                        if (string.IsNullOrEmpty(o.name)) {
                            o.name = System.Guid.NewGuid().ToString();
                        }
                        AssetDatabase.AddObjectToAsset(o, path);
                    }

                    EditorUtility.SetDirty(asset);
                }

                foreach (var o in objects)
                {
                    EditorUtility.SetDirty(o);
                }
                AssetDatabase.Refresh();
            });
            root.Add(generateButton);
            generateButton.text = "Generate";
            generateButton.style.marginTop = Constants.spacing;
        }
    }
}
