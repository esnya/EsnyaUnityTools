using System.IO;
using UnityEditor;
using UnityEngine;

#if VRCSDK && UDON
using VRC.Udon;
#endif

namespace EsnyaFactory
{
    public class EsnyaUdonToolsSettings : ScriptableObject
    {
        public const string SettingsPath = "ProjectSettings/EsnyaUdonTools.asset";

        [Tooltip("Saved SerializedUdonPrograms individually for each nearest directory.")]
        public bool customUdonProgramFolder;

        [Tooltip("Increment version of package.json when world buliding.")]
        public bool autoNpmPackageVersion;

        [HideInInspector]
        public string youtubeApiKey;

        private static EsnyaUdonToolsSettings instance;
        public static EsnyaUdonToolsSettings Instance
        {
            get
            {
                if (!instance)
                {
                    instance = CreateInstance<EsnyaUdonToolsSettings>();
                    if (File.Exists(SettingsPath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsPath), instance);
                }
                return instance;
            }
        }

        private static SerializedObject serialized;
        public static SerializedObject Serialized
        {
            get
            {
                if (serialized == null)
                {
                    serialized = new SerializedObject(Instance);
                }
                return serialized;
            }
        }

        public void Save()
        {
            var json = JsonUtility.ToJson(this);
            File.WriteAllText(SettingsPath, json);
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingProvider()
        {
            return new SettingsProvider("Project/Esnya Unity Tools/Udon", SettingsScope.Project)
            {
                label = "Esnya Udon Tools",
                guiHandler = (searchContext) =>
                {
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        Serialized.Update();

                        var property = Serialized.GetIterator();
                        property.NextVisible(true);
                        while (property.NextVisible(false))
                        {
                            EditorGUILayout.PropertyField(property, true);
                        }
                        Serialized.ApplyModifiedProperties();

                        if (change.changed) Instance.Save();
                    }
                },
            };
        }
    }
}
