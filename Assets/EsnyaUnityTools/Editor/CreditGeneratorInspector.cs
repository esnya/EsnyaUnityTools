using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace EsnyaFactory {
  [CustomEditor(typeof(CreditGenerator))]
  public class CreditGeneratorInspector : Editor {
    public bool filesFoldout, addFoldout = false;
    public List<string> files = new List<string>();

    static List<string> GetFileList(CreditGenerator generator)
    {
      return generator.fileNames
          .Split(' ')
          .SelectMany(AssetDatabase.FindAssets)
          .Select(AssetDatabase.GUIDToAssetPath)
          .ToList();
    }

    public override void OnInspectorGUI() {
      var generator = target as CreditGenerator;
      using (var change = new EditorGUI.ChangeCheckScope()) {
        base.OnInspectorGUI();

        if (change.changed) files = GetFileList(generator);
      }


      EditorGUILayout.Space();

      filesFoldout = EditorGUILayout.Foldout(filesFoldout, "License Files");
      if (filesFoldout) {
        generator.files = generator.files
          .Where(file => {
            using (new EditorGUILayout.HorizontalScope()) {
              var button = GUILayout.Button("Remove", GUILayout.ExpandWidth(false));
              EditorGUILayout.LabelField(file);
              return !button;
            }
          })
          .ToList();
      }

      EditorGUILayout.Space();

      addFoldout = EditorGUILayout.Foldout(addFoldout, "Add License Files");
      if (addFoldout) {
        using (var change = new EditorGUI.ChangeCheckScope()) {
          var added = files
            .Where(file => !generator.files.Contains(file))
            .Where(file => {
              using (new EditorGUILayout.HorizontalScope()) {
                var button = GUILayout.Button("Add", GUILayout.ExpandWidth(false));
                EditorGUILayout.LabelField(file);
                return button;
              }
            })
            .ToList();

          if (change.changed) {
            generator.files = generator.files.Where(files.Contains).Concat(added).OrderBy(f => f).ToList();
          }
        }
      }

      EditorGUILayout.Space();

      if (GUILayout.Button("Refresh File List")) {
        files = GetFileList(generator);
      }

      if (GUILayout.Button("Update Text")) {
        var lines = generator.files
          .Where(File.Exists)
          .Select(file => (
            Path.GetFileName(Path.GetDirectoryName(file)),
            File.ReadAllLines(file).Select(line => line.Trim())
          ))
          .Select(t => {
            var name = t.Item1;
            var license = t.Item2.FirstOrDefault(line => line.ToLower().Contains("license")) ?? "";
            if (license.Contains("SIL Open Font License")) license = "SIL Open Font License";
            else if (license.Contains("MIT License") || t.Item2.Any(line => line.Contains("THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND"))) license = "MIT License";
            var copyright = license == "Apache License"
              ? ""
              : t.Item2.FirstOrDefault(line => line.ToLower().Contains("copyright"));
            return generator.format.Replace("%name%", name).Replace("%license%", license).Replace("%copyright%", copyright);
          })
          .Prepend(generator.prefix)
          .Append(generator.suffix)
          .Where(line => !string.IsNullOrWhiteSpace(line));
        generator.targetText.text = string.Join("\n", lines);
        generator.targetText.ForceMeshUpdate();
      }
    }
  }
}
