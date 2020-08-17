namespace EsnyaFactory {
  using System.Linq;
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEditor;

  class CrunchAll: EditorWindow {

    [MenuItem("EsnyaTools/CrunchAll")]
    [MenuItem("Assets/EsnyaTools/CrunchAll")]
    private static void Init()
    {
      var window = EditorWindow.GetWindow<CrunchAll>();
      window.Show();
    }

    private int textureCount;
    private List<TextureImporter> importers;

    private void OnGUI()
    {
      titleContent = new GUIContent("Crunch All");
      ListImporters();

      EditorGUILayout.LabelField($"{textureCount - importers.Count}/{textureCount} textures are crunched.");

      if (GUILayout.Button("Crunch All")) {
        SetCrunchAll();
      }
    }

    private void ListImporters()
    {
      var textures = AssetDatabase.FindAssets("t:Texture2D", new []{"Assets"});
      textureCount = textures.Count();
      importers = textures
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(path => TextureImporter.GetAtPath(path) as TextureImporter)
        .Where(importer => importer?.crunchedCompression == false)
        .ToList();
    }

    private void SetCrunchAll()
    {
      importers.ForEach(importer => {
        importer.crunchedCompression = true;
        importer.SaveAndReimport();
      });
    }
  }
}
