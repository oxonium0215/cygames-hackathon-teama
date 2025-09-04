using UnityEngine;

namespace Game.Gimmicks.FadingTile
{
    public class FadeOutChecker : MonoBehaviour
    {
        private FadingTile _fadingTile;
        void Start()
        {
            Transform parent = transform.parent;
            if (parent == null) return;
            Transform grandParent = parent.parent;
            if (grandParent != null) parent = grandParent;

            foreach (Transform child in grandParent)
            {
                if (child.CompareTag("FadingTile"))
                {
                    _fadingTile = child.GetComponent<FadingTile>();
                    break;
                }
            }
        }

        // Update is called once per frame
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _fadingTile.FadeOut();
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _fadingTile.FadeOutCancel();
            }
        }
    }
}