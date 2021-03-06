﻿#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace EsnyaFactory
{
  public class AnimationReplacer : EditorWindow
  {
    [MenuItem("EsnyaTools/Animation Replacer")]
    [MenuItem("Assets/EsnyaTools/Animation Replacer")]
    private static AnimationReplacer ShowWindow()
    {
      var window = GetWindow<AnimationReplacer>();
      window.Show();
      return window;
    }

    AnimatorController controller;
    AnimatorController prevController;
    KeyValuePair<Motion, Motion>[] motionTable;

    private IEnumerable<Motion> GetMotions(AnimatorStateMachine stateMachine)
    {
      return stateMachine.stateMachines
          .SelectMany(sub => GetMotions(sub.stateMachine))
          .Concat(stateMachine.states.Select(state => state.state.motion));
    }

    private IEnumerable<KeyValuePair<Motion, Motion>> GetMotionTable(AnimatorController controller)
    {
      return controller.layers
          .SelectMany(layer => GetMotions(layer.stateMachine))
          //.Where(motion => motion != null)
          .Distinct()
        .OrderBy(motion => motion?.name)
          .Select(motion => new KeyValuePair<Motion, Motion>(motion, null));
    }

    private void ReplaceMotions(AnimatorStateMachine stateMachine, IEnumerable<KeyValuePair<Motion, Motion>> motionDict)
    {
      stateMachine.states
        .Select(state => new KeyValuePair<AnimatorState, Motion>(state.state, motionDict.FirstOrDefault(p => p.Key == state.state.motion).Value))
        .Where(p => p.Value != null)
            .ToList()
        .ForEach(p =>
        {
          var state = p.Key;
          var prev = state.motion;
          var next = p.Value;
          Debug.Log($"Repling {prev?.name} by {next.name}");
          state.motion = next;
        });
      stateMachine.stateMachines.ToList().ForEach(subStateMachine => ReplaceMotions(subStateMachine.stateMachine, motionDict));
    }

    private void ExecuteReplace(AnimatorController controller, KeyValuePair<Motion, Motion>[] motionTable)
    {
      var motionDict = motionTable
        .Where(pair => pair.Value != null)
        .ToList();
      controller.layers.ToList().ForEach(layer =>
      {
        Debug.Log($"Replacing layer {layer.name}");
        ReplaceMotions(layer.stateMachine, motionDict);
      });
    }

    private void OnGUI()
    {
      titleContent = new GUIContent("Animation Replacer");

      controller = EditorGUILayout.ObjectField("Animation Controller", controller, typeof(AnimatorController), false) as AnimatorController;
      if (controller != null && (controller != prevController || motionTable == null))
      {
        motionTable = GetMotionTable(controller).ToList().ToArray();
        prevController = controller;
      }
      if (controller == null)
      {
        prevController = null;
      }


      EditorGUI.BeginDisabledGroup(controller == null);
      if (GUILayout.Button("Refresh List"))
      {
        prevController = null;
      }

      EditorGUILayout.Space();

      EditorGUI.EndDisabledGroup();
      if (motionTable != null)
      {
        motionTable = motionTable
            .Select(motionMap => new KeyValuePair<Motion, Motion>(
                motionMap.Key,
          EditorGUILayout.ObjectField(motionMap.Key?.name ?? "None (Motion)", motionMap.Value, typeof(Motion), false) as Motion
            ))
            .ToArray();
      }

      EditorGUILayout.Space();

      var hasChange = motionTable != null && motionTable.Any(motionMap => motionMap.Value != null);
      EditorGUI.BeginDisabledGroup(!hasChange || controller == null);
      if (GUILayout.Button("Replace"))
      {
        ExecuteReplace(controller, motionTable);
        prevController = null;
      }
      EditorGUI.EndDisabledGroup();
    }
  }
}
#endif
