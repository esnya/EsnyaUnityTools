namespace EsnyaFactory {
  using UnityEngine;
  using UnityEditor;

  public class SkinnedMeshBaker : EditorWindow {

    [MenuItem("EsnyaTools/Bake Skinned Mesh")]
    private static void ShowWindow() {
      var window = GetWindow<SkinnedMeshBaker>();
      window.titleContent = new GUIContent("Bake Skinned Mesh");
      window.Show();
    }

    private SkinnedMeshRenderer renderer;

    private void OnGUI() {
      renderer = EditorGUILayout.ObjectField("Skinned Mesh Renderer", renderer, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;

      using (new EditorGUI.DisabledGroupScope(renderer == null)) {
        if (GUILayout.Button("Bake")) {
          var filename = EditorUtility.SaveFilePanel("Save", $"{Application.dataPath}", $"{renderer.sharedMesh.name}_baked.asset", "asset");
          if (filename == null) {
            Debug.Log("Canceled");
            return;
          }

          var baked = new Mesh();
          baked.name = $"{renderer.name}_baked";
          renderer.BakeMesh(baked);
          AssetDatabase.CreateAsset(baked, filename.Replace(Application.dataPath, "Assets"));
        }
      }
    }
  }
}
