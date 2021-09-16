#if UDON
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Codice.Client.Common;
using Ludiq.OdinSerializer.Utilities;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using VRC.Udon;

namespace EsnyaFactory
{
    public class UdonDiff : EditorWindow
    {

        [MenuItem("EsnyaTools/UdonDiff")]
        private static void ShowWindow()
        {
            var window = GetWindow<UdonDiff>();
            window.Show();
        }

        private static IEnumerable<SerializedProperty> EnumerateProperties(SerializedObject serializedObject)
        {
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                yield return property;
            }
        }

        private bool IsFieldsValid
        {
            set => DiffButton.SetEnabled(value);
            get => DiffButton.enabledSelf;
        }
        private bool IsPrefab
        {
            set => PrefabApplyToggle.SetEnabled(value);
            get => PrefabApplyToggle.enabledSelf;
        }
        private ObjectField BaseField => rootVisualElement.Q<ObjectField>("base-field");
        private ObjectField TargetField => rootVisualElement.Q<ObjectField>("target-field");
        private ListView DiffListView => rootVisualElement.Q<ListView>("diff-list-view");
        private Toggle PrefabApplyToggle => rootVisualElement.Q<Toggle>("prefab-apply-button");
        private Button DiffButton => rootVisualElement.Q<Button>("diff-button");
        private VisualElement DiffList => rootVisualElement.Q<VisualElement>("diff-list");

        private void OnEnable()
        {
            titleContent = new GUIContent(nameof(UdonDiff));

            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("UIElements/Common"));

            var ui = Resources.Load<VisualTreeAsset>("UIElements/UdonDiff/EditorWindow").CloneTree();
            rootVisualElement.Add(ui);

            var reloadButton = ui.Q<Button>("reload-button");
            if (reloadButton != null)
            {
                reloadButton.clicked += () =>
                {
                    rootVisualElement.Clear();
                    OnEnable();
                };
            }

            BaseField.objectType = typeof(UdonBehaviour);
            TargetField.objectType = typeof(UdonBehaviour);

            BaseField.RegisterValueChangedCallback(e => OnFieldChanged());
            TargetField.RegisterValueChangedCallback(e => OnFieldChanged());
            OnFieldChanged();

            ui.Q<Button>("diff-button").clicked += () => Diff();
        }

        private void OnFieldChanged()
        {
            DiffList.Clear();
            IsFieldsValid = BaseField.value != null && TargetField.value != null;
            // IsPrefab = IsFieldsValid && PrefabUtility.IsPartOfPrefabInstance(TargetField.value);
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null) return "/";
            return $"{GetPath(transform.parent)}/{transform.gameObject.name}";
        }

        private static string GetRelativePath(Transform relativeTo, Transform transform)
        {
            var relativeToPath = GetPath(relativeTo);
            var transformPath = GetPath(transform);

            return transformPath?.StartsWith(relativeToPath) ?? false
                ? transformPath.Substring(relativeToPath.Length + 1)
                : transformPath;
        }

        private static string GetComponentPath(Component component)
        {
            return $"{GetPath(component.transform)}";
        }

        private static Transform FindFromPath(Transform source, string path)
        {
            return source.Find(path);
        }

        private static bool IsDiff(FieldInfo field, UdonSharpBehaviour baseProxy, UdonSharpBehaviour targetProxy)
        {
            var targetValue = field.GetValue(targetProxy);
            var baseValue = field.GetValue(baseProxy);


            if (Equals(targetValue, baseValue) || targetValue == baseValue) return false;

            var targetPath = GetRelativePath(targetProxy.transform, (targetValue as GameObject)?.transform ?? (targetValue as Component)?.transform);
            var basePath = GetRelativePath(baseProxy.transform, (baseValue as GameObject)?.transform ?? (baseValue as Component)?.transform);
            Debug.Log($"{basePath} → {targetPath}");

            if (field.FieldType == typeof(GameObject) || field.FieldType.InheritsFrom<Component>() && targetPath != null && targetPath == basePath) return false;

            return true;
        }

        private static object GetRevertedValue(FieldInfo field, UdonSharpBehaviour baseProxy, UdonSharpBehaviour targetProxy)
        {
            var baseValue = field.GetValue(baseProxy);
            if (field.FieldType != typeof(GameObject) && !field.FieldType.InheritsFrom<Component>()) return baseValue;

            var path = GetRelativePath(baseProxy.transform, (baseValue as GameObject)?.transform ?? (baseValue as Transform)?.transform);
            var targetTransform = targetProxy.transform.Find(path);
            return (field.FieldType == typeof(GameObject) ? targetTransform?.transform : targetTransform?.GetComponent(baseValue.GetType())) ?? baseValue;
        }

        private void Diff()
        {
            DiffList.Clear();

            var target = TargetField.value as UdonBehaviour;

            var baseProxy = UdonSharpEditorUtility.GetProxyBehaviour(BaseField.value as UdonBehaviour);
            var targetProxy = UdonSharpEditorUtility.GetProxyBehaviour(target);

            var baseSerialized = new SerializedObject(baseProxy);
            var targetSerialized = new SerializedObject(targetProxy);

            var diffItemTemplate = Resources.Load<VisualTreeAsset>("UIElements/UdonDiff/DiffItem");

            foreach (var property in EnumerateProperties(targetSerialized))
            {
                var field = targetProxy.GetType().GetField(property.name);
                Debug.Log($"{field.Name}: {field.GetValue(targetProxy)}, {field.GetValue(baseProxy)}, {Equals(field.GetValue(targetProxy), field.GetValue(baseProxy))}");

                if (!IsDiff(field, baseProxy, targetProxy)) continue;

                var diffItem = diffItemTemplate.CloneTree();
                DiffList.Add(diffItem);

                diffItem.Q<PropertyField>("base-value").BindProperty(baseSerialized.FindProperty(property.propertyPath));
                diffItem.Q<PropertyField>("target-value").BindProperty(property);

                diffItem.Q<Button>("revert-button").clicked += () =>
                {
                    targetProxy.UpdateProxy();

                    field.SetValue(targetProxy, GetRevertedValue(field, baseProxy, targetProxy));

                    targetProxy.ApplyProxyModifications();
                    EditorUtility.SetDirty(TargetField.value);

                    // if (IsPrefab && PrefabApplyToggle.value)
                    // {
                    //     targetSerialized.Update();
                    //     PrefabUtility.ApplyPropertyOverride(
                    //         targetSerialized.FindProperty(property.propertyPath),
                    //         PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target),
                    //         InteractionMode.UserAction
                    //     );
                    // }

                    DiffList.Remove(diffItem);
                };
            };
        }
    }
}
#endif
