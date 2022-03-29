using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public class GISTool : EditorWindow
    {

        [MenuItem("EsnyaTools/GIS Tool")]
        private static void ShowWindow()
        {
            var window = GetWindow<GISTool>();
            window.Show();
        }

        private static readonly GUILayoutOption[] miniLabelLayout = {
            GUILayout.ExpandWidth(false),
            GUILayout.Width(30),
        };
        private float DMSField(string label, float value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, miniLabelLayout);
                var deg = EditorGUILayout.FloatField(Mathf.Floor(value));
                EditorGUILayout.LabelField("'", miniLabelLayout);
                var min = EditorGUILayout.FloatField(Mathf.Floor(value * 60) % 60);
                EditorGUILayout.LabelField("\"", miniLabelLayout);
                var sec = EditorGUILayout.FloatField(value * 3600 % 60);
                return deg + min / 60 + sec / 3600;
            }
        }

        public Transform originTransform;
        public Vector2 originLonLat;
        public bool originDMS;
        public Vector2 lonLat;
        public bool dms;

        private void OnEnable()
        {
            titleContent = new GUIContent("GIS Tool");
        }

        private const float EquatorialRadius = 6378137f;
        private const float PolarRadius = 6356752.3142f;
        private const float F = 1/298.257223563f;

        private Vector2 Deg2Meter(Vector2 originPosition, Vector2 originAngle, Vector2 angle)
        {
            var dy = (angle.y - originAngle.y) / 360.0f * 2 * Mathf.PI * PolarRadius;
            var r = Mathf.Cos(Mathf.Deg2Rad * (originAngle.y + angle.y) / 2);
            var dx = (angle.x - originAngle.x) / 360.0f * 2 * Mathf.PI * r * PolarRadius;

            return originPosition + new Vector2(dx, dy);
        }

        private void OnGUI()
        {
            originTransform = EditorGUILayout.ObjectField("Origin", originTransform, typeof(Transform), true) as Transform;
            if (originDMS)
            {
                originLonLat.y = DMSField("Lat", originLonLat.y);
                originLonLat.x = DMSField("Lon", originLonLat.x);
            }
            else
            {
                originLonLat.y = EditorGUILayout.FloatField("Lat", originLonLat.y);
                originLonLat.x = EditorGUILayout.FloatField("Lon", originLonLat.x);
            }
            originDMS = EditorGUILayout.Toggle("DMS", originDMS);

            if (!Selection.activeTransform)
            {
                EditorGUILayout.HelpBox("Select Target GameObject", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();

            if (dms)
            {
                lonLat.y = DMSField("Lat", lonLat.y);
                lonLat.x = DMSField("Lon", lonLat.x);
            }
            else
            {
                lonLat.y = EditorGUILayout.FloatField("Lat", lonLat.y);
                lonLat.x = EditorGUILayout.FloatField("Lon", lonLat.x);
            }
            dms = EditorGUILayout.Toggle("DMS", dms);

            if (GUILayout.Button("Appy"))
            {
                var xz = Deg2Meter(new Vector2(originTransform.position.x, originTransform.position.z), originLonLat, lonLat);
                Undo.RecordObject(Selection.activeTransform, "Apply GIS Position");
                Selection.activeTransform.position = new Vector3(xz.x, Selection.activeTransform.position.y, xz.y);
            }
        }
    }
}
