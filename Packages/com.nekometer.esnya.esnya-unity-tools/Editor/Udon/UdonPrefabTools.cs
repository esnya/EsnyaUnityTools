using System;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using VRC.Udon.Serialization.OdinSerializer;

namespace EsnyaFactory
{
    public class UdonPrefabTools
    {
        private static SerializedProperty GetHiddenProperty(UnityEngine.Object obj, string propertyPath)
        {
            return new SerializedObject(obj).FindProperty(propertyPath);
        }
        private static T GetHiddenObject<T>(UnityEngine.Object obj, string propertyPath) where T : UnityEngine.Object
        {
            return GetHiddenProperty(obj, propertyPath).objectReferenceValue as T;
        }
        private static UdonBehaviour GetUdonSharpBackingBehaviour(UdonSharpBehaviour usharpBehaviour)
        {
            return GetHiddenProperty(usharpBehaviour, "_udonSharpBackingUdonBehaviour").objectReferenceValue as UdonBehaviour;
        }
        private static List<T> GetHiddenList<T>(UnityEngine.Object obj, string propertyPath) where T : UnityEngine.Object
        {
            var property = GetHiddenProperty(obj, propertyPath);
            var list = new List<T>(property.arraySize);
            for (var i = 0; i < list.Count; i++)
            {
                list[i] = property.GetArrayElementAtIndex(i).objectReferenceValue as T;
            }
            return list;
        }

        private static AbstractSerializedUdonProgramAsset GetProgramAsset(UdonBehaviour udonBehaviour)
        {
            return GetHiddenProperty(udonBehaviour, "serializedProgramAsset").objectReferenceValue as AbstractSerializedUdonProgramAsset;
        }
        private static IUdonProgram LoadProgram(UdonBehaviour udonBehaviour)
        {
            return GetProgramAsset(udonBehaviour).RetrieveProgram();
        }
        private static UdonSharpBehaviour GetUdonSharpProxyBehaviour(UdonBehaviour udonBehaviour)
        {
            return udonBehaviour.GetComponents<UdonSharpBehaviour>().FirstOrDefault(usharpBehaviour => GetUdonSharpBackingBehaviour(usharpBehaviour) == udonBehaviour);
        }

        private static object ReplaceProxy(object value)
        {
            if (value is Component[])
            {
                var udonBehaviourArray = (value as Component[]).Select(component => component as UdonBehaviour).ToArray();
                if (udonBehaviourArray.Where(udonBehaviour => udonBehaviour != null).Any(UdonSharpEditorUtility.IsUdonSharpBehaviour))
                {
                    return udonBehaviourArray.Select(udonBehaviour => udonBehaviour ? UdonSharpEditorUtility.GetProxyBehaviour(udonBehaviour) : null).ToArray();
                }
            }

            if (value is UdonBehaviour && UdonSharpEditorUtility.IsUdonSharpBehaviour(value as UdonBehaviour))
            {
                return UdonSharpEditorUtility.GetProxyBehaviour(value as UdonBehaviour);
            }

            return value;
        }

        [MenuItem("Assets/EsnyaTools/Repair U# References")]
        private static void RepairUdonSharpReferences()
        {
            try {

            var udonBehaviours = Selection.gameObjects.SelectMany(o => o.GetComponentsInChildren<UdonBehaviour>(true)).ToArray();
            var i = 0;
            foreach (var udonBehaviour in udonBehaviours)
            {
                EditorUtility.DisplayProgressBar("Repaiering", udonBehaviour.ToString(), (float)(++i) / udonBehaviours.Length);

                if (!UdonSharpEditorUtility.IsUdonSharpBehaviour(udonBehaviour)) continue;
                var udonSharpBehaviour = UdonSharpEditorUtility.GetProxyBehaviour(udonBehaviour);

                var property = (new SerializedObject((udonBehaviour))).FindProperty("publicVariablesUnityEngineObjects");
                var publicVariableObjects = new List<UnityEngine.Object>(property.arraySize);
                foreach (var j in Enumerable.Range(0, property.arraySize))
                {
                    var value = property.GetArrayElementAtIndex(j).objectReferenceValue;
                    publicVariableObjects.Add(value);
                }

                var serializedPublicVariablesBytes = Convert.FromBase64String(GetHiddenProperty(udonBehaviour, "serializedPublicVariablesBytesString").stringValue);
                var publicVariables = SerializationUtility.DeserializeValue<IUdonVariableTable>(
                    serializedPublicVariablesBytes,
                    (DataFormat)GetHiddenProperty(udonBehaviour, "publicVariablesSerializationDataFormat").intValue,
                    publicVariableObjects
                );
                foreach (var variableSymbol in publicVariables.VariableSymbols)
                {
                    try
                    {
                        if (!publicVariables.TryGetVariableValue(variableSymbol, out var udonValue)) continue;
                        var udonSharpValue = udonSharpBehaviour.GetProgramVariable(variableSymbol);
                        var convertedValue = ReplaceProxy(udonValue);

                        if (udonSharpValue != udonValue)
                        {
                            Debug.Log($"{udonBehaviour}.{variableSymbol}: {udonSharpValue} -> {convertedValue}");
                            udonSharpBehaviour.SetProgramVariable(variableSymbol, convertedValue);
                            EditorUtility.SetDirty(udonSharpBehaviour);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                AssetDatabase.Refresh();
            }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Assets/EsnyaTools/Repair USharp Behaviour", true)]
        private static bool RepairUdonSharpReferencesValidate()
        {
            return Selection.gameObjects.Select(PrefabUtility.GetPrefabAssetType).Any(t => t == PrefabAssetType.Regular || t == PrefabAssetType.Variant);
        }
    }
}
