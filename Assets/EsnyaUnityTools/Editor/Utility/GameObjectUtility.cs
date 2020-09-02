namespace EsnyaFactory {
  using System.Linq;
  using UnityEngine;
  using UnityEditor;
  using UnityEditor.Animations;

  public class GameObjectUtility {
    public static GameObject GetOrAddChild(GameObject gameObject, string name) {
      var child = gameObject.transform.Find(name);
      if (child != null) return child.gameObject;

      child = new GameObject(name).transform;
      child.SetParent(gameObject.transform);
      return child.gameObject;
    }
    public static T GetOrAddComponent<T>(GameObject gameObject) where T : Behaviour {
      var component = gameObject.GetComponent<T>();
      if (component != null) return component;
      return gameObject.AddComponent<T>();
    }


    public static string GetHierarchyPath(Transform target, Transform root = null)
    {
      string path = target.gameObject.name;
      Transform parent = target.parent;
      while (parent != null && parent != root)
      {
        path = parent.name + "/" + path;
        parent = parent.parent;
      }
      return path;
    }
  }
}

