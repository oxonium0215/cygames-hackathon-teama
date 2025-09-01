using UnityEngine;
using System.Collections;

public class FadingTile : MonoBehaviour
{
    [SerializeField] private float _waitBeforeFadeMs = 500f;
    [SerializeField] private float _fadeDurationMs = 1000f;
    private Coroutine _fadeCoroutine;
    private Renderer _rend;
    private Color _originalColor;
    public bool IsFadingOut { get; private set; }

    void Awake()
    {
        _rend = GetComponent<Renderer>();
        if (_rend != null)
        {
            _originalColor = _rend.material.color;
        }
    }

    void Start()
    {
        IsFadingOut = false;
    }

    // Update is called once per frame
    void Update()
    {
    }
    public void FadeOut()
    {
        if (IsFadingOut) return;
        _fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }
    public void FadeOutCancel()
    {
        if (IsFadingOut) return;
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        if (_rend != null)
        {
            _rend.material.color = _originalColor;
        }
    }

    public void FadeIn()
    {
        if (!IsFadingOut) return;
        gameObject.SetActive(true);
        IsFadingOut = false;
        _rend.material.color = _originalColor;
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(_waitBeforeFadeMs / 1000f);

        float elapsed = 0f;
        Color startColor = _originalColor;
        Color endColor = new Color(_originalColor.r, _originalColor.g, _originalColor.b, 0f);

        while (elapsed < _fadeDurationMs / 1000f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / (_fadeDurationMs / 1000f));
            if (_rend != null)
            {
                _rend.material.color = Color.Lerp(startColor, endColor, t);
            }
            yield return null;
        }

        gameObject.SetActive(false);
        IsFadingOut = true;
        _fadeCoroutine = null;
    }
}
