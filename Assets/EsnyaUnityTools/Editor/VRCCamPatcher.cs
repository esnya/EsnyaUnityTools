namespace EsnyaFactory {
  using System.Linq;
  using UnityEditor;
  using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
  using UnityEngine.Rendering.PostProcessing;
#endif

  public class VRCCamPatcher : EditorWindow {

    [MenuItem("EsnyaTools/Fix VRCCam")]
    private static void ShowWindow() {
      var window = GetWindow<VRCCamPatcher>();
      window.Show();
    }

    private Camera camera;
    private bool postProcessing = true;
    private bool backgroundColor = false;
    private Color color = Color.gray;
    private bool physicalCamera = true;
    private Vector2 sensorSize = new Vector2(36.0f, 24.0f);
    private float focalLength = 50.0f;


    private void OnEnable()
    {
      titleContent = new GUIContent("VRC Cam Patcher");

      camera = AssetDatabase.FindAssets("t:GameObject VRCCam")
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
        .Select(o => o.GetComponent<Camera>())
        .Where(c => c != null)
        .FirstOrDefault();
    }

    private void OnGUI() {
      camera = EEU.ObjectField<Camera>("VRC Cam", camera, false);

      EditorGUILayout.Space();
      EEU.Disabled(camera == null, () => {
#if UNITY_POST_PROCESSING_STACK_V2
        postProcessing = EditorGUILayout.Toggle("Post Processing", postProcessing);
#endif

        EditorGUILayout.Space();

        backgroundColor = EditorGUILayout.Toggle("Background Color", backgroundColor);
        if (backgroundColor) {
          color = EditorGUILayout.ColorField(color);
        }

        EditorGUILayout.Space();

        physicalCamera = EditorGUILayout.Toggle("Physical Camera", physicalCamera);
        if (physicalCamera) {
          focalLength = EditorGUILayout.FloatField("Focal Length", focalLength);
          sensorSize = EditorGUILayout.Vector2Field("Sensor Size", sensorSize);
        }

        EditorGUILayout.Space();

        EEU.Box("Presets", () => {
          EEU.Horizontal(() => {
            EEU.Button("Default", () => {
              postProcessing = false;
              backgroundColor = false;
              physicalCamera = false;
            });
            EEU.Button("Avatar", () => {
              postProcessing = true;
              backgroundColor = true;
              physicalCamera = true;
            });
            EEU.Button("World", () => {
              postProcessing = true;
              backgroundColor = false;
              physicalCamera = true;
            });
          });
        });

        EditorGUILayout.Space();

        EEU.Button("Patch", Patch);
      });
    }

    private void Patch() {
      SetupPostProcessingLayer();
      SetupBackgroundColor();
      SetupPhyisical();
      AssetDatabase.Refresh();
    }

    private void SetupPostProcessingLayer() {
#if UNITY_POST_PROCESSING_STACK_V2
      var layer = camera.GetComponent<PostProcessLayer>();

      if (layer == null) {
        layer = camera.gameObject.AddComponent<PostProcessLayer>();
      } else if (!postProcessing) {
        DestroyImmediate(layer, true);
        return;
      }

      layer.volumeTrigger = camera.transform;
      layer.volumeLayer = LayerMask.GetMask(new []{"Everything"});
#endif
    }

    private void SetupBackgroundColor() {
      if (backgroundColor) {
        camera.clearFlags = CameraClearFlags.Color;
        camera.backgroundColor = color;
      } else {
        camera.clearFlags = CameraClearFlags.Skybox;
      }
    }

    private void SetupPhyisical() {
      if (physicalCamera) {
        camera.usePhysicalProperties = true;
        camera.focalLength = focalLength;
        camera.sensorSize = sensorSize;
      } else {
        camera.usePhysicalProperties = false;
      }
    }
  }
}
