using System;
using System.Collections.Generic;
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

        private static readonly Dictionary<int, SerializedObject> serializedObjects = new Dictionary<int, SerializedObject>();
        private static SerializedObject GetSerializedObject(UnityEngine.Object o)
        {
            if (!o) return null;

            var id = o.GetInstanceID();
            if (serializedObjects.ContainsKey(id))
            {
                var serializedObject = serializedObjects[id];
                // Debug.Log($"Chache Hit: {id} ({o})@{AssetDatabase.GetAssetPath(o)} = ({serializedObjects[id].modifiedObject})@{AssetDatabase.GetAssetPath(serializedObjects[id].modifiedObject)}");
                serializedObject.Update();
                return serializedObject;
            }
            else
            {
                var serializedObject = new SerializedObject(o);
                serializedObjects[id] = serializedObject;
                return serializedObject;
            }
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Prefab Refinery");

            Resources.Load<VisualTreeAsset>("PrefabRefinery").CloneTree(rootVisualElement);
            rootVisualElement.Q<Button>("revert-all").clicked += () =>
            {
                Scan(Selection.gameObjects, true);
                AssetDatabase.Refresh();
                Scan(Selection.gameObjects);
            };

            rootVisualElement.Q<Button>("scan").clicked += () => Scan(Selection.gameObjects);
        }

        private static UnityEngine.Object GetModifiedObject(GameObject prefabInstanceRoot, PropertyModification mod)
        {
            if (mod.target == null) return null;

            if (mod.target is GameObject)
            {
                return EditorUtility.CollectDeepHierarchy(new[] { mod.target })
                    .Where(o => o is GameObject)
                    .Select(o => o as GameObject)
                    .FirstOrDefault(o => PrefabUtility.GetCorrespondingObjectFromSource(o)?.GetInstanceID() == mod.target.GetInstanceID());
            }
            return prefabInstanceRoot.GetComponentsInChildren(mod.target.GetType(), true)
                .FirstOrDefault(c => PrefabUtility.GetCorrespondingObjectFromSource(c)?.GetInstanceID() == mod.target.GetInstanceID());
        }

        private static bool PropertyEquals(UnityEngine.Object a, UnityEngine.Object b, string propertyPath)
        {
            if (!a || !b) return false;
            var propertyA = GetSerializedObject(a).FindProperty(propertyPath);
            var propertyB = GetSerializedObject(b).FindProperty(propertyPath);
            if ((propertyA ?? propertyB) == null) return false;
            return GetPropertyValue(propertyA)?.Equals(GetPropertyValue(propertyB)) ?? false;
        }

        private static void SetObjectField(ObjectField field, UnityEngine.Object value)
        {
            if (field == null) return;
            field.objectType = value?.GetType() ?? typeof(UnityEngine.Object);
            field.value = value;
        }

        private static object GetPropertyValue(SerializedProperty property)
        {
            switch (property?.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Color:
                    return property.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return property.intValue;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex;
                case SerializedPropertyType.Vector2:
                    return property.vector2Value;
                case SerializedPropertyType.Vector3:
                    return property.vector3Value;
                case SerializedPropertyType.Vector4:
                    return property.vector4Value;
                case SerializedPropertyType.Rect:
                    return property.rectValue;
                case SerializedPropertyType.ArraySize:
                    return property.intValue;
                case SerializedPropertyType.Character:
                    return (char)property.intValue;
                case SerializedPropertyType.AnimationCurve:
                    return property.animationCurveValue;
                case SerializedPropertyType.Bounds:
                    return property.boundsValue;
                // case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue;
                case SerializedPropertyType.ExposedReference:
                    return property.exposedReferenceValue;
                case SerializedPropertyType.FixedBufferSize:
                    return property.intValue;
                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue;
                case SerializedPropertyType.RectInt:
                    return property.rectIntValue;
                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue;
                default:
                    return null;
            }
        }

        private static VisualElement ToVisualElement(object value)
        {
            if (value == null) return new Label("null");
            if (value is string @string) return new Label(@string);
            if (value is bool @bool) return new Toggle() { value = @bool };
            if (value is int || value is char || value is UnityEngine.Object[]) return new Label(value.ToString());
            if (value is Vector2 @vector2) return new Vector2Field() { value = @vector2 };
            if (value is Vector3 @vector3) return new Vector3Field() { value = @vector3 };
            if (value is Vector4 @vector4) return new Vector4Field() { value = @vector4 };
            if (value is Rect @rect) return new RectField() { value = @rect };
            if (value is Color @color) return new ColorField() { value = @color };
            if (value is AnimationCurve @animationCurve) return new CurveField() { value = @animationCurve };
            if (value is Bounds @bounds) return new BoundsField() { value = @bounds };
            if (value is Gradient @gradient) return new GradientField() { value = @gradient };
            if (value is Quaternion @quaternion) return new Vector3Field() { value = @quaternion.eulerAngles };
            if (value is LayerMask @layerMask) return new LayerMaskField() { value = @layerMask };
            if (value is Vector2Int @vector2Int) return new Vector2IntField() { value = @vector2Int };
            if (value is Vector3Int @vector3Int) return new Vector3IntField() { value = @vector3Int };
            if (value is RectInt @rectInt) return new RectIntField() { value = @rectInt };
            if (value is BoundsInt @boundsInt) return new BoundsIntField() { value = @boundsInt };
            if (value is UnityEngine.Object @object) return new ObjectField() { value = @object, objectType = @object?.GetType() ?? typeof(UnityEngine.Object) };

            return new VisualElement();
        }

        private static VisualElement ToVisualElement(SerializedProperty property) => ToVisualElement(GetPropertyValue(property));

        private static BaseField<T> Readonly<T>(BaseField<T> field)
        {
            field.RegisterValueChangedCallback(e => e.PreventDefault());
            return field;
        }
        private static VisualElement Readonly(VisualElement field)
        {
            if (field is BaseField<object> @baseField) return Readonly(baseField);
            return field;
        }

        private void Scan(IEnumerable<GameObject> targetObjects, bool revertAll = false)
        {
            var modificationList = rootVisualElement.Q<VisualElement>("modifications");
            var modificationTemplate = Resources.Load<VisualTreeAsset>("PrefabRefineryModification");

            modificationList.Clear();

            var modifications = targetObjects
                .Where(PrefabUtility.IsPartOfAnyPrefab)
                .Where(PrefabUtility.IsOutermostPrefabInstanceRoot)
                .Select(PrefabUtility.GetNearestPrefabInstanceRoot)
                .Distinct()
                .SelectMany(prefabInstanceRoot =>
                    (PrefabUtility.GetPropertyModifications(prefabInstanceRoot) ?? Enumerable.Empty<PropertyModification>())
                        .Where(m => m.target)
                        .Where(m => !PrefabUtility.IsDefaultOverride(m))
                        .Select(m => (sourceObject: m.target, modifiedObject: GetModifiedObject(prefabInstanceRoot, m), m.propertyPath, m.value, m.objectReference))
                        .Where(t => t.modifiedObject && PropertyEquals(t.modifiedObject, t.sourceObject, t.propertyPath))
                        .Select(t => (prefabInstanceRoot, t.sourceObject, t.modifiedObject, t.propertyPath, t.value, t.objectReference))
                );
            foreach (var (prefabInstanceRoot, sourceObject, modifiedObject, propertyPath, value, objectReference) in modifications)
            {
                var item = modificationTemplate.CloneTree()[0];
                SetObjectField(item.Q<ObjectField>("source-prefab-instance-root"), PrefabUtility.GetNearestPrefabInstanceRoot(sourceObject) ?? PrefabUtility.GetOutermostPrefabInstanceRoot(sourceObject));
                SetObjectField(item.Q<ObjectField>("prefab-instance-root"), prefabInstanceRoot);

                SetObjectField(item.Q<ObjectField>("source-object"), sourceObject);
                SetObjectField(item.Q<ObjectField>("modified-object"), modifiedObject);
                item.Q<Label>("property-path").text = propertyPath;

                item.Q<VisualElement>("source-value").Add(Readonly(ToVisualElement(GetSerializedObject(sourceObject).FindProperty(propertyPath))));
                item.Q<VisualElement>("modified-value").Add(Readonly(ToVisualElement((object)objectReference ?? value)));

                void Revert()
                {
                    PrefabUtility.RevertPropertyOverride(GetSerializedObject(modifiedObject).FindProperty(propertyPath), InteractionMode.UserAction);
                    EditorUtility.SetDirty(modifiedObject);
                }
                if (revertAll) Revert();
                else
                {
                    item.Q<Button>("revert").clicked += () =>
                    {
                        Revert();
                        AssetDatabase.Refresh();
                        Scan(targetObjects);
                    };
                }
                modificationList.Add(item);
            }
        }
    }
}
