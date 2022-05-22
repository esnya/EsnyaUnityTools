namespace EsnyaFactory {
  using UnityEngine;
  using UnityEditor;

  public class AssetUtility {
    public static T GetOrCreateAsset<T>(string path, bool scriptable = false) where T : Object, new () {
      var asset = AssetDatabase.LoadAssetAtPath<T>(path);
      if (asset != null) return asset;

      asset = scriptable ? ScriptableObject.CreateInstance(typeof(T)) as T : new T();
      AssetDatabase.CreateAsset(asset, path);
      return asset;
    }
  }
}
