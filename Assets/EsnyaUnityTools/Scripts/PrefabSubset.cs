using UnityEngine;
using System.Collections.Generic;

namespace EsnyaUnityTools
{
    [CreateAssetMenu(menuName = "EsnyaTools/PrefubSubset")]
    public class PrefabSubset : ScriptableObject
    {
        [System.Serializable]
        public class Subset
        {
            public string name;
            public string path;
            public GameObject prefab;
        }

        public GameObject basePrefab;
        public List<Subset> subsets;
    }
}
