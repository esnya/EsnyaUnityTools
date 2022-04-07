using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EsnyaFactory
{
    public class TransformTool : EditorWindow
    {
        [MenuItem("EsnyaTools/Transform Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<TransformTool>();
            window.Show();

        }

        [Header("Snap")]
        public SerializedObject serializedObject;
        public float maxDistance = 1.0f;
        public LayerMask layerMask = 0x801;
        public QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;


        [Header("Align")]
        public Vector3 step = Vector3.right;
        // public Vector3 offset = Vector3.zero;
        public int duplicateCount = 2;

        [Header("Geography")]
        public Vector2 originLatLon;
        public Vector2 latLon;
        private const float semiMajorRadius = 6378137;
        private const float flattening = 1.0f / 298.257223563f;

        private void OnEnable()
        {
            titleContent = new GUIContent("Transform Tool");
            serializedObject = new SerializedObject(this);
        }

        private static Vector2 ParseLatLon(string text)
        {
            if (text.Contains('°'))
            {
                var pattern = new Regex("(?<latdir>[NS])(?<lat1>[0-9]+)°(?<lat60>[0-9]+)\\.(?<lat3600>[0-9]+)'[,/ ]+(?<londir>[WE])(?<lon1>[0-9]+)°(?<lon60>[0-9]+)\\.(?<lon3600>[0-9]+)'");
                var match = pattern.Match(text);
                var lat = (match.Groups["latdir"].Value == "S" ? -1 : 1) * (float.Parse(match.Groups["lat1"].Value) + float.Parse(match.Groups["lat60"].Value) / 60 + float.Parse(match.Groups["lat3600"].Value) / 3600);
                var lon = (match.Groups["londir"].Value == "W" ? -1 : 1) * (float.Parse(match.Groups["lon1"].Value) + float.Parse(match.Groups["lon60"].Value) / 60 + float.Parse(match.Groups["lon3600"].Value) / 3600);
                return new Vector2(lat, lon);
            }
            var split = text.Split(',');
            return new Vector2(float.Parse(split[0]), float.Parse(split[1]));
        }

        private void OnGUI()
        {
            serializedObject.Update();

            var serializedProperty = serializedObject.GetIterator();
            serializedProperty.NextVisible(true);

            while (serializedProperty.NextVisible(false))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(serializedProperty, true);
                    switch (serializedProperty.name)
                    {
                        case nameof(originLatLon):
                        case nameof(latLon):
                            using (new EditorGUILayout.VerticalScope())
                            {
                                GUILayout.FlexibleSpace();
                                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                                {
                                    if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                                    {
                                        var value = serializedProperty.vector2Value;
                                        EditorGUIUtility.systemCopyBuffer = $"{value.x},{value.y}";
                                    }
                                    if (GUILayout.Button("Paste", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                                    {
                                        serializedProperty.vector2Value = ParseLatLon(EditorGUIUtility.systemCopyBuffer);
                                    }
                                }
                                GUILayout.FlexibleSpace();
                            }
                            break;
                    }
                }

                switch (serializedProperty.name)
                {
                    case nameof(queryTriggerInteraction):
                        if (GUILayout.Button("Snap To Ground"))
                        {
                            var transforms = Selection.transforms;
                            foreach (var transform in transforms)
                            {
                                Undo.RecordObject(transform, "Snap To Ground");
                                var hitUp = Physics.Raycast(transform.position + Vector3.up * maxDistance, Vector3.down, out var up, maxDistance, layerMask, queryTriggerInteraction);
                                if (hitUp) transform.position = up.point;
                                else
                                {
                                    var hitDown = Physics.Raycast(transform.position, Vector3.down, out var down, maxDistance, layerMask, queryTriggerInteraction);
                                    if (hitDown) transform.position = down.point;
                                }
                            }
                        }
                        EditorGUILayout.Separator();
                        break;
                    case nameof(duplicateCount):
                        if (GUILayout.Button("Duplicate Aligned"))
                        {
                            for (var i = 1; i < duplicateCount; i++)
                            {
                                foreach (var gameObject in Selection.gameObjects.OrderBy(o => o.transform.GetSiblingIndex()))
                                {
                                    var duplicated = PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) ? ClonePrefab(gameObject) : Instantiate(gameObject);
                                    duplicated.name = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(gameObject.transform.parent, gameObject.name);
                                    duplicated.transform.parent = gameObject.transform.parent;
                                    duplicated.transform.localPosition = gameObject.transform.localPosition + step * i;
                                    duplicated.transform.localRotation = gameObject.transform.localRotation;
                                    duplicated.transform.localScale = gameObject.transform.localScale;
                                    Undo.RegisterCreatedObjectUndo(duplicated, "Duplicate Aligned");
                                }
                            }
                        }
                        EditorGUILayout.Separator();
                        break;
                    case nameof(latLon):
                        if (GUILayout.Button("Move To"))
                        {
                            var activeTransform = Selection.activeTransform;
                            if (activeTransform)
                            {
                                var z = VincentyInverse(originLatLon, new Vector2(latLon.x, originLatLon.y)) * Mathf.Sign(latLon.x - originLatLon.x);
                                var x = VincentyInverse(originLatLon, new Vector2(originLatLon.x, latLon.y)) * Mathf.Sign(latLon.y - originLatLon.y);
                                activeTransform.position = new Vector3(x, activeTransform.position.y, z);
                            }
                        }
                        EditorGUILayout.Separator();
                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private float VincentyInverse(Vector2 latLon1, Vector2 latLon2, int iterations = 1000)
        {
            if (Mathf.Approximately(Vector3.Distance(latLon1, latLon2), 0)) return 0;

            var a = semiMajorRadius;
            var f = flattening;
            var b = (1 - f) * a;

            var φ1 = Mathf.Deg2Rad * latLon1.x;
            var φ2 = Mathf.Deg2Rad * latLon2.x;
            var λ1 = Mathf.Deg2Rad * latLon1.y;
            var λ2 = Mathf.Deg2Rad * latLon2.y;

            var U1 = Mathf.Atan((1 - f) * Mathf.Tan(φ1));
            var U2 = Mathf.Atan((1 - f) * Mathf.Tan(φ2));

            var sinU1 = Mathf.Sin(U1);
            var sinU2 = Mathf.Sin(U2);
            var cosU1 = Mathf.Cos(U1);
            var cosU2 = Mathf.Cos(U2);

            var L = λ2 - λ1;

            var λ = L;

            for (var i = 0; i < iterations; i++)
            {
                var sinλ = Mathf.Sin(λ);
                var cosλ = Mathf.Cos(λ);
                var sinσ = Mathf.Sqrt(Mathf.Pow((cosU2 * sinλ), 2) + Mathf.Pow((cosU1 * sinU2 - sinU1 * cosU2 * cosλ), 2));
                var cosσ = sinU1 * sinU2 + cosU1 * cosU2 * cosλ;
                var σ = Mathf.Atan2(sinσ, cosσ);
                var sinα = cosU1 * cosU2 * sinλ / sinσ;
                var cos2α = 1 - Mathf.Pow(sinα, 2);
                var cos2σm = cosσ - 2 * sinU1 * sinU2 / cos2α;
                var C = f / 16 * cos2α * (4 + f * (4 - 3 * cos2α));
                var λʹ = λ;
                λ = L + (1 - C) * f * sinα * (σ + C * sinσ * (cos2σm + C * cosσ * (-1 + Mathf.Pow(2 * cos2σm, 2))));

                if (Mathf.Abs(λ - λʹ) <= 1e-12f)
                {
                    var u2 = cos2α * (Mathf.Pow(a, 2) - Mathf.Pow(b, 2)) / Mathf.Pow(b, 2);
                    var A = 1 + u2 / 16384 * (4096 + u2 * (-768 + u2 * (320 - 175 * u2)));
                    var B = u2 / 1024 * (256 + u2 * (-128 + u2 * (74 - 47 * u2)));
                    var Δσ = B * sinσ * (cos2σm + B / 4 * (cosσ * (-1 + 2 * Mathf.Pow(cos2σm, 2)) - B / 6 * cos2σm * (-3 + 4 * Mathf.Pow(sinσ, 2)) * (-3 + 4 * Mathf.Pow(cos2σm, 2))));
                    var s = b * A * (σ - Δσ);

                    // var α1 = Mathf.Atan2(cosU2 * sinλ, cosU1 * sinU2 - sinU1 * cosU2 * cosλ);
                    // var α2 = Mathf.Atan2(cosU1 * sinλ, -sinU1 * cosU2 + cosU1 * sinU2 * cosλ) + Mathf.PI;

                    // if (α1 < 0)
                    // {
                    //     α1 = α1 + Mathf.PI * 2;
                    // }

                    return s;
                }
            }

            return float.NaN;
        }

        private static GameObject ClonePrefab(GameObject prefabInstance)
        {
            var mods = PrefabUtility.GetPropertyModifications(prefabInstance);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstance));
            var duplicated = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            PrefabUtility.SetPropertyModifications(duplicated, mods);
            return duplicated;
        }
    }
}
