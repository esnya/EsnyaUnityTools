using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Runtime.Versioning;
#if UNITY_2018
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#else
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif


namespace EsnyaFactory
{
    [System.Serializable]
    public class LayerGenerator : EditorWindow
    {
        public abstract class Generator : PersistentObject {
            public abstract string GetName();
            public abstract VisualElement CreateGUI();
            public abstract IEnumerable<Object> Generate(AnimatorController animatorController, AnimatorStateMachine stateMachine);
        }

        const string titleText = "Layer Generator";

        [MenuItem("EsnyaTools/" + titleText)]
        static void ShowWindow()
        {
            GetWindow<LayerGenerator>().Show();
        }

        public string savePath = "Assets";
        public Generator[] generators;
        public Generator generator;
        public AnimatorController animatorController;
        public int layerIndex;

#if !UNITY_2018
        private VisualElement GetRootVisualContainer()
        {
            return rootVisualElement;
        }
#endif
        void OnValidate() {
            var isValid = animatorController != null
                && savePath.StartsWith("Assets")
                && layerIndex < animatorController.layers.Length;

            var root = this.GetRootVisualContainer();

            root.Q<Button>("generateButton").SetEnabled(isValid);
        }

        void OnAnimatorControllerChanged(AnimatorController newValue)
        {
            var container = this.GetRootVisualContainer().Q<VisualElement>("Input:layer");
            container.Clear();


            if (newValue != null)
            {
                if (layerIndex >= newValue.layers.Length) layerIndex = 0;
                var popupField = new PopupField<string>(
                    newValue.layers.Select(l => l.name).ToList(),
                    layerIndex
                );
                container.Add(popupField);

#if UNITY_2018
                popupField.OnValueChanged(e2 => layerIndex = popupField.index);
#else
                popupField.RegisterCallback<InputEvent>(e2 => layerIndex = popupField.index, TrickleDown.NoTrickleDown);
#endif
            }
            else
            {
                container.Add(new Label("Select Animator Controller"));
            }
        }

        void OnGeneratorChanged(Generator newValue)
        {
            generator = newValue;

            var container = this.GetRootVisualContainer().Q("generatorProperties");
            container.Clear();

            if (newValue == null)
            {
                container.Add(new Label("Select Generator"));
            }
            else
            {
                var root = newValue.CreateGUI();
                root.Bind(new SerializedObject(generator));
                container.Add(root);
            }
        }

        void OnEnable()
        {
            titleContent = new GUIContent(titleText);

            var data = EditorPrefs.GetString(nameof(LayerGenerator), JsonUtility.ToJson(this, false));
            JsonUtility.FromJsonOverwrite(data, this);

            generators = new Generator[] {
                ScriptableObject.CreateInstance<SimpleToggleLayerGenerator>(),
                ScriptableObject.CreateInstance<SimpleSwitchCaseLayerGenerator>(),
            };

            var target = new SerializedObject(this);

            var root = Resources.Load<VisualTreeAsset>("UI/LayerGeneratorWindow").CloneTree();
#if UNITY_2018
            root.AddStyleSheetPath("UI/LayerGeneratorWindow");
#else
            root.styleSheets.Add(Resources.Load<StyleSheet>("UI/LayerGeneratorWindow"));
#endif
            root.Bind(target);
            this.GetRootVisualContainer().Add(root);

            var objectField = root.Q<ObjectField>("Input:animatorController");

#if UNITY_2018
            objectField.OnValueChanged(e => OnAnimatorControllerChanged(e.newValue as AnimatorController));
#else
            objectField.RegisterCallback<InputEvent>(e => OnAnimatorControllerChanged(objectField.value as AnimatorController));
#endif

            OnAnimatorControllerChanged(animatorController);

            generator = generators.First(g => generator == null ? true : generator.GetName() == g.GetName());

            data = EditorPrefs.GetString(nameof(LayerGenerator), JsonUtility.ToJson(this, false));
            JsonUtility.FromJsonOverwrite(data, generator);

            var generatorField = new PopupField<string>(
                generators.Select(g => g.GetName()).ToList(),
                generators.Select((g, i) => (g, i)).FirstOrDefault(t => t.Item1.GetName() == generator.GetName()).Item2
            );

#if UNITY_2018
            generatorField.OnValueChanged(e => OnGeneratorChanged(generators[generatorField.index]));
#else
            generatorField.RegisterCallback<InputEvent>(e => OnGeneratorChanged(generators[generatorField.index]));
#endif
            root.Q<VisualElement>("Input:generator").Add(generatorField);
            OnGeneratorChanged(generator);

            root.Q<Button>("generateButton").clickable.clicked += () => {
                var objects = generator.Generate(animatorController, animatorController.layers[layerIndex].stateMachine);
                var name = $"{nameof(LayerGenerator)}_{System.DateTime.Now.ToString("o").Replace(':', '-')}";
                ExAssetUtility.PackAssets(objects, $"{savePath}/{name}.asset");
            };
        }

        void OnDisable()
        {
            foreach (var g in generators) g.Save();

            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(nameof(LayerGenerator), data);
        }
    }
}
