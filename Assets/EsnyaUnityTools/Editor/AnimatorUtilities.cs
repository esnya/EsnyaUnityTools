namespace EsnyaFactory {
  using UnityEngine;
  using UnityEditor;
  using UnityEditor.Animations;
  public class AnimatorUtilities {
    public static void AddLayer(AnimatorController controller, AnimatorControllerLayer newLayer) {
      newLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;

      var controllerPath = AssetDatabase.GetAssetPath(controller);
      if (!string.IsNullOrEmpty(controllerPath)) {
        AssetDatabase.AddObjectToAsset(newLayer.stateMachine, controllerPath);
      }

      controller.AddLayer(newLayer);
    }
  }
}
