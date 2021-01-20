using UnityEditor;
using UnityEngine;

namespace EsnyaFactory
{
    public class PersistentObject : ScriptableObject
    {
        public virtual string GetPersistentKey() {
            return $"{GetType().FullName}.{name}";
        }

        void OnEnable()
        {
            Load();
        }

        public void Load()
        {
            var data = EditorPrefs.GetString(GetPersistentKey(), JsonUtility.ToJson(this, false));
            JsonUtility.FromJsonOverwrite(data, this);
        }

        void OnDisable()
        {
            Save();
        }

        public void Save()
        {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(GetPersistentKey(), data);
        }
    }
}
