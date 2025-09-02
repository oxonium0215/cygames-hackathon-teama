using UnityEngine;
using System.Collections;
using Game.Input;

public class FadeInChecker : MonoBehaviour
{
    [SerializeField] private float fadeInMs = 1000f;
    private FadingTile _fadingTile;
    private bool _isPlayerInside;
    private bool _fadeInStart;
    [SerializeField] private PlayerInputRelay _playerInputRelay;
    private Coroutine _fadeInCoroutine;
    void Start()
    {
        _fadeInStart = false;
        Transform parent = transform.parent;
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (child.CompareTag("FadingTile"))
            {
                transform.position = child.position;
                transform.rotation = child.rotation;
                transform.localScale = child.localScale;
                _fadingTile = child.GetComponent<FadingTile>();
                break;
            }
        }
        _fadeInCoroutine = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (_fadingTile == null) return;
        if (_fadingTile.IsFadingOut && !_fadeInStart)
        {
            _fadeInCoroutine = StartCoroutine(FadeInRoutine());
        }
        if (_fadeInCoroutine != null && _playerInputRelay.IsPreview)
        {
            StopCoroutine(_fadeInCoroutine);
            _fadeInCoroutine = null;
        }
        if (_fadeInCoroutine == null && !_playerInputRelay.IsPreview)
        {
            _fadeInCoroutine = StartCoroutine(FadeInRoutine());
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = false;
        }
    }
    private IEnumerator FadeInRoutine()
    {
        _fadeInStart = true;
        yield return new WaitForSeconds(fadeInMs / 1000f);

        yield return new WaitUntil(() => _isPlayerInside == false);

        _fadingTile.FadeIn();
        _fadeInStart = false;
        _fadeInCoroutine = null;
    }
}
