namespace EsnyaFactory {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using UnityEngine;
  using UnityEditor;
  using UnityEditor.Animations;

  [CustomEditor(typeof(FBXAnimationConverter))]
  public class FBXAnimationConverterInspector : Editor {
    private static IEnumerable<AnimationClip> EnumerateClips(UnityEngine.Object source) {
      return AssetDatabase
        .LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(source))
        .Where(o => o is AnimationClip && !o.name.StartsWith("__preview__"))
        .Select(o => o as AnimationClip);
    }

    private static T AddOrCreateObject<T>(string path, Func<T, bool> predicate)where T: UnityEngine.Object, new() {
      var found = AssetDatabase.LoadAllAssetsAtPath(path).Where(o => o is T).Select(o => o as T).Where(predicate).FirstOrDefault();
      if (found != null) return found;

      var created = new T();
      created.name = "New Object";
      AssetDatabase.AddObjectToAsset(created, path);

      return created;
    }

    private static string ReplacePlaceHolders(Match match, string str) {
      return new Regex("\\$([0-9]+|\\&)").Replace(str, m => {
        var index = m.Value.Substring(1);
        if (index == "&") {
          return match.Value;
        }

        var n = int.Parse(index);
        if (n >= match.Groups.Count || n < 0) return m.Value;
        return match.Groups[n].Value;
      });
    }

    public static void GenerateAnimationClips(FBXAnimationConverter extractor, bool cleanUp) {
      var source = extractor.source;
      var converterProfiles = extractor.converterProfiles;
      var savePath = AssetDatabase.GetAssetPath(extractor);

      var clips = EnumerateClips(source).ToList();

      var generatedClips = converterProfiles.SelectMany(e => {
        var clipRegex = new Regex(e.clipFilter, RegexOptions.IgnoreCase);

        return clips
          .Select(c => (clipRegex.Match(c.name), c))
          .Where(t => t.Item1.Success)
          .Select(t => {
            var nameMatch = t.Item1;
            var sourceClip = t.Item2;
            var pathRegex = new Regex(ReplacePlaceHolders(nameMatch, e.pathFilter), RegexOptions.IgnoreCase);
            Debug.Log(ReplacePlaceHolders(nameMatch, e.pathFilter));
            var propertyRegex = new Regex(ReplacePlaceHolders(nameMatch, e.propertyFilter), RegexOptions.IgnoreCase);
            Debug.Log(ReplacePlaceHolders(nameMatch, e.propertyFilter));

            var name = clipRegex.Replace(sourceClip.name, e.clipName);

            var clip = AddOrCreateObject<AnimationClip>(savePath, c => c.name == name);
            clip.ClearCurves();
            clip.name = name;
            clip.frameRate = sourceClip.frameRate;

            foreach (var binding in AnimationUtility.GetCurveBindings(sourceClip).Where(b => pathRegex.Match(b.path).Success && propertyRegex.Match(b.propertyName).Success)) {
              var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
              AnimationUtility.SetEditorCurve(clip, binding, curve);
            }

            EditorUtility.SetDirty(clip);

            return clip;
          });
        }).ToList();

      if (cleanUp) {
        foreach (var unused in AssetDatabase.LoadAllAssetsAtPath(savePath).Where(o => o is AnimationClip && !generatedClips.Contains(o))) {
          Debug.Log($"Removing {unused.name}");
          AssetDatabase.RemoveObjectFromAsset(unused);
        }
      }

      EditorUtility.SetDirty(extractor);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
    }
    private bool clips = false;
    private bool cleanUp = false;

    public override void OnInspectorGUI() {
      var extractor = target as FBXAnimationConverter;
      serializedObject.Update();

      var source = EditorGUILayout.ObjectField("Source", serializedObject.FindProperty("source").objectReferenceValue, typeof(GameObject), false) as GameObject;
      serializedObject.FindProperty("source").objectReferenceValue = source;

      clips = EditorGUILayout.Foldout(clips, "Source Clips");
      if (source != null && clips) {
        using (new EditorGUI.DisabledGroupScope(true)) {
          foreach (var clip in EnumerateClips(source)) {
            EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
          }
        }
      }

      EditorGUILayout.PropertyField(serializedObject.FindProperty("converterProfiles"), true);

      using (new EditorGUI.DisabledGroupScope(source == null)) {
        if (GUILayout.Button("Generate Animation Clips")) {
          GenerateAnimationClips(extractor, cleanUp);
        }
      }

      cleanUp = EditorGUILayout.Toggle("Remove Unused Clips", cleanUp);

      serializedObject.ApplyModifiedProperties();
    }
  }

  public class FBXAnimationConverterPostprocessor : AssetPostprocessor {
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
      var table = FBXAnimationConverter
        .FindObjectsOfType<FBXAnimationConverter>()
        .Select(a => (AssetDatabase.GetAssetPath(a), a))
        .GroupBy(t => t.Item1)
        .ToDictionary(g => g.Key, g => g.Select(t => t.Item2).ToList());

      importedAssets
        .Concat(movedAssets)
        .Where(path => table.ContainsKey(path))
        .SelectMany(path => table[path])
        .ToList()
        .ForEach(c => FBXAnimationConverterInspector.GenerateAnimationClips(c, false));
    }
  }
}
