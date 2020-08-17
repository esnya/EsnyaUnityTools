#if UNITY_EDITOR
namespace EsnyaFactory
{
  using System.Threading.Tasks;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using UnityEngine;
  using UnityEditor;

  class EEU {
    static public T ObjectField<T>(string label, T value, bool allowSceneObjects) where T : Object
    {
      return EditorGUILayout.ObjectField(label, value, typeof(T), allowSceneObjects) as T;
    }
    static public T ObjectPopup<T>(string label, T value, IEnumerable<T> items, System.Func<T, string> getLabel)
    {
      var oldIndex = items.Select((item, index) => new { item, index }).FirstOrDefault(a => System.Object.Equals(a.item, value))?.index;
      var newIndex =  EditorGUILayout.Popup(label, oldIndex ?? 0, items.Select(getLabel).ToArray());
      return items.Skip(newIndex).FirstOrDefault();
    }
    static public Regex RegexField(string label, Regex value)
    {
      var str = EditorGUILayout.TextField(label, value?.ToString());
      return str != null ? new Regex(str) : null;
    }
    static public T EnumPopup<T>(string label, T value) where T : System.Enum
    {
      return (T)EditorGUILayout.EnumPopup(label, value);
    }

    static public DefaultAsset AssetDirectoryField(string label, DefaultAsset value) {
      var newValue = ObjectField(label, value, false);
      if (newValue != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(newValue))) {
          return newValue;
      }
      return value;
    }
    static public void Button(string label, System.Action action)
    {
      if (GUILayout.Button(label)) {
        action();
      }
    }

    static public void Disabled(bool disabled, System.Action content)
    {
      EditorGUI.BeginDisabledGroup(disabled);
      content();
      EditorGUI.EndDisabledGroup();
    }

    static public Vector2 Scroll(Vector2 scrollPosition, System.Action contents)
    {
      var newScrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
      contents();
      EditorGUILayout.EndScrollView();
      return newScrollPosition;
    }

    static public void Box(System.Action contents)
    {
      Box(null, contents);
    }

    static public void Box(string title, System.Action contents)
    {
      EditorGUILayout.BeginVertical(GUI.skin.box);
      if (title != null) {
        TitleLabel(title);
      }
      contents();
      EditorGUILayout.EndVertical();
    }

    static public void Vertical(System.Action content) {
      EditorGUILayout.BeginVertical();
      content();
      EditorGUILayout.EndVertical();
    }

    static public void Horizontal(System.Action content) {
      EditorGUILayout.BeginHorizontal();
      content();
      EditorGUILayout.EndHorizontal();
    }

    static public void BoxMessage(string message)
    {
      Box(() => EditorGUILayout.LabelField(message));
    }

    static public void TitleLabel(string label)
    {
      EditorGUILayout.LabelField(label, new GUIStyle() { fontStyle = FontStyle.Bold });
    }
  }
}
#endif
