using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public class ExAssetUtility
    {
        public class ExAssetRoot : ScriptableObject {}

        static public void PackAssets(IEnumerable<Object> objects, string path)
        {
            var root = ScriptableObject.CreateInstance<ExAssetRoot>();
            AssetDatabase.CreateAsset(root, path);

            foreach (var o in objects) {
                if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o))) continue;
                AssetDatabase.AddObjectToAsset(o, path);
            }
        }
    }
}
