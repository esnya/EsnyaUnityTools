namespace EsnyaFactory {
  using System.Linq;
  using UnityEngine;
  using UnityEditor;
  using UnityEditor.Animations;

  public class AnimatorUtility {
    public static void AddParameterIfNotExists(AnimatorController controller, string name, AnimatorControllerParameterType type) {
      if (controller.parameters.FirstOrDefault(p => p.name == name) == null) {
        controller.AddParameter(name, type);
      }
    }

    public static void AddLayer(AnimatorController controller, AnimatorControllerLayer newLayer) {
      newLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;

      var controllerPath = AssetDatabase.GetAssetPath(controller);
      if (!string.IsNullOrEmpty(controllerPath)) {
        AssetDatabase.AddObjectToAsset(newLayer.stateMachine, controllerPath);
      }

      controller.AddLayer(newLayer);
    }

    public static AnimatorControllerLayer GetOrAddLayer(AnimatorController controller, string name) {
      var layer = controller.layers.FirstOrDefault(l => l.name == name);
      if (layer != null) return layer;
      layer = new AnimatorControllerLayer();
      layer.name = name;
      layer.defaultWeight = 1.0f;
      layer.stateMachine = new AnimatorStateMachine();
      AddLayer(controller, layer);
      return layer;
    }
  }
}
