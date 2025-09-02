using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Input;

namespace Game.Gimmicks
{
    public class SwitchController : MonoBehaviour
    {
        [SerializeField] private List<Transform> blockGroups = new();
        [SerializeField, Min(0.1f)] private float duration = 2f;
        [SerializeField] private string activatorTag = "Player";
        [SerializeField] private Transform buttonTop;
        [SerializeField] private float pressDepth = 0.10f;
        [SerializeField] private float pressAnimTime = 0.08f;
        [SerializeField] private AnimationCurve pressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private Collider pressSensor;
        [SerializeField] private PlayerInputRelay _playerInputRelay;
        [SerializeField] private Renderer[] renderersToToggle;
        [SerializeField] private Collider[] collidersToToggle;
        private readonly List<GameObject> cachedBlocks = new();
        private Coroutine activeRoutine;
        private bool isActiveRoutineRunning = false;
        private bool visualPressed = false;
        private Vector3 topInitialLocalPos;
        private Vector3 topDownLocalPos;
        private int sensorStayCount = 0;
        private bool prevPreview = false;
        private void Awake()
        {
            CacheBlocksFromGroups();
            SetBlocksActive(false);

            if (!buttonTop)
            {
                var child = transform.Find("ButtonTop");
                if (child) buttonTop = child;
            }
            if (!pressSensor)
            {
                var box = GetComponent<BoxCollider>();
                if (box && box.isTrigger) pressSensor = box;
            }
            if (buttonTop)
            {
                topInitialLocalPos = buttonTop.localPosition;
                topDownLocalPos = topInitialLocalPos + Vector3.down * pressDepth;
            }
            if (renderersToToggle == null || renderersToToggle.Length == 0)
                renderersToToggle = GetComponentsInChildren<Renderer>(true);
            if (collidersToToggle == null || collidersToToggle.Length == 0)
                collidersToToggle = GetComponentsInChildren<Collider>(true);

            ExcludeChildrenOfButtonTopFromLegacyToggles();
            SyncButtonVisualImmediate();
        }

        private void OnEnable()
        {
            SyncButtonVisualImmediate();
            prevPreview = IsPreviewNow();
        }

        private void LateUpdate()
        {
            bool nowPreview = IsPreviewNow();
            if (nowPreview != prevPreview)
            {
                SyncButtonVisualImmediate();
                prevPreview = nowPreview;
            }
            UpdatePressSensorStayCount();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.collider.CompareTag(activatorTag)) return;

            foreach (var cp in collision.contacts)
            {
                if (Vector3.Dot(cp.normal, Vector3.down) > 0.8f)
                {
                    TryActivate();
                    break;
                }
            }
        }

        private void TryActivate()
        {
            if (isActiveRoutineRunning) return;

            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(ActivateBlocksRoutine());
        }

        private IEnumerator ActivateBlocksRoutine()
        {
            isActiveRoutineRunning = true;

            yield return PressDown();
            SetBlocksActive(true);

            float expire = Time.unscaledTime + duration;
            const float releaseGrace = 0.08f;
            float lastPressedTime = (IsPressedByActivator() || IsPreviewNow()) ? Time.unscaledTime : -999f;

            while (true)
            {
                if (IsPressedByActivator() || IsPreviewNow())
                {
                    expire = Time.unscaledTime + duration;
                    lastPressedTime = Time.unscaledTime;
                }

                bool bufferedPressed = (Time.unscaledTime - lastPressedTime) <= releaseGrace;
                if (bufferedPressed && expire < Time.unscaledTime + duration)
                {
                    expire = Time.unscaledTime + duration;
                }

                if (Time.unscaledTime >= expire) break;
                yield return null;
            }

            SetBlocksActive(false);
            yield return PressUp();
            isActiveRoutineRunning = false;
        }

        private void SyncButtonVisualImmediate()
        {
            if (!buttonTop) return;
            buttonTop.localPosition = visualPressed ? topDownLocalPos : topInitialLocalPos;
        }

