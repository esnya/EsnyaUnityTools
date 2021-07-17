using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using VRC.Udon;
using VRC.Udon.Editor.ProgramSources;

namespace EsnyaFactory
{
    public class RepairUdon : EditorWindow
    {
        [MenuItem("EsnyaTools/Rpair Udon")]
        private static void ShowWindow()
        {
            var window = GetWindow<RepairUdon>();
            window.Show();
        }

        private static Dictionary<int, UdonProgramAsset> programCache;
        private static UdonProgramAsset FindUdonSharpBehaviour(AbstractSerializedUdonProgramAsset compiledUdon)
        {
            if (programCache == null) programCache = new Dictionary<int, UdonProgramAsset>();

            var hashCode = compiledUdon.GetHashCode();
            if (programCache.ContainsKey(hashCode)) return programCache[hashCode];

            foreach (var program in AssetDatabase.FindAssets("t:UdonProgramAsset").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<UdonProgramAsset>).Where(a => a != null))
            {
                if (program.SerializedProgramAsset == null) continue;
                programCache[program.SerializedProgramAsset.GetHashCode()] = program;
            }

            if (programCache.ContainsKey(hashCode)) return programCache[hashCode];
            return null;
        }

        private static void Repair(UdonBehaviour brokenUdon)
        {
            var serializedUdon = new SerializedObject(brokenUdon);
            var property = serializedUdon.FindProperty("serializedProgramAsset");
            var compiledUdon = property?.objectReferenceValue as AbstractSerializedUdonProgramAsset;
            if (!compiledUdon)
            {
                Debug.LogError($"Could not find serialized program asset of {brokenUdon.gameObject.name}");
                return;
            }

            var udonProgram = FindUdonSharpBehaviour(compiledUdon);
            if (!udonProgram)
            {
                Debug.LogError($"Could not find udon program asset of {brokenUdon.gameObject.name}");
                return;
            }

            Undo.RecordObject(brokenUdon, "Repair Udon");
            brokenUdon.programSource = udonProgram;

            Debug.Log($"{brokenUdon.gameObject.name} fixed!!");
        }

        private Vector2 scrollPosition;

        private void OnGUI()
        {
            titleContent = new GUIContent("Repair Udon");
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scroll.scrollPosition;

                EditorGUILayout.Space();


                var brokenUdons = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<UdonBehaviour>(true)).Where(u => u.programSource == null);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Broken Udon in Scene");
                    if (GUILayout.Button("Repair All", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        foreach (var brokenUdon in brokenUdons) Repair(brokenUdon);
                    }
                }

                foreach (var brokenUdon in brokenUdons)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(brokenUdon, typeof(UdonBehaviour), true);
                        if (GUILayout.Button("Select", EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(false)))
                        {
                            Selection.activeGameObject = brokenUdon.gameObject;
                            Selection.activeObject = brokenUdon;
                        }
                        if (GUILayout.Button("Repair", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false)))
                        {
                            Repair(brokenUdon);
                        }
                    }
                }
            }
        }
    }
}
