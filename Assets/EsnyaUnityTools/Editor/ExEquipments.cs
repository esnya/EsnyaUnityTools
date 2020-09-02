#if VRC_SDK_VRCSDK3 && !UDON
namespace EsnyaFactory {
  using System.Collections.Generic;
  using System.Linq;
  using UnityEngine;
  using UnityEngine.Animations;
  using UnityEditor;
  using UnityEditor.Animations;
  using VRC.SDK3.Avatars.Components;
  using VRC.SDK3.Avatars.ScriptableObjects;

  public class ExEquipments : EditorWindow {
    [MenuItem("EsnyaTools/ExEquipments")]
    public static void Open() {
      var w = GetWindow<ExEquipments>();
      w.titleContent = new GUIContent("ExEquipments");
      w.Show();
    }

    private Vector2 scroll;
    private VRCAvatarDescriptor avatar;
    private VRCExpressionParameters expressionParameters;
    private AnimatorController controller;
    private GameObject item;
    private List<Transform> targets = new List<Transform>(){ null };
    private bool drop = false;
    private DefaultAsset outDirAsset;
    private string outDir {
      get {
        return outDirAsset != null ? AssetDatabase.GetAssetPath(outDirAsset) : null;
      }
    }

    private int dropIndex {
      get {
        return targets.Count;
      }
    }

    private bool isValid {
      get {
        return avatar != null && expressionParameters != null && controller != null && item != null && targets.All(t => t != null) && AssetDatabase.IsValidFolder(outDir);
      }
    }

    private void OnGUI() {
      scroll = EEU.Scroll(scroll, () => {
        EditorGUILayout.Space();

        item = EEU.ObjectField<GameObject>("Target Item", item, true);

        EditorGUILayout.Space();

        EEU.Box("Avatar Resources", () => {
          avatar = EEU.ObjectField<VRCAvatarDescriptor>("Avatar", avatar, true);
          expressionParameters = EEU.ObjectField<VRCExpressionParameters>("Expression Parameters", expressionParameters, false);
          controller = EEU.ObjectField<AnimatorController>("FX Layer", controller, false);
        });

        EditorGUILayout.Space();

        EEU.Box("Target Transforms", () => {
          targets = targets.Select((target, i) => EEU.ObjectField<Transform>($"Target {i}", target, true)).ToList();
          EEU.Horizontal(() => {
            EEU.Button("+", () => targets.Add(null));
            EEU.Button("-", () => targets = targets.Take(targets.Count - 1).ToList());
          });
        });

        EditorGUILayout.Space();

        EEU.Box("Extra Actions", () => {
          drop = EditorGUILayout.Toggle("Drop", drop);
        });

        EditorGUILayout.Space();

        outDirAsset = EEU.AssetDirectoryField("Output Directory", outDirAsset);

        EditorGUILayout.Space();

        EEU.Disabled(!isValid, () => {
          EEU.Button("Setup", Setup);
        });

        EditorGUILayout.Space();
      });
    }

    private ParentConstraint SetupParentConstraint() {
      var parentConstraint = GameObjectUtility.GetOrAddComponent<ParentConstraint>(item);
      parentConstraint.enabled = true;
      parentConstraint.constraintActive = true;
      parentConstraint.locked = true;
      parentConstraint.weight = 1.0f;
      parentConstraint.SetSources(targets.Select((target, i) => new ConstraintSource() {
        sourceTransform = target.transform,
        weight = 0.0f,
      }).ToList());
      return parentConstraint;
    }

    private T GetOrCreateAsset<T>(string path, bool scriptable = false) where T : Object, new () {
      var fullPath = $"{outDir}/{path}";
      return AssetUtility.GetOrCreateAsset<T>(fullPath, scriptable);
    }

    private string GetAnimationAssetPath(Transform target) {
      return $"{item.name}_{target.name.Replace($"{item.name}_", "")}.anim";
    }

    private List<AnimationClip> SetupClips() {
      return targets.Select((target, i) => {
        var clip = GetOrCreateAsset<AnimationClip>(GetAnimationAssetPath(target));
        clip.ClearCurves();
        Enumerable.Range(0, targets.Count + (drop ? 1 : 0)).ToList().ForEach(j => {
          clip.SetCurve(GameObjectUtility.GetHierarchyPath(item.transform, avatar.transform), typeof(ParentConstraint), $"m_Sources.Array.data[{j}].weight", new AnimationCurve(new [] {
            new Keyframe(0, j == i ? 1 : 0),
          }));
        });
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        return clip;
      }).ToList();
    }

