namespace EsnyaFactory {
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using UnityEngine;
  using UnityEditor;
    using System.IO;

    public class AutoTexture : EditorWindow {
    private static void FillTexutre(Material material, string name, Regex pattern, IEnumerable<string> textures) {
      if (material.GetTexture(name) != null) return;

      var texture = textures.Where(path => pattern.Match(path).Success).Select(AssetDatabase.LoadAssetAtPath<Texture2D>).FirstOrDefault();
      if (texture == null) return;

      Debug.Log($"{name} texture found: {AssetDatabase.GetAssetPath(texture)}");
      material.SetTexture(name, texture);
      EditorUtility.SetDirty(material);
    }
    private static void FillTextures(Material material, string keyword) {
      var basePathPattern = new Regex("/[^/]*$");
      var dir = basePathPattern.Replace(AssetDatabase.GetAssetPath(material), "");
      var textures = AssetDatabase.FindAssets($"{keyword} t:Texture2D", new [] { dir, basePathPattern.Replace(dir, "") }).Select(AssetDatabase.GUIDToAssetPath);

      FillTexutre(material, "_MainTex", new Regex(@"_(color|albedo)", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_MetallicGlossMap", new Regex(@"_(metallic|metallness)", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_BumpMap", new Regex(@"_(normal)", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_ParallaxMap", new Regex(@"_(height|displace|displacement)", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_OcclusionMap", new Regex(@"_(ao|occlusion)", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_EmissionMap", new Regex(@"_(emi|emission)", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_SpecGlossMap", new Regex(@"_(roughness)", RegexOptions.IgnoreCase), textures);
    }

    [MenuItem("CONTEXT/Material/Auto Fill Textures")]
    [MenuItem("EsnyaTools/Auto Texture")]
    private static void ShowWindow(MenuCommand menuCommand) {
      var window = GetWindow<AutoTexture>();
      window.Show();
    }

    public enum SearchMode {
      Material,
      Texture,
    }

    public Shader shader = Shader.Find("Standard");
    public SearchMode searchMode = SearchMode.Material;
    public string savePath = "Assets";
    [Space][Header("Patterns")]
    public string materialName = @"(?<=/)[a-zA-Z0-9_ ]+(?=_[a-zA-Z]+\.[a-z]+$)";
    [Tooltip("Albedo")] public string _MainTex = @"(color|albedo|diffuse)";
    [Tooltip("Metallic")] public string _MetallicGlossMap = @"(metallic|metallness)";
    [Tooltip("Normal")] public string _BumpMap = @"(normal|norm|nrm)";
    [Tooltip("Height")] public string _ParallaxMap = @"(height|displace|displacement)";
    [Tooltip("Occlusion")] public string _OcclusionMap = @"(ao|occlusion)";
    [Tooltip("Emission")] public string _EmissionMap = @"(emi|emission)";
    [Tooltip("Roughness")] public string _SpecGlossMap = @"(roughness)";
    public RegexOptions regexOptions = RegexOptions.IgnoreCase;
    [System.NonSerialized] private SerializedObject serializedWindow;
    private void OnEnable()
    {
      serializedWindow = new SerializedObject(this);
      titleContent = new GUIContent("Auto Texture");
      UpdateTextureList();
    }

    private readonly string[] maps = {
      "_MainTex",
      "_MetallicGlossMap",
      "_BumpMap",
      "_ParallaxMap",
      "_OcclusionMap",
      "_EmissionMap",
      "_SpecGlossMap",
    };
    private Dictionary<string, Dictionary<string, string>> textureTable;
    private IEnumerable<string> ListMaterialNames()
    {
      if (searchMode == SearchMode.Material)
      {
        return Selection.objects
          .Select(o => o as Material)
          .Where(m => m != null)
          .Select(m => m.name);
      }

      if (searchMode == SearchMode.Texture)
      {
        var r = new Regex(materialName, regexOptions);
        return Selection.objects
          .Select(o => o as Texture2D)
          .Where(t => t != null)
          .Select(AssetDatabase.GetAssetPath)
          .Select(path => r.Match(path))
          .Where(m => m.Success)
          .Select(m => m.Value)
          .Distinct();
      }

      return Enumerable.Empty<string>();
    }
    private void UpdateTextureList()
    {
      var materialNames = ListMaterialNames();
      var paths = AssetDatabase.FindAssets("t:Texture2D").Select(AssetDatabase.GUIDToAssetPath).ToList();

      var props = maps.Select(serializedWindow.FindProperty).ToList();
      textureTable = materialNames
        .Select(name => (
          name, props.Select(p => (p.name, FindTexture(name, p.stringValue, paths))).ToDictionary(t => t.name, t => t.Item2)
        ))
        .ToDictionary(t => t.name, t => t.Item2);
    }

    private string FindTexture(string name, string pattern, IEnumerable<string> paths)
    {
      var r = new Regex(pattern, regexOptions);
      var founds = paths.Where(path => path.Contains(name)).Where(path => r.IsMatch(path)).Take(2).ToList();
      if (founds.Count == 2) Debug.LogWarning("Conflicted");
      return founds.FirstOrDefault();
    }


    private Material LoadOrCreateMaterial(string name)
    {
      var path = $"{savePath}/{name}.mat";
      var found = AssetDatabase.LoadAssetAtPath<Material>(path);
      if (found != null) return found;

      if (File.Exists(path)) File.Delete(path);
      var material = new Material(shader) { name = name };
      AssetDatabase.CreateAsset(material, path);
      return material;
    }

    private IEnumerable<Material> ListTargetMaterials()
    {
      if (searchMode == SearchMode.Material)
      {
        return Selection.objects.Select(o => o as Material);
      }

      if (searchMode == SearchMode.Texture)
      {
        return textureTable.Keys.Select(LoadOrCreateMaterial);
      }

      return Enumerable.Empty<Material>();
    }

    private void Apply()
    {
      UpdateTextureList();


      AssetDatabase.StartAssetEditing();

      var materials = ListTargetMaterials();
      foreach (var material in materials)
      {
        if (!textureTable.ContainsKey(material.name)) continue;

        material.shader = shader;

        var textures = textureTable[material.name];

        foreach (var b in textures)
        {
          var name = b.Key;
          if (!material.HasProperty(name))
          {
            Debug.Log($"{material.name} has not property {name}. Skipping.");
            continue;
          }

          var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(b.Value);
          material.SetTexture(name, texture);

          var importer = AssetImporter.GetAtPath(b.Value) as TextureImporter;
          if (importer != null)
          {
            if (name == "_BumpMap") importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = name == "_MainTex" || name == "_EmissionMap";
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
          }

          EditorUtility.SetDirty(material);
        }
      }

      AssetDatabase.StopAssetEditing();
      AssetDatabase.Refresh();
    }

    private Vector2 scrollPosition;
    private void OnGUI()
    {
      using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
      {
        scrollPosition = scroll.scrollPosition;
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
          serializedWindow.Update();
          var property = serializedWindow.GetIterator();
          property.NextVisible(true);
          property.NextVisible(true);
          do {
            switch (property.name) {
              case "regexOptions":
                regexOptions = (RegexOptions)EditorGUILayout.EnumFlagsField(property.displayName, regexOptions);
                break;
              default:
                EditorGUILayout.PropertyField(property);
                break;
            }
          } while (property.NextVisible(true));
          serializedWindow.ApplyModifiedProperties();

          EditorGUILayout.Space();

          if (GUILayout.Button("Update List")) UpdateTextureList();
          if (GUILayout.Button("Apply")) Apply();
        }

        EditorGUILayout.Space();

        if (textureTable == null) return;
        foreach (var a in textureTable)
        {
          var name = a.Key;
          EditorGUILayout.LabelField(name);

          using (new EditorGUI.IndentLevelScope())
          {
            foreach (var b in a.Value)
            {
              var key = b.Key;
              var texture = b.Value;
              EditorGUILayout.LabelField(key, texture);
            }
          }
        }
      }
    }
  }
}