        private IEnumerator PressDown()
        {
            if (!buttonTop) yield break;
            if (visualPressed) { SyncButtonVisualImmediate(); yield break; }

            var from = buttonTop.localPosition;
            var to = topDownLocalPos;
            float t = 0f;
            float denom = Mathf.Max(pressAnimTime, 0.001f);

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / denom;
                float k = pressCurve.Evaluate(Mathf.Clamp01(t));
                buttonTop.localPosition = Vector3.LerpUnclamped(from, to, k);
                yield return null;
            }
            buttonTop.localPosition = to;
            visualPressed = true;
        }

        private IEnumerator PressUp()
        {
            if (!buttonTop) yield break;
            if (!visualPressed) { SyncButtonVisualImmediate(); yield break; }

            var from = buttonTop.localPosition;
            var to = topInitialLocalPos;
            float t = 0f;
            float denom = Mathf.Max(pressAnimTime, 0.001f);

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / denom;
                float k = pressCurve.Evaluate(Mathf.Clamp01(t));
                buttonTop.localPosition = Vector3.LerpUnclamped(from, to, k);
                yield return null;
            }
            buttonTop.localPosition = to;
            visualPressed = false;
        }

        private void UpdatePressSensorStayCount()
        {
            if (!pressSensor)
            {
                sensorStayCount = 0;
                return;
            }

            Vector3 center;
            Vector3 halfExtents;
            Quaternion rot;

            if (pressSensor is BoxCollider box)
            {
                center = box.transform.TransformPoint(box.center);
                Vector3 worldSize = Vector3.Scale(box.size, box.transform.lossyScale);
                halfExtents = worldSize * 0.5f;
                rot = box.transform.rotation;
            }
            else
            {
                center = pressSensor.bounds.center;
                halfExtents = pressSensor.bounds.extents;
                rot = pressSensor.transform.rotation;
            }

            halfExtents += new Vector3(0.01f, 0.02f, 0.01f);

            var hits = Physics.OverlapBox(
                center,
                halfExtents,
                rot,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            int count = 0;
            foreach (var h in hits)
            {
                if (!h) continue;
                if (h.attachedRigidbody && h.attachedRigidbody.CompareTag(activatorTag)) { count++; continue; }
                if (h.transform.root.CompareTag(activatorTag)) { count++; continue; }
            }
            sensorStayCount = count;
        }

        private bool IsPressedByActivator() => sensorStayCount > 0;
        private void CacheBlocksFromGroups()
        {
            cachedBlocks.Clear();
            foreach (var group in blockGroups)
            {
                if (!group) continue;
                foreach (Transform t in group.GetComponentsInChildren<Transform>(true))
                {
                    if (t == group) continue;
                    cachedBlocks.Add(t.gameObject);
                }
            }
        }

        private void SetBlocksActive(bool on)
        {
            foreach (var go in cachedBlocks)
                if (go) go.SetActive(on);
        }

        private void ExcludeChildrenOfButtonTopFromLegacyToggles()
        {
            if (!buttonTop) return;

            if (renderersToToggle != null && renderersToToggle.Length > 0)
            {
                var list = new List<Renderer>(renderersToToggle.Length);
                foreach (var r in renderersToToggle)
                {
                    if (!r) continue;
                    if (r.transform.IsChildOf(buttonTop)) continue;
                    list.Add(r);
                }
                renderersToToggle = list.ToArray();
            }

            if (collidersToToggle != null && collidersToToggle.Length > 0)
            {
                var list = new List<Collider>(collidersToToggle.Length);
                foreach (var c in collidersToToggle)
                {
                    if (!c) continue;
                    if (c.transform.IsChildOf(buttonTop)) continue;
                    list.Add(c);
                }
                collidersToToggle = list.ToArray();
            }
        }
        private bool IsPreviewNow()
        {
            return _playerInputRelay != null && _playerInputRelay.IsPreview;
        }
    }
}