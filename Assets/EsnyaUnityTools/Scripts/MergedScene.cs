using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace EsnyaFactory
{

    [CreateAssetMenu(menuName = "EsnyaTools/Merged Scene")]
    public class MergedScene : ScriptableObject
    {
#if UNITY_EDITOR
        [Serializable]
        public struct SceneItem
        {
            public SceneAsset sceneAsset;
            public Vector3 offset;
            public Vector3 angleOffset;
            public bool isLightingSource;
        }
        public SceneAsset mergedSceneAsset;
        public SceneItem[] scenes = {};

        private string SavePath => $"{Path.GetDirectoryName(AssetDatabase.GetAssetPath(this))}/{name}.unity";

        public void Update()
        {
            if (mergedSceneAsset == null)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene), SavePath);
            }

            EditorSceneManager.OpenScene(SavePath, OpenSceneMode.Single);
            var mergedScene = SceneManager.GetActiveScene();

            foreach (var o in mergedScene.GetRootGameObjects()) DestroyImmediate(o);

            foreach (var item in scenes.Where(i => i.sceneAsset != null))
            {
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(item.sceneAsset), OpenSceneMode.Additive);
                var scene = SceneManager.GetSceneByName(item.sceneAsset.name);

                if (item.isLightingSource)
                {
                    SceneManager.SetActiveScene(scene);
                    var skybox = RenderSettings.skybox;
                    var sun = RenderSettings.sun;

                    if (sun != null)
                    {
                        var sunRoot = scene.GetRootGameObjects().First(o => o.GetComponentsInChildren<Light>().Contains(sun));
                        SceneManager.MoveGameObjectToScene(sunRoot, mergedScene);
                        sunRoot.transform.position += item.offset;
                        sunRoot.transform.rotation *= Quaternion.Euler(item.angleOffset);
                    }

                    SceneManager.SetActiveScene(mergedScene);
                    RenderSettings.skybox = skybox;
                    RenderSettings.sun = sun;
                }

                foreach (var o in scene.GetRootGameObjects())
                {
                    if (o.CompareTag("EditorOnly")) continue;
                    SceneManager.MoveGameObjectToScene(o, mergedScene);
                    o.transform.position += item.offset;
                    o.transform.rotation *= Quaternion.Euler(item.angleOffset);
                }

                SceneManager.UnloadSceneAsync(scene);
            }

            Debug.Log($"Merged scene saved into {SavePath}");
            EditorSceneManager.SaveScene(mergedScene, SavePath);
        }

        [CustomEditor(typeof(MergedScene))]
        public class MergedSceneEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                target.name = EditorGUILayout.TextField("Name", target.name);

                EditorGUILayout.Space();

                if (GUILayout.Button("Force Update")) (target as MergedScene)?.Update();
            }

            [InitializeOnLoadMethod]
            public void RegisterCallback()
            {
                EditorSceneManager.sceneSaved += (scene) => {
                    foreach (var mergedScene in AssetDatabase.FindAssets($"t:{typeof(MergedScene).Name}").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<MergedScene>).Where(ms => ms.scenes.Any(i => i.sceneAsset.name == scene.name)))
                    {
                        mergedScene.Update();
                    }
                };
            }
        }
#endif
    }
}
