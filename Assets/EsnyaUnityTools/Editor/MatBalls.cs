using UnityEngine;
using UnityEditor;
using System.Linq;

public class MatBalls : EditorWindow {

  [MenuItem("EsnyaTools/Mat Balls")]
  private static void ShowWindow() {
    var window = GetWindow<MatBalls>();
    window.titleContent = new GUIContent("Mat Balls");
    window.Show();
  }

  enum ColliderType
  {
    None,
    Box,
    Sphere,
    Capsule,
    ConvexMesh,
    Mesh,
  }

  public PrimitiveType primitiveType = PrimitiveType.Sphere;
  public Transform parent;
  public Vector3 offset = Vector3.zero, scale = Vector3.one;
  private void OnGUI() {
    var materials = Selection.objects.Select(o => o as Material).Where(m => m != null).ToList();

    primitiveType = (PrimitiveType)EditorGUILayout.EnumPopup("Mesh", primitiveType);
    parent = EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true) as Transform;
    offset = EditorGUILayout.Vector3Field("Offset", offset);
    scale = EditorGUILayout.Vector3Field("Scale", scale);

    if (GUILayout.Button($"Generate {materials.Count} objects"))
    {
      var position = Vector3.zero;
      foreach (var material in materials)
      {
        var o = GameObject.CreatePrimitive(primitiveType);
        o.name = material.name;
        Undo.RegisterCreatedObjectUndo(o, "Create");

        if (parent != null) o.transform.parent = parent;
        o.transform.localPosition = position;
        o.transform.localScale = scale;

        o.GetComponent<MeshRenderer>().sharedMaterial = material;

        position += offset;
      }
    }
  }
}
