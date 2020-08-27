namespace EsnyaFactory {
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using UnityEngine;
  using UnityEditor;

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

      FillTexutre(material, "_MainTex", new Regex(@"_(color|albedo)\.[^.]+$", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_MetallicGlossMap", new Regex(@"_(metallic|metallness)\.[^.]+$", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_BumpMap", new Regex(@"_(normal)\.[^.]+$", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_ParallaxMap", new Regex(@"_(height|displace|displacement)\.[^.]+$", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_OcclusionMap", new Regex(@"_(ao|occlusion)\.[^.]+$", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_EmissionMap", new Regex(@"_(emi|emission)\.[^.]+$", RegexOptions.IgnoreCase), textures);
      FillTexutre(material, "_SpecGlossMap", new Regex(@"_(roughness)\.[^.]+$", RegexOptions.IgnoreCase), textures);
    }

    [MenuItem("CONTEXT/Material/Auto Fill Textures")]
    private static void ShowWindow(MenuCommand menuCommand) {
      var material = menuCommand.context as Material;
      FillTextures(material, material.name);
      FillTextures(material, new Regex(@"[ _]*[0-9]+$", RegexOptions.IgnoreCase).Replace(material.name, ""));
      AssetDatabase.Refresh();
    }
  }
}
