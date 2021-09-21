using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;

namespace EsnyaFactory
{
    [CreateAssetMenu(menuName = "EsnyaTools/PrefabUdonVariables")]
    public class PrefabUdonVariables : ScriptableObject
    {
        [Serializable]
        public struct UdonVariable
        {
            public string symbolName;
            public UnityEngine.Object objectReference;
            public string value;
        }


        [Serializable]
        public struct UdonVariables
        {
            public UdonBehaviour udonInstance;
            public string gameObjectName;
            public UdonVariable[] variables;
        }

        [Serializable]
        public struct PrefabVariables
        {
            public GameObject prefabInstance;
            public string prefabPath;
            public UdonVariables[] udonVariables;
        }

        [HideInInspector] public PrefabVariables[] udonVariables;

        public UdonVariables ScanUdon(UdonBehaviour udon)
        {
            return new UdonVariables()
            {
                udonInstance = udon,
                gameObjectName = udon.gameObject.name,
                variables = udon.publicVariables.VariableSymbols.Select(symbolName =>
                {
                    udon.publicVariables.TryGetVariableValue(symbolName, out object value);
                    return new UdonVariable()
                    {
                        symbolName = symbolName,
                        objectReference = value as UnityEngine.Object,
                        value = value?.ToString() ?? "null",
                    };
                }).OrderBy(v => v.symbolName).ToArray(),
            };
        }

        public PrefabVariables ScanPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return new PrefabVariables()
            {
                prefabPath = path,
                prefabInstance = prefab,
                udonVariables = prefab.GetComponentsInChildren<UdonBehaviour>()
                    .Select(ScanUdon)
                    .Where(uv => uv.variables.Length > 0)
                    .OrderBy(uv => uv.gameObjectName).ToArray(),
            };
        }
        public void Scan()
        {
            udonVariables = AssetDatabase.FindAssets("t:GameObject", new[] { Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)) })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(ScanPrefab)
                .Where(pv => pv.udonVariables.Length > 0)
                .OrderBy(pv => pv.prefabPath)
                .ToArray();
            EditorUtility.SetDirty(this);
        }

        private void Reset() => Scan();
        private void OnValidate() => Scan();
    }

    public class PrefabUdonVariablesScanner : AssetPostprocessor
    {
        private void OnPostprocessPrefab(GameObject root)
        {
            var path = AssetDatabase.GetAssetPath(root);
            UnityEngine.Object.FindObjectsOfType<PrefabUdonVariables>()
                .Where(puv => path.StartsWith(Path.GetDirectoryName(AssetDatabase.GetAssetPath(puv))))
                .ToList()
                .ForEach(puv => puv.Scan());
        }

        [InitializeOnLoadMethod]
        private static void RegisterCallbacks()
        {
            EditorSceneManager.sceneSaving += (_, __) => UnityEngine.Object.FindObjectsOfType<PrefabUdonVariables>()
                .ToList()
                .ForEach(puv => puv.Scan());
        }
    }

    [CustomEditor(typeof(PrefabUdonVariables))]
    public class PrefabUdonVariablesEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Update Now")) (target as PrefabUdonVariables)?.Scan();
        }
    }
}