    private AnimatorControllerLayer SetupLayer(List<AnimationClip> clips) {
      EsnyaFactory.AnimatorUtility.AddParameterIfNotExists(controller, item.name, AnimatorControllerParameterType.Int);

      var layer = EsnyaFactory.AnimatorUtility.GetOrAddLayer(controller, item.name);
      layer.defaultWeight = 1.0f;
      layer.stateMachine.states.ToList().ForEach(state => layer.stateMachine.RemoveState(state.state));

      clips.Select((clip, i) => new { clip, i }).ToList().ForEach(a => {
        var state = layer.stateMachine.AddState(a.clip.name, new Vector3(400, 100 * a.i, 0));
        state.motion = a.clip;
        state.writeDefaultValues = true;

        var transition = layer.stateMachine.AddAnyStateTransition(state);
        transition.canTransitionToSelf = false;
        transition.conditions = new [] {
          new AnimatorCondition() {
            mode = AnimatorConditionMode.Equals,
            parameter = item.name,
            threshold = a.i,
          },
        };
        transition.duration = 0.25f;
        transition.hasExitTime = false;
      });

      EditorUtility.SetDirty(controller);
      AssetDatabase.SaveAssets();

      return layer;
    }


    private void SetupDrop(ParentConstraint parentConstraint, AnimatorControllerLayer layer) {
        var worldAnchor = GameObjectUtility.GetOrAddChild(avatar.gameObject, "WorldAnchor");
        var worldConstraint = GameObjectUtility.GetOrAddChild(worldAnchor, "WorldConstraint");

        var rotationConstraint = GameObjectUtility.GetOrAddComponent<RotationConstraint>(worldConstraint);
        rotationConstraint.enabled = true;
        rotationConstraint.constraintActive = true;
        rotationConstraint.locked = true;
        rotationConstraint.weight = 1.0f;
        rotationConstraint.SetSources(new List<ConstraintSource>() { new ConstraintSource() { sourceTransform = worldAnchor.transform, weight = -0.5f } });

        var positionConstraint = GameObjectUtility.GetOrAddComponent<PositionConstraint>(worldConstraint);
        positionConstraint.enabled = true;
        positionConstraint.constraintActive = true;
        positionConstraint.locked = true;
        positionConstraint.weight = 0.5f;
        positionConstraint.SetSources(new List<ConstraintSource>() { new ConstraintSource() { sourceTransform = worldAnchor.transform, weight = -1.0f }});

        var dropTarget = GameObjectUtility.GetOrAddChild(worldConstraint, $"{item.name}_Drop");

        parentConstraint.AddSource(new ConstraintSource() { sourceTransform = dropTarget.transform, weight = 0.0f });

        var dropTargetParentConstraint = GameObjectUtility.GetOrAddComponent<ParentConstraint>(dropTarget);
        dropTargetParentConstraint.enabled = false;
        dropTargetParentConstraint.constraintActive = true;
        dropTargetParentConstraint.locked = true;
        dropTargetParentConstraint.weight = 1.0f;
        dropTargetParentConstraint.SetSources(new List<ConstraintSource>() { new ConstraintSource() { sourceTransform = item.transform, weight = 1.0f }});

        targets.ForEach(target => {
          var clip = GetOrCreateAsset<AnimationClip>(GetAnimationAssetPath(target));
          clip.SetCurve(GameObjectUtility.GetHierarchyPath(dropTarget.transform, avatar.transform), typeof(ParentConstraint), "m_Enabled", new AnimationCurve(new [] {
            new Keyframe(0, 1),
          }));
          EditorUtility.SetDirty(clip);
        });

        var clipPath = $"{item.name}_Drop.anim";
        var dropClip = GetOrCreateAsset<AnimationClip>(clipPath);
        dropClip.ClearCurves();

        dropClip.SetCurve(GameObjectUtility.GetHierarchyPath(dropTarget.transform, avatar.transform), typeof(ParentConstraint), "m_Enabled", new AnimationCurve(new [] {
          new Keyframe(0, 0),
        }));
        dropClip.SetCurve(GameObjectUtility.GetHierarchyPath(item.transform, avatar.transform), typeof(ParentConstraint), $"m_Sources.Array.data[{dropIndex}].weight", new AnimationCurve(new [] {
          new Keyframe(0, 1),
        }));

        targets.Select((target, i) => i).ToList().ForEach(i => {
          dropClip.SetCurve(GameObjectUtility.GetHierarchyPath(item.transform, avatar.transform), typeof(ParentConstraint), $"m_Sources.Array.data[{i}].weight", new AnimationCurve(new [] {
            new Keyframe(0, 0),
          }));
        });

        EditorUtility.SetDirty(dropClip);
        AssetDatabase.SaveAssets();

        var state = layer.stateMachine.AddState("Drop", new Vector3(400, 100 * targets.Count, 0));
        state.motion = dropClip;
        state.writeDefaultValues = true;

        var transition = layer.stateMachine.AddAnyStateTransition(state);
        transition.canTransitionToSelf = false;
        transition.conditions = new [] {
          new AnimatorCondition() {
            mode = AnimatorConditionMode.Equals,
            parameter = item.name,
            threshold = dropIndex,
          },
        };

        transition.duration = 0.5f / 60;
        transition.hasExitTime = false;
    }

