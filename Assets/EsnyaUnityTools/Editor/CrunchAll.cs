namespace EsnyaFactory {
  using System.Linq;
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEditor;

  class CrunchAll: EditorWindow {

    [MenuItem("EsnyaTools/Crunch All")]
    [MenuItem("Assets/EsnyaTools/Crunch All")]
    private static void Init()
    {
      var window = EditorWindow.GetWindow<CrunchAll>();
      window.Show();
    }

    private int textureCount;
    private List<TextureImporter> toCrunch;
    private List<TextureImporter> toStreaming;
    private bool crunch =  true;
    private bool streaming = true;

    private void OnGUI()
    {
      titleContent = new GUIContent("Crunch All");
      ListImporters();

      EditorGUILayout.LabelField($"{toCrunch.Count}/{textureCount} textures are not crunch comporessed.");
      EditorGUILayout.LabelField($"{toStreaming.Count}/{textureCount} texture's streaming mipmaps are disabled.");

      crunch = EditorGUILayout.Toggle("Crunch Comporess", crunch);
      streaming = EditorGUILayout.Toggle("Streaming Mipmaps", streaming);

      EditorGUI.BeginDisabledGroup(!crunch && !streaming);
      if (GUILayout.Button("Execute")) {
        Execute();
      }
      EditorGUI.EndDisabledGroup();
    }

    private void ListImporters()
    {
      var textures = AssetDatabase.FindAssets("t:Texture2D", new []{"Assets"});
      textureCount = textures.Count();
      var importers = textures
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(path => TextureImporter.GetAtPath(path) as TextureImporter)
        .ToList();
      toCrunch = importers.Where(importer => importer?.crunchedCompression == false).ToList();
      toStreaming = importers.Where(importer => importer?.streamingMipmaps == false).ToList();
    }

    private void Execute()
    {
      ListImporters();
      var toReimport = toCrunch.Concat(toStreaming).Distinct().Select(importer => {
        if (crunch) importer.crunchedCompression = true;
        if (streaming) importer.streamingMipmaps = true;
        EditorUtility.SetDirty(importer);
        return importer.assetPath;
      }).ToList();
      toReimport.ForEach(path => AssetDatabase.WriteImportSettingsIfDirty(path));
      AssetDatabase.Refresh();
    }
  }
}
