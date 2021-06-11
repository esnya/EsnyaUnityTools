using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace EsnyaFactory
{
    public class EEUI
    {
        public static readonly GUILayoutOption[] minIButtonLayout = {
            GUILayout.ExpandWidth(false),
            // GUILayout.Width(100),
        };

        public static void Scroll(ref Vector2 scrollPosition, System.Action GUIRenderer)
        {
            using (var scope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scope.scrollPosition;
                GUIRenderer();
            }
        }


        public static void BoldLabel(string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        public static void ValueField(string label, ref bool value)
        {
            value = EditorGUILayout.Toggle(label, value);
        }

        public static void ValueField<T>(string label, ref T value) where T : Enum
        {
            value = (T)EditorGUILayout.EnumPopup(label, value);
        }

        public static void ObjectField<T>(string label, ref T value, bool allowSceneObjects) where T : UnityEngine.Object
        {
            value = EditorGUILayout.ObjectField(label, value, typeof(T), allowSceneObjects) as T;
        }

        public static void ObjectFieldWithAction<T>(string label, ref T value, bool allowSceneObjects, string actionLabel, Func<T, T> Action) where T : UnityEngine.Object
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                ObjectField(label, ref value, allowSceneObjects);
                if (GUILayout.Button(actionLabel, EditorStyles.miniButton, minIButtonLayout)) value = Action(value);
            }
        }
        public static void ObjectFieldWithAction<T>(string label, ref T value, bool allowSceneObjects, string actionLabel, Action Action) where T : UnityEngine.Object
        {
            ObjectFieldWithAction(label, ref value, allowSceneObjects, actionLabel, v => { Action(); return v; });
        }

        public static void ObjectFieldWithSelection(string label, ref GameObject value, bool allowSceneObjects)
        {
            ObjectFieldWithAction(label, ref value, allowSceneObjects, "From Selected", (v) => Selection.activeGameObject ?? v);
        }
        public static void ObjectFieldWithSelection<T>(string label, ref T value, bool allowSceneObjects) where T : UnityEngine.Object
        {
            ObjectFieldWithAction(
                label,
                ref value,
                allowSceneObjects,
                "From Selected",
                (v) => (typeof(T).IsSubclassOf(typeof(Component)) ? Selection.activeGameObject?.GetComponent<T>() : Selection.activeObject as T) ?? v
            );
        }

        public static void Button(string label, Action Action)
        {
            if (GUILayout.Button(label)) Action();
        }

        public static void Toolbar<T>(ref T value) where T : Enum
        {
            var type = value.GetType();
            var names = Enum.GetNames(type);
            var values = Enum.GetValues(type) as T[];
            var value_ = value;
            var currentSelected = values.Select((v, i) => (v, i)).FirstOrDefault(t => t.v.Equals(value_)).i;
            var selected = GUILayout.Toolbar(currentSelected, names);
            if (selected != currentSelected) value = values[selected];
        }
    }
}
