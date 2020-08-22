namespace EsnyaFactory {
  using UnityEditor;
  using UnityEditor.Animations;
  using UnityEngine;

  public class BlendTreeCreator : EditorWindow {
    [MenuItem("EsnyaTools/Create BlendTree")]
    [MenuItem("Assets/EsnyaTools/Create BlendTree")]
    private static void ShowWindow() {
      var window = GetWindow<BlendTreeCreator>();
      window.Show();
    }

    void OnEnable()
    {
      titleContent = new GUIContent("Create BlendTree");
    }

    private DefaultAsset directory;
    private string blendTreeName;
    private void OnGUI() {
      directory = EEU.AssetDirectoryField("Output Directory", directory);
      blendTreeName = EditorGUILayout.TextField("Name", blendTreeName);

      EEU.Disabled(directory == null || blendTreeName.Length == 0, () => {
        EEU.Button("Create", () => {
          var tree = new BlendTree() {
            name = blendTreeName,
          };
          var path = $"{AssetDatabase.GetAssetPath(directory)}/{blendTreeName}.asset";
          for (int i = 1; AssetDatabase.LoadAssetAtPath<Object>(path) != null; i++) {
            path = $"{AssetDatabase.GetAssetPath(directory)}/{blendTreeName} ({i}).asset";
          };
          AssetDatabase.CreateAsset(tree, path);
          AssetDatabase.SaveAssets();
        });
      });
    }
  }
}
