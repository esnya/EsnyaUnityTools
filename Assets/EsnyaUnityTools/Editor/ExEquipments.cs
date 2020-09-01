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

  public class ExEquipmentsa : EditorWindow {
    [MenuItem("EsnyaTools/ExEquipments")]
    public static void Open() {
      var w = GetWindow<ExEquipmentsa>();
      w.titleContent = new GUIContent("ExEquipments");
      w.Show();
    }

    private VRCAvatarDescriptor avatar;
    private VRCExpressionParameters expressionParameters;
    private AnimatorController controller;
    private GameObject item;
    private List<Transform> targets = new List<Transform>(){ null };
    private bool drop = false;
    private DefaultAsset outDir;

    private int dropIndex {
      get {
        return targets.Count;
      }
    }

    private void OnGUI() {
      avatar = EEU.ObjectField<VRCAvatarDescriptor>("Avatar", avatar, true);
      expressionParameters = EEU.ObjectField<VRCExpressionParameters>("Expression Parameters", expressionParameters, false);
      controller = EEU.ObjectField<AnimatorController>("Controller", controller, false);

      item = EEU.ObjectField<GameObject>("Item", item, true);

      targets = targets.Select((target, i) => EEU.ObjectField<Transform>($"Target {i}", target, true)).ToList();

      EEU.Horizontal(() => {
        EEU.Button("+", () => targets.Add(null));
        EEU.Button("-", () => targets = targets.Take(targets.Count - 1).ToList());
      });

      drop = EditorGUILayout.Toggle("Drop", drop);

      outDir = EEU.AssetDirectoryField("Output Directory", outDir);


      EEU.Button("Setup", Setup);
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Behaviour {
      var component = gameObject.GetComponent<T>();
      if (component != null) return component;
      return gameObject.AddComponent<T>();
    }

    private static GameObject GetOrAddChild(GameObject gameObject, string name) {
      var child = gameObject.transform.Find(name);
      if (child != null) return child.gameObject;

      child = new GameObject(name).transform;
      child.SetParent(gameObject.transform);
      return child.gameObject;
    }

    private static AnimatorControllerLayer GetOrAddLayer(AnimatorController controller, string name) {
      var layer = controller.layers.FirstOrDefault(l => l.name == name);
      if (layer != null) return layer;
      controller.AddLayer(name);
      return GetOrAddLayer(controller, name);
    }

    private static void AddParameterIfNotExists(AnimatorController controller, string name, AnimatorControllerParameterType type) {
      if (controller.parameters.FirstOrDefault(p => p.name == name) == null) {
        controller.AddParameter(name, type);
      }
    }

    private static string GetHierarchyPath(Transform target, Transform root)
    {
      string path = target.gameObject.name;
      Transform parent = target.parent;
      while (parent != null && parent != root)
      {
        path = parent.name + "/" + path;
        parent = parent.parent;
      }
      return path;
    }

    private T GetOrCreateAsset<T>(string path, bool scriptable = false) where T : Object, new () {
      var fullPath = $"{AssetDatabase.GetAssetPath(outDir)}/{path}";
      var asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
      if (asset != null) return asset;

      asset = scriptable ? ScriptableObject.CreateInstance(typeof(T)) as T : new T();
      AssetDatabase.CreateAsset(asset, fullPath);
      return asset;
    }

    private ParentConstraint SetupParentConstraint() {
      var parentConstraint = GetOrAddComponent<ParentConstraint>(item);
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

    private string GetAnimationAssetPath(Transform target) {
      return $"{item.name}_{target.name.Replace($"{item.name}_", "")}.anim";
    }

    private List<AnimationClip> SetupClips() {
      return targets.Select((target, i) => {
        var clip = GetOrCreateAsset<AnimationClip>(GetAnimationAssetPath(target));
        clip.ClearCurves();
        Enumerable.Range(0, targets.Count + (drop ? 1 : 0)).ToList().ForEach(j => {
          clip.SetCurve(GetHierarchyPath(item.transform, avatar.transform), typeof(ParentConstraint), $"m_Sources.Array.data[{j}].weight", new AnimationCurve(new [] {
            new Keyframe(0, j == i ? 1 : 0),
          }));
        });
        return clip;
      }).ToList();
    }

    private AnimatorControllerLayer SetupLayer(List<AnimationClip> clips) {
      AddParameterIfNotExists(controller, item.name, AnimatorControllerParameterType.Int);

      var layer = GetOrAddLayer(controller, item.name);
      layer.defaultWeight = 1.0f;
      layer.stateMachine.states.ToList().ForEach(state => layer.stateMachine.RemoveState(state.state));
      EditorUtility.SetDirty(controller);


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

      return layer;
    }


    private void SetupDrop(ParentConstraint parentConstraint, AnimatorControllerLayer layer) {
        var worldAnchor = GetOrAddChild(avatar.gameObject, "WorldAnchor");
        var worldConstraint = GetOrAddChild(worldAnchor, "WorldConstraint");

        var rotationConstraint = GetOrAddComponent<RotationConstraint>(worldConstraint);
        rotationConstraint.enabled = true;
        rotationConstraint.constraintActive = true;
        rotationConstraint.locked = true;
        rotationConstraint.weight = 1.0f;
        rotationConstraint.SetSources(new List<ConstraintSource>() { new ConstraintSource() { sourceTransform = worldAnchor.transform, weight = -0.5f } });

        var positionConstraint = GetOrAddComponent<PositionConstraint>(worldConstraint);
        positionConstraint.enabled = true;
        positionConstraint.constraintActive = true;
        positionConstraint.locked = true;
        positionConstraint.weight = 0.5f;
        positionConstraint.SetSources(new List<ConstraintSource>() { new ConstraintSource() { sourceTransform = worldAnchor.transform, weight = -1.0f }});

        var dropTarget = GetOrAddChild(worldConstraint, $"{item.name}_Drop");

        parentConstraint.AddSource(new ConstraintSource() { sourceTransform = dropTarget.transform, weight = 0.0f });

        var dropTargetParentConstraint = GetOrAddComponent<ParentConstraint>(dropTarget);
        dropTargetParentConstraint.enabled = false;
        dropTargetParentConstraint.constraintActive = true;
        dropTargetParentConstraint.locked = true;
        dropTargetParentConstraint.weight = 1.0f;
        dropTargetParentConstraint.SetSources(new List<ConstraintSource>() { new ConstraintSource() { sourceTransform = item.transform, weight = 1.0f }});

        targets.ForEach(target => {
          var clip = GetOrCreateAsset<AnimationClip>(GetAnimationAssetPath(target));
          clip.SetCurve(GetHierarchyPath(dropTarget.transform, avatar.transform), typeof(ParentConstraint), "m_Enabled", new AnimationCurve(new [] {
            new Keyframe(0, 1),
          }));
        });

        var clipPath = $"{item.name}_Drop.anim";
        var dropClip = GetOrCreateAsset<AnimationClip>(clipPath);
        dropClip.ClearCurves();

        dropClip.SetCurve(GetHierarchyPath(dropTarget.transform, avatar.transform), typeof(ParentConstraint), "m_Enabled", new AnimationCurve(new [] {
          new Keyframe(0, 0),
        }));
        EditorUtility.SetDirty(dropClip);

        dropClip.SetCurve(GetHierarchyPath(item.transform, avatar.transform), typeof(ParentConstraint), $"m_Sources.Array.data[{dropIndex}].weight", new AnimationCurve(new [] {
          new Keyframe(0, 1),
        }));

        targets.Select((target, i) => i).ToList().ForEach(i => {
          dropClip.SetCurve(GetHierarchyPath(item.transform, avatar.transform), typeof(ParentConstraint), $"m_Sources.Array.data[{i}].weight", new AnimationCurve(new [] {
            new Keyframe(0, 0),
          }));
        });

        EditorUtility.SetDirty(dropClip);

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
    }

    private void Setup() {
      var parentConstraint = SetupParentConstraint();
      var clips = SetupClips();
      var layer = SetupLayer(clips);

      if (drop) {
        SetupDrop(parentConstraint, layer);
      }

      SetupParameters();
      SetupMenu();

      AssetDatabase.Refresh();
    }
  }
}
#endif
