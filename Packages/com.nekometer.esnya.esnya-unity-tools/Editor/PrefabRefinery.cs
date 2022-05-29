using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace EsnyaFactory
{
    public class PrefabRefinery : EditorWindow
    {
        [MenuItem("EsnyaTools/Prefab Refinery")]
        private static void ShowWindow()
        {
            GetWindow<PrefabRefinery>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Prefab Refinery");

            Resources.Load<VisualTreeAsset>("PrefabRefinery").CloneTree(rootVisualElement);
            rootVisualElement.Q<Button>("revert-all").clicked += () => {
                Refresh(true);
                AssetDatabase.Refresh();
                Refresh();
            };

            Refresh();
        }

        private static UnityEngine.Object GetTargetObject(GameObject prefabInstanceRoot, PropertyModification mod)
        {
            if (mod.target == null) return null;

            if (mod.target is GameObject)
            {
                return prefabInstanceRoot.GetComponentsInChildren<Transform>(true)
                    .Select(c => c.gameObject)
                    .FirstOrDefault(o => PrefabUtility.GetCorrespondingObjectFromSource(o)?.GetInstanceID() == mod.target.GetInstanceID());
            }
            return prefabInstanceRoot.GetComponentsInChildren(mod.target.GetType(), true)
                .FirstOrDefault(c => PrefabUtility.GetCorrespondingObjectFromSource(c)?.GetInstanceID() == mod.target.GetInstanceID());
        }

        private static FieldInfo GetModificationField(UnityEngine.Object targetObject, string propertyPath)
        {
            return targetObject?.GetType()?.GetField(propertyPath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static bool PropertyEquals(UnityEngine.Object targetObject, PropertyModification mod)
        {
            if (targetObject == null) return false;

            var field = GetModificationField(targetObject, mod.propertyPath);
            if (field == null) return false; // ToDo: array

            return field.GetValue(mod.target) == field.GetValue(targetObject);
        }

        private static void SetObjectField(ObjectField field, UnityEngine.Object value)
        {
            if (field == null) return;
            field.objectType = value?.GetType() ?? typeof(UnityEngine.Object);
            field.value = value;
        }

        private void OnSelectionChange() => Refresh();

        private void Refresh(bool revertAll = false)
        {
            var modificationList = rootVisualElement.Q<VisualElement>("modifications");
            var modificationTemplate = Resources.Load<VisualTreeAsset>("PrefabRefineryModification");

            modificationList.Clear();

            var modifications = Selection.gameObjects
                .Where(PrefabUtility.IsPartOfAnyPrefab)
                .Where(PrefabUtility.IsOutermostPrefabInstanceRoot)
                .Select(PrefabUtility.GetNearestPrefabInstanceRoot)
                .Distinct()
                .SelectMany(prefabInstanceRoot =>
                    (PrefabUtility.GetPropertyModifications(prefabInstanceRoot) ?? Enumerable.Empty<PropertyModification>())
                        .Select(modification => (modification, targetObject: GetTargetObject(prefabInstanceRoot, modification)))
                        .Where(t => !PrefabUtility.IsDefaultOverride(t.modification) && PropertyEquals(t.targetObject, t.modification))
                        .Select(t => (prefabInstanceRoot, t.modification, t.targetObject))
                );
            foreach (var (prefabInstanceRoot, modification, targetObject) in modifications)
            {
                var item = modificationTemplate.CloneTree()[0];
                SetObjectField(item.Q<ObjectField>("prefab-instance-root"), prefabInstanceRoot);
                SetObjectField(item.Q<ObjectField>("target-object"), targetObject);
                SetObjectField(item.Q<ObjectField>("target"), modification.target);
                item.Q<TextField>("property-path").value = modification.propertyPath;

                var field = GetModificationField(targetObject, modification.propertyPath);
                if (field?.FieldType?.IsSubclassOf(typeof(UnityEngine.Object)) ?? false)
                {
                    SetObjectField(item.Q<ObjectField>("prefab-object-reference"), field.GetValue(modification.target) as UnityEngine.Object);
                    SetObjectField(item.Q<ObjectField>("object-reference"), field.GetValue(targetObject) as UnityEngine.Object);
                    item.Remove(item.Q<TextField>("value"));
                    item.Remove(item.Q<TextField>("prefab-value"));
                }
                else
                {
                    item.Q<TextField>("prefab-value").value = field.GetValue(modification.target)?.ToString() ?? "null";
                    item.Q<TextField>("value").value = field.GetValue(targetObject)?.ToString() ?? "null";
                    item.Remove(item.Q<ObjectField>("object-reference"));
                    item.Remove(item.Q<ObjectField>("prefab-object-reference"));
                }

                void Revert()
                {
                    PrefabUtility.RevertPropertyOverride(new SerializedObject(targetObject).FindProperty(modification.propertyPath), InteractionMode.UserAction);
                    EditorUtility.SetDirty(targetObject);
                }
                if (revertAll) Revert();
                else
                {
                    item.Q<Button>("revert").clicked += () =>
                    {
                        Revert();
                        AssetDatabase.Refresh();
                        Refresh();
                    };
                }
                modificationList.Add(item);
            }
        }
    }
}
