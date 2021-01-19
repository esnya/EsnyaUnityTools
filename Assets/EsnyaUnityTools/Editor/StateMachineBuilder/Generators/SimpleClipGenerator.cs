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
    public class SimpleClipGenerator : EditorWindow
    {
        public AnimationClip clip;
        public string typeText;
        public string relativePath;
        public string propertyName;
        public float value;

        void OnEnable()
        {
            var root = this.GetRootVisualContainer();
            root.style.paddingTop = Constants.spacing;
            root.style.paddingLeft = Constants.spacing;
            root.style.paddingRight = Constants.spacing;
            root.style.paddingBottom = Constants.spacing;

            root.Add(new FieldLabel("Relative Path"));
            var relativePathField = new TextField();
            root.Add(relativePathField);
            relativePathField.value = relativePath;
            relativePathField.OnValueChanged(e => relativePath = e.newValue);

            root.Add(new FieldLabel("Type"));
            var typeField = new TextField();
            root.Add(typeField);
            typeField.value = typeText;
            typeField.OnValueChanged(e => typeText = e.newValue);

            root.Add(new FieldLabel("Property Name"));
            var propertyNameField = new TextField();
            root.Add(propertyNameField);
            propertyNameField.value = propertyName;
            propertyNameField.OnValueChanged(e => propertyName = e.newValue);
            
            var generateButton = new Button(Generate);
            root.Add(generateButton);
            generateButton.style.marginTop = Constants.spacing;
            generateButton.text = "Generate";
        }

        void Generate()
        {
            if (!clip)
            {
                clip = new AnimationClip() {
                    name = $"{relativePath}.{propertyName}={value}",
                };
            }  

            var type = System.Type.GetType(typeText);

            if (type == null)
            {
                EditorUtility.DisplayDialog("Error", $"Could not find type: {typeText}", "Cancel");
                return;
            }

            StateMachineBuilderUtility.AddSimpleCurve(clip, relativePath, type, propertyName, value);

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(clip))) {
                var path = EditorUtility.SaveFilePanelInProject("Save Animation Clip", "Animation.clip", "clip", "Save Animation Clip");
                Debug.Log(path);
            }
        }
    }
}
