﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


namespace EsnyaFactory
{
    public class StaticProfile : MonoBehaviour
    {
        public enum ColliderModification
        {
            None,
            EnableConvex,
            DisableConvex,
            ReplaceMeshToBox,
            EnableCollider,
            DisableCollider,
            Remove,
        }

        [Multiline] public string includePattern = ".*";
        [Multiline] public string excludePattern = @"Text \(TMP\)";

        [Header("Static Flags")]
        public bool overrideStaticFlags = true;
#if UNITY_EDITOR
        public StaticEditorFlags staticFlags = (StaticEditorFlags)0xFFFFFF;
#endif
        [Header("Lightmap")]
        public bool overrideLightmapSettings = true;
        public float lightmapScaleOffset = 1.0f;
#if UNITY_EDITOR
        public LightmapParameters lightmapParameters;
#endif
#if UNITY_2019
        public ReceiveGI receiveGI = ReceiveGI.Lightmaps;
#endif


        [Header("Shadow")]
        public bool overrideShadowSettings;
        public ShadowCastingMode shadowCastingMode = ShadowCastingMode.TwoSided;
        public bool receiveShadow = true;

        [Header("Collider")]
        public bool overrideColliders = true;
        public ColliderModification colliderModification;

        private void Reset()
        {
            gameObject.tag = "EditorOnly";
        }

#if UNITY_EDITOR
        public void Apply()
        {
            gameObject.tag = "EditorOnly";

            var includeRegex = new Regex(includePattern);
            var excludeRegex = new Regex(excludePattern);
            var origin = transform.parent;
            foreach (var o in origin.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).Where(o => includeRegex.IsMatch(o.name) && !excludeRegex.IsMatch(o.name)))
            {
                if (overrideStaticFlags) GameObjectUtility.SetStaticEditorFlags(o, o == gameObject ? 0 : staticFlags);

                if (overrideLightmapSettings)
                {
                    var renderers = o.GetComponents<Renderer>();
                    if (renderers.Length > 0)
                    {
                        var serializedObject = new SerializedObject(renderers);
                        serializedObject.FindProperty("m_ScaleInLightmap").floatValue = lightmapScaleOffset;
                        serializedObject.FindProperty("m_LightmapParameters").objectReferenceValue = lightmapParameters;
#if UNITY_2019
                        serializedObject.FindProperty("m_ReceiveGI").intValue = (int)receiveGI;
#endif
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                if (overrideShadowSettings)
                {
                    foreach (var renderer in o.GetComponents<Renderer>())
                    {
                        Undo.RecordObject(renderer, "Override Shadow Settings");
                        renderer.shadowCastingMode = shadowCastingMode;
                        renderer.receiveShadows = receiveShadow;
                    }
                }

                if (overrideColliders)
                {
                    switch (colliderModification)
                    {
                        case ColliderModification.DisableConvex:
                        case ColliderModification.EnableConvex:
                            foreach (var c in o.GetComponents<MeshCollider>())
                            {
                                Undo.RecordObject(c, "Override Colliders");
                                c.convex = colliderModification == ColliderModification.EnableConvex;
                            }
                            break;
                        case ColliderModification.ReplaceMeshToBox:
                            foreach (var c in o.GetComponents<MeshCollider>())
                            {
                                var bounds = c.bounds;
                                Undo.DestroyObjectImmediate(c);

                                var box = o.AddComponent<BoxCollider>();

                                box.center = o.transform.InverseTransformPoint(bounds.center);

                                var size = bounds.size;
                                for (int i = 0; i < 3; i++) size[i] /= o.transform.lossyScale[i];
                                box.size = Quaternion.Inverse(o.transform.rotation) * size;

                                Undo.RegisterCreatedObjectUndo(box, "Replace Mesh To Box");
                            }
                            break;
                        case ColliderModification.Remove:
                            foreach (var c in o.GetComponents<Collider>()) Undo.DestroyObjectImmediate(c);
                            break;
                        case ColliderModification.EnableCollider:
                        case ColliderModification.DisableCollider:
                            foreach (var c in o.GetComponents<Collider>())
                            {
                                Undo.RecordObject(c, "Override Colliders");
                                c.enabled = colliderModification == ColliderModification.EnableCollider;
                            }
                            break;
                    }
                }
            }
        }
#endif
    }


#if UNITY_EDITOR
    [
        CustomEditor(typeof(StaticProfile)),
        CanEditMultipleObjects,
    ]
    public class StaticProfileEditor : Editor
    {

        private static bool ToggleButton(string label, bool state, GUIStyle baseStyle = null)
        {
            var style = state ? new GUIStyle(baseStyle ?? EditorStyles.miniButton) : baseStyle ?? EditorStyles.miniButton;
            if (state) {
                style.normal.background = style.active.background;
                style.fontStyle = FontStyle.BoldAndItalic;
            }
            return GUILayout.Button(label, style);
        }

        private static int EnumMaskToggleButtons<E>(string label, int value) where E : System.Enum
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, new [] { GUILayout.ExpandWidth(false), GUILayout.Width(EditorGUIUtility.labelWidth) });
                return EnumMaskToggleButtons<E>(value);
            }
        }
        private static int EnumMaskToggleButtons<E>(int value) where E : System.Enum
        {
            var optionValues = typeof(E).GetEnumValues();
            var optionNames = typeof(E).GetEnumNames();
            var optionCount = optionValues.Length;

            var everything = (1 << optionCount) - 1;


            using (new EditorGUILayout.VerticalScope())
            {
                for (int i = 0; i < optionValues.Length; i++)
                {
                    var optionValue = (int)optionValues.GetValue(i);
                    var optionName = (string)optionNames.GetValue(i);
                    var state = (value & optionValue) != 0;

                    if (ToggleButton(optionName, state, EditorStyles.miniButton))
                    {
                        return state ? value & ~optionValue : value | optionValue;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (ToggleButton("Nothing", value == 0, EditorStyles.miniButtonLeft)) return 0;
                    if (ToggleButton("Everything", value == everything, EditorStyles.miniButtonRight)) return everything;
                }
            }

            return value;
        }

        private static bool IsHeader(SerializedProperty property)
        {
            var type = property.serializedObject.targetObject.GetType();
            var prop = type?.GetField(property.name);
            return prop?.GetCustomAttributes(true).Any(a => a is HeaderAttribute) ?? false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            EditorGUILayout.PropertyField(property, false);

            var visible = true;
            while (property.NextVisible(false))
            {
                if (IsHeader(property))
                {
                    EditorGUILayout.PropertyField(property, true);
                    visible = property.propertyType != SerializedPropertyType.Boolean || property.boolValue;
                }
                else if (visible)
                {
                    if (property.name == nameof(StaticProfile.staticFlags))
                    {
                        property.intValue = EnumMaskToggleButtons<StaticEditorFlags>(property.displayName, property.intValue);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(property, true);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (GUILayout.Button("Appy Now"))
            {
                foreach (var p in targets.Select(t => t as StaticProfile).Where(p => p != null)) p.Apply();
            }
        }

        [InitializeOnLoadMethod]
        private static void RegisterCallback()
        {
            EditorSceneManager.sceneSaving += (_, __) => ApplyAll();
        }

        private static void ApplyAll()
        {
            foreach (var p in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<StaticProfile>()))
            {
                p.Apply();
            }
        }
    }
#endif

}