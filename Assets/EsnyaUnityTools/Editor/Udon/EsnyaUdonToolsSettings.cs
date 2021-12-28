using UnityEngine;
using UnityEditor;

namespace EsnyaFactory
{
    public class EsnyaUdonToolsSettings : ScriptableObject
    {
        public const string filePath = "Assets/EsnyaUdonTools.asset";
        [HideInInspector] public string youtubeApiKey;

        public static EsnyaUdonToolsSettings Load()
        {
            var exists = AssetDatabase.LoadAssetAtPath<EsnyaUdonToolsSettings>(filePath);
            if (exists) return exists;

            var newAsset = CreateInstance<EsnyaUdonToolsSettings>();
            AssetDatabase.CreateAsset(newAsset, filePath);
            return newAsset;
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.Refresh();
        }
    }
}