    private void SetupParameters() {
      if (expressionParameters.FindParameter(item.name) != null) return;
      var emptyIndex = expressionParameters.parameters.Select((p, i) => new { p, i }).First(a => string.IsNullOrEmpty(a.p.name)).i;
      expressionParameters.parameters[emptyIndex] = new VRCExpressionParameters.Parameter() {
        name = item.name,
        valueType = VRCExpressionParameters.ValueType.Int,
      };
      EditorUtility.SetDirty(expressionParameters);
      AssetDatabase.SaveAssets();
    }

    private void SetupMenu() {
      var menu = GetOrCreateAsset<VRCExpressionsMenu>($"ExEquipments_{item.name}.asset", true);
      menu.name = item.name;

      var controls = targets.Select((target, i) => new VRCExpressionsMenu.Control() {
        name = target.name.Replace($"{item.name}_", ""),
        type = VRCExpressionsMenu.Control.ControlType.Toggle,
        parameter = new VRCExpressionsMenu.Control.Parameter() {
          name = item.name,
        },
        value = i,
      });

      menu.controls = (drop ? controls.Prepend(new VRCExpressionsMenu.Control() {
        name = "Drop",
        type = VRCExpressionsMenu.Control.ControlType.Toggle,
        parameter = new VRCExpressionsMenu.Control.Parameter() {
          name = item.name,
        },
        value = targets.Count,
      }) : controls).ToList();

      EditorUtility.SetDirty(menu);
      AssetDatabase.SaveAssets();
    }

    private void Setup() {
      uint totalSetps = 7;
      uint step = 0;

      EditorUtility.DisplayProgressBar("ExEquipments", "Setup in progress", (float)(step++) / totalSetps);

      EditorUtility.DisplayProgressBar("ExEquipments", "ParentConstraint", (float)(step++) / totalSetps);
      var parentConstraint = SetupParentConstraint();

      EditorUtility.DisplayProgressBar("ExEquipments", "Animation clips", (float)(step++) / totalSetps);
      var clips = SetupClips();

      EditorUtility.DisplayProgressBar("ExEquipments", "Animation controller layer", (float)(step++) / totalSetps);
      var layer = SetupLayer(clips);

      EditorUtility.DisplayProgressBar("ExEquipments", "Extra: Drop", (float)(step++) / totalSetps);
      if (drop) {
        SetupDrop(parentConstraint, layer);
      }


      EditorUtility.DisplayProgressBar("ExEquipments", "Expression parameters", (float)(step++) / totalSetps);
      SetupParameters();

      EditorUtility.DisplayProgressBar("ExEquipments", "Expressions menu", (float)(step++) / totalSetps);
      SetupMenu();

      EditorUtility.DisplayProgressBar("ExEquipments", "Finalize", (float)(step++) / totalSetps);
      AssetDatabase.Refresh();
      EditorUtility.ClearProgressBar();
    }
  }
}
#endif
