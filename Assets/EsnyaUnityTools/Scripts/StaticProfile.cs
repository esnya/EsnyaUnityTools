using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


namespace EsnyaFactory
{
    [DisallowMultipleComponent]
    public class StaticProfile : MonoBehaviour
    {
#if UNITY_EDITOR
        public StaticEditorFlags staticFlags = (StaticEditorFlags)0xFFFFFF;
        [Multiline] public string staticExcludePattern = "^$";

        public float lightmapScaleOffset = 1.0f;
        public LightmapParameters lightmapParameters;

        public bool convexMeshCollider;
        [Multiline] public string convexInvertPattern = "^$";

        public void Apply()
        {
            gameObject.tag = "EditorOnly";

            var staticExcludeRegex = new Regex(staticExcludePattern);
            var origin = transform.parent ?? transform;
            foreach (var o in origin.GetComponentsInChildren<Transform>().Select(t => t.gameObject).Where(o => !staticExcludeRegex.IsMatch(o.name)))
            {
                GameObjectUtility.SetStaticEditorFlags(o, o == gameObject ? 0 : staticFlags);
            }

            var serializedObject = new SerializedObject(origin.GetComponentsInChildren<Renderer>());
            serializedObject.FindProperty("m_ScaleInLightmap").floatValue = lightmapScaleOffset;
            serializedObject.FindProperty("m_LightmapParameters").objectReferenceValue = lightmapParameters;
            serializedObject.ApplyModifiedProperties();

            var convexInvertRegex = new Regex(convexInvertPattern);
            foreach (var collider in origin.GetComponentsInChildren<MeshCollider>()) collider.convex = convexMeshCollider ^ convexInvertRegex.IsMatch(collider.gameObject.name);
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
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                if (property.name == nameof(StaticProfile.staticFlags))
                {
                    property.intValue = (int)(StaticEditorFlags)EditorGUILayout.EnumFlagsField(property.displayName, (StaticEditorFlags)property.intValue);
                }
                else
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Appy"))
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
