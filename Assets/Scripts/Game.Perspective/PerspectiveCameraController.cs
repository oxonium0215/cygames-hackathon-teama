using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Game.Perspective
{
    /// <summary>
    /// Handles camera rotation and positioning during perspective switches.
    /// Separated from projection management for cleaner role division.
    /// </summary>
    [MovedFrom("POC.GameplayProjection")]
    public class PerspectiveCameraController : MonoBehaviour
    {
        [Header("Camera Setup")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float cameraDistance = 10f;
        [SerializeField] private Vector3 pivotOffset = Vector3.zero;

        [Header("Views")]
        [SerializeField] private float viewAYaw = 0f;
        [SerializeField] private float viewBYaw = 90f;

        [Header("Rotation Animation")]
        [SerializeField] private float rotateDuration = 0.3f;
        [SerializeField] private AnimationCurve rotateEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Transform rotationCenter;
        private int currentViewIndex = 0;

        public Transform CameraPivot => cameraPivot;
        public int CurrentViewIndex => currentViewIndex;
        public bool IsRotating { get; private set; }

        public void Initialize(Transform center)
        {
            rotationCenter = center;
            RepositionPivotToCenter();
            ApplyViewImmediate(currentViewIndex);
        }

        public void SetViewImmediate(int viewIndex)
        {
            currentViewIndex = viewIndex;
            ApplyViewImmediate(viewIndex);
        }

        public System.Collections.IEnumerator RotateToView(int targetViewIndex)
        {
            if (IsRotating) yield break;

            IsRotating = true;
            currentViewIndex = targetViewIndex;

            RepositionPivotToCenter();

            float startYaw = cameraPivot.eulerAngles.y;
            float targetYaw = (targetViewIndex == 0) ? viewAYaw : viewBYaw;
            float deltaYaw = Mathf.DeltaAngle(startYaw, targetYaw);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.0001f, rotateDuration);
                float s = rotateEase.Evaluate(Mathf.Clamp01(t));

                var eul = cameraPivot.eulerAngles;
                eul.y = Mathf.LerpAngle(startYaw, startYaw + deltaYaw, s);
                cameraPivot.eulerAngles = eul;

                yield return null;
            }

            IsRotating = false;
        }

        private void ApplyViewImmediate(int viewIndex)
        {
            RepositionPivotToCenter();

            var eul = cameraPivot.eulerAngles;
            eul.y = (viewIndex == 0) ? viewAYaw : viewBYaw;
            cameraPivot.eulerAngles = eul;
        }

        private void RepositionPivotToCenter()
        {
            if (!cameraPivot) return;

            Vector3 center = rotationCenter ? rotationCenter.position : cameraPivot.position - pivotOffset;
            Vector3 target = center + pivotOffset;

            // Do not scroll down: preserve current (higher) Y if applicable.
            target.y = Mathf.Max(target.y, cameraPivot.position.y);

            cameraPivot.position = target;

            if (cameraPivot.childCount > 0)
            {
                var cam = cameraPivot.GetChild(0);
                cam.localPosition = new Vector3(0f, 0f, -Mathf.Abs(cameraDistance));
                cam.localRotation = Quaternion.identity;
            }
        }
    }
}