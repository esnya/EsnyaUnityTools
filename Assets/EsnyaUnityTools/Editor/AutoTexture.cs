namespace EsnyaFactory {
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using UnityEngine;
  using UnityEditor;
    using Ludiq.OdinSerializer.Utilities;

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
    public enum ShaderType {
      Standard,
      AutodeskInteractive,
    }

    public string texturesPath = "Assets/Textures";
    public string materialsPath = "Assets/Materials";
    public ShaderType shaderType = ShaderType.Standard;
    public SearchMode searchMode = SearchMode.Material;
    [Space][Header("Patterns")]
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

    private string[] GetSearchPaths()
    {
      switch (searchMode)
      {
        case SearchMode.Material:
          return new [] { materialsPath };
        case SearchMode.Texture:
          return new [] { texturesPath };
      }
      return new string[0];
    }

    private Dictionary<Material, Dictionary<string, string>> textures;
    private void UpdateTextureList()
    {
      var paths = AssetDatabase.FindAssets("t:Texture2D", new [] { texturesPath }).Select(AssetDatabase.GUIDToAssetPath).ToList();

      var props = maps.Select(serializedWindow.FindProperty).ToList();
      textures = Selection.objects
        .Select(o => o as Material)
        .Where(m => m != null)
        .Select(m => (
          m, props.Select(p => (p.name, FindTexture(m.name, p.stringValue, paths))).ToDictionary(t => t.name, t => t.Item2)
        ))
        .ToDictionary(t => t.m, t => t.Item2);
        textures.ForEach(t => Debug.Log($"{t.Key}: {t.Value}"));
    }

    private string FindTexture(string name, string pattern, IEnumerable<string> paths)
    {
      var r = new Regex(pattern, regexOptions);
      var founds = paths.Where(path => path.Contains(name)).Where(path => r.IsMatch(path)).Take(2).ToList();
      if (founds.Count == 2) Debug.LogWarning("Conflicted");
      return founds.FirstOrDefault();
    }

    private void Apply()
    {
      UpdateTextureList();
      foreach (var a in textures)
      {
        var material = a.Key;
        foreach (var b in a.Value)
        {
          var name = b.Key;
          if (!material.HasProperty(name))
          {
            Debug.Log($"{material.name} has not property ${name}. Skipping.");
            continue;
          }

          material.SetTexture(name, AssetDatabase.LoadAssetAtPath<Texture2D>(b.Value));
          if (name == "_MainTex") material.SetColor("_Color", Color.white);
          if (name == "_EmissionMap") material.SetColor("_EmissionColor", Color.white);
          EditorUtility.SetDirty(material);
        }
      }

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

        if (textures == null) return;
        foreach (var a in textures)
        {
          var material = a.Key;
          EditorGUILayout.LabelField(material.name);

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
