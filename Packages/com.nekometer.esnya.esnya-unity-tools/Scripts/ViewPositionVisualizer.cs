#if VRC_SDK_VRCSDK3 && !UDON
namespace EsnyaFactory
{
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    [ExecuteInEditMode]
    public class ViewPositionVisualizer : MonoBehaviour {
        public VRCAvatarDescriptor avatar;
        [Range(0.0f, 1.0f)] public float upright = 1.0f;
        [HideInInspector] public Vector3 headLocalViewPosition = Vector3.zero;

        public Animator animator {
            get {
                return avatar?.GetComponent<Animator>();
            }
        }

        public Transform head {
            get {
                return animator.GetBoneTransform(HumanBodyBones.Head);
            }
        }

        public Vector3 globalViewPosition {
            get {
                return avatar.ViewPosition + avatar.transform.position;
            }
        }

        public Vector3 currentViewPosition {
            get {
                return head.position + head.rotation * headLocalViewPosition;
            }
        }

        public Vector3 targetViewPosition {
            get {
                return Vector3.Scale(avatar.ViewPosition, new Vector3(1, upright, 1)) + avatar.transform.position;
            }
        }
        public Vector3 viewPositionDiff {
            get {
                return targetViewPosition - currentViewPosition;
            }
        }

        private void OnDrawGizmos() {
            if (avatar == null || headLocalViewPosition == Vector3.zero) return;
            var viewPosition = avatar.ViewPosition;

            Gizmos.color = new Color(1, 0, 0, 1);

            Gizmos.DrawWireSphere(globalViewPosition, 0.01f);
            Gizmos.DrawWireSphere(Vector3.Scale(new Vector3(1, 0.65f, 1), viewPosition) + avatar.transform.position, 0.01f);
            Gizmos.DrawWireSphere(Vector3.Scale(new Vector3(1, 0.28f, 1), viewPosition) + avatar.transform.position, 0.01f);

            Gizmos.DrawLine(new Vector3(viewPosition.x, -1, viewPosition.z) + avatar.transform.position, viewPosition + avatar.transform.position + Vector3.up);

            Gizmos.color = new Color(1, 1, 1, 1);
            Gizmos.DrawWireSphere(currentViewPosition, 0.01f);

            Gizmos.color = new Color(0, 1, 0, 1);
            Gizmos.DrawWireSphere(targetViewPosition, 0.01f);
        }

        private void Update() {
            if (avatar == null) return;
            if (headLocalViewPosition == Vector3.zero) {
                headLocalViewPosition = Quaternion.Inverse(head.rotation) * (avatar.ViewPosition - head.position);
            }
        }

        public static ViewPositionVisualizer AddVisualizerObject(VRCAvatarDescriptor avatar = null) {
            var container = new GameObject(avatar != null ? $"{avatar.gameObject.name}_ViewPosition" : "ViewPositionVisualizer");
            var visualizer = container.AddComponent<ViewPositionVisualizer>();
            visualizer.avatar = avatar;
            return visualizer;
        }
    }
}
#endif
