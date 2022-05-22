using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace EsnyaTools {
  public class MeshTools : EditorWindow {
    public enum CreationMode {
      BakeOnly,
      ChildOfSelected,
      SiblingOfSelected,
      // ChildOfTarget,
      // SiblingOfTarget,
    }

    public enum RemoveMode {
      DoNothing,
      Disable,
      EditorOnly,
      Remove,
    }

    public string savePath;
    public CreationMode creationMode;
    public RemoveMode targetRemoveMode;
    public RemoveMode selectedRemoveMode;
    public bool copyMeshRenderer;

    [MenuItem("EsnyaTools/Mesh Tools")]
    private static void ShowWindow() {
      var window = GetWindow<MeshTools>();
      window.titleContent = new GUIContent("Mesh Tools");
      window.Show();
    }

    private void OnGUI() {
      var meshRendererCount = Selection.gameObjects.Select(o => o.GetComponentsInChildren<MeshRenderer>()).SelectMany(a => a).Count();
      var skinnedMeshRendererCount = Selection.gameObjects.Select(o => o.GetComponentsInChildren<SkinnedMeshRenderer>()).SelectMany(a => a).Count();
      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        EditorGUILayout.LabelField($"{meshRendererCount} meshes and {skinnedMeshRendererCount} skinned meshes in selected GameObject(s).");
      }

      EditorGUILayout.Space();

      var serializedObject = new SerializedObject(this);
      serializedObject.Update();
      using (new EditorGUILayout.HorizontalScope()) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(savePath)));
        if (GUILayout.Button("...")) {
          var directory = EditorUtility.SaveFolderPanel("Save To", Application.dataPath, "Assets");
          savePath = directory.Replace(Application.dataPath, "Assets");
        }
      }
      EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creationMode)));
      EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(targetRemoveMode)));
      EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(selectedRemoveMode)));

      EditorGUILayout.Space();

      using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(copyMeshRenderer)));

        using (new EditorGUI.DisabledGroupScope(skinnedMeshRendererCount == 0)) {
          if (GUILayout.Button("Bake Skinned Mesh")) {
            serializedObject.ApplyModifiedProperties();
            BakeSelected();
          }
        }
      }

      // EditorGUILayout.Space();

      // using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
      //   using (new EditorGUI.DisabledGroupScope(meshRendererCount == 0/* || creationMode >= CreationMode.ChildOfTarget*/)) {
      //     if (GUILayout.Button("Merge Meshes")) {
      //       serializedObject.ApplyModifiedProperties();
      //       MergeSelected();
      //     }
      //   }
      // }

      serializedObject.ApplyModifiedProperties();
    }

    public static string ToUniqueAssetPath(string path)
    {
      if (AssetDatabase.GetMainAssetTypeAtPath(path) == null) return path;

      var directory = Path.GetDirectoryName(path);
      var filename = Path.GetFileNameWithoutExtension(path);
      var extension = Path.GetExtension(path);

      for (int i = 1; i < 256; i++) {
        var newPath = $"{directory}/{filename} ({i}){extension}";
        Debug.Log(newPath);
        if (AssetDatabase.GetMainAssetTypeAtPath(newPath) == null) {
          return newPath;
        }
      }

      throw new System.Exception("Failed to create asset");
    }

    void CreateNewObject(GameObject targetObject, GameObject selectedObject, string actionName, System.Action<GameObject> onCreated) {
      if (creationMode == CreationMode.BakeOnly) return;

      var newObject = new GameObject($"{targetObject.name} {actionName}");
      Undo.RegisterCreatedObjectUndo(newObject, actionName);
      PlaceNewObject(targetObject, selectedObject, newObject);
      onCreated(newObject);

      RemoveObject(targetObject, actionName, targetRemoveMode);
      RemoveObject(selectedObject, actionName, selectedRemoveMode);
    }

    void PlaceNewObject(GameObject targetObject, GameObject selectedObject, GameObject newObject) {
      newObject.transform.parent = targetObject.transform;
      newObject.transform.localPosition = Vector3.zero;
      newObject.transform.localRotation = Quaternion.identity;
      newObject.transform.localScale = Vector3.one;

      switch (creationMode) {
        case CreationMode.BakeOnly:
          return;
        // case CreationMode.ChildOfTarget:
        //   newObject.transform.parent = targetObject.transform;
        //   break;
        // case CreationMode.SiblingOfTarget:
        //   newObject.transform.parent = targetObject.transform.parent;
        // break;
        case CreationMode.ChildOfSelected:
          newObject.transform.parent = selectedObject.transform;
          break;
        case CreationMode.SiblingOfSelected:
          newObject.transform.parent = selectedObject.transform.parent;
          break;
      }
    }

    void RemoveObject(GameObject obj, string actionName, RemoveMode removeMode) {
      Undo.RecordObject(obj, actionName);
      switch (removeMode) {
        case RemoveMode.DoNothing:
          return;
        case RemoveMode.Disable:
          obj.SetActive(false);
          return;
        case RemoveMode.EditorOnly:
          obj.SetActive(false);
          obj.tag = "EditorOnly";
          return;
        case RemoveMode.Remove:
          DestroyImmediate(obj);
          return;
      }
    }

    public void CopyRendererProperties(Renderer sourceRenderer, Renderer destinationRenderer) {
      destinationRenderer.enabled = sourceRenderer.enabled;
      destinationRenderer.lightmapScaleOffset = sourceRenderer.lightmapScaleOffset;
      destinationRenderer.lightProbeProxyVolumeOverride = sourceRenderer.lightProbeProxyVolumeOverride;
      destinationRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
      destinationRenderer.motionVectorGenerationMode = sourceRenderer.motionVectorGenerationMode;
      destinationRenderer.probeAnchor = sourceRenderer.probeAnchor;
      destinationRenderer.receiveShadows = sourceRenderer.receiveShadows;
      destinationRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
      destinationRenderer.rendererPriority = sourceRenderer.rendererPriority;
      destinationRenderer.renderingLayerMask = sourceRenderer.renderingLayerMask;
      destinationRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
      destinationRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
      destinationRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
      destinationRenderer.sortingLayerName = sourceRenderer.sortingLayerName;
      destinationRenderer.sortingOrder = sourceRenderer.sortingOrder;
    }

    public void BakeSelected() {
      foreach (var selectedObject in Selection.gameObjects) {
        var skinnedMeshRenderers = selectedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        var meshRenderers = selectedObject.GetComponentsInChildren<MeshRenderer>();
        foreach (var skinnedMeshRenderer in skinnedMeshRenderers) {
          var targetObject = skinnedMeshRenderer.gameObject;
          var name = $"{targetObject.name}_Baked";
          var mesh = new Mesh();
          mesh.name = name;
          skinnedMeshRenderer.BakeMesh(mesh);
          AssetDatabase.CreateAsset(mesh, ToUniqueAssetPath($"{savePath}/{name}.asset"));

          CreateNewObject(skinnedMeshRenderer.gameObject, selectedObject, "Baked", newObject => {
            var meshFilter = newObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = newObject.AddComponent<MeshRenderer>();
            CopyRendererProperties(skinnedMeshRenderer, meshRenderer);
          });
        }

        if (copyMeshRenderer) {
          foreach (var meshRenderer in meshRenderers) {
            CreateNewObject(meshRenderer.gameObject, selectedObject, "Clone", newObject => {
              var meshFilter = newObject.AddComponent<MeshFilter>();
              meshFilter.sharedMesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;

              var newMeshRenderer = newObject.AddComponent<MeshRenderer>();
              CopyRendererProperties(meshRenderer, newMeshRenderer);
            });
          }
        }
      }
    }

    public void MergeSelected()
    {
      foreach (var selectedObject in Selection.gameObjects) {
        var meshRenderers = selectedObject.GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers.Length == 0) continue;

        var combine = meshRenderers.Select(r => r.GetComponent<MeshFilter>()).Skip(1).Reverse().Select(f => new CombineInstance() {
          mesh = f.sharedMesh,
          transform = f.transform.localToWorldMatrix * selectedObject.transform.worldToLocalMatrix,
        }).ToArray();
        var materials = meshRenderers.SelectMany(r => r.sharedMaterials).ToArray();
        var name = $"{selectedObject.name}_Merged";
        var mesh = Instantiate(meshRenderers.Select(r => r.GetComponent<MeshFilter>().sharedMesh).First());
        mesh.name = name;
        mesh.CombineMeshes(combine, false, true, false);
        MeshUtility.Optimize(mesh);
        AssetDatabase.CreateAsset(mesh, ToUniqueAssetPath($"{savePath}/{name}.asset"));

        CreateNewObject(meshRenderers[0].gameObject, selectedObject, "Merge", newObject => {
          newObject.transform.position = selectedObject.transform.position;
          newObject.transform.rotation = selectedObject.transform.rotation;

          var meshFilter = newObject.AddComponent<MeshFilter>();
          meshFilter.sharedMesh = mesh;

          var newMeshRenderer = newObject.AddComponent<MeshRenderer>();
          CopyRendererProperties(meshRenderers[0], newMeshRenderer);
          newMeshRenderer.sharedMaterials = materials;
        });
      }
    }
  }
}
