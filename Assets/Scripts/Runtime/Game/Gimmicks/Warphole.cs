using UnityEngine;

namespace Game.Gimmicks
{
    public class Warphole : MonoBehaviour
{

    [Header("Warp Settings")]
    public Warphole TargetWarpHole;  

    [Header("Effects")]
    public AudioClip WarpSE;
    public ParticleSystem WarpEffect;

    private AudioSource _audioSource;
    private bool _isWarping;


    private void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterPlayer(other);
    }



    public void OnTriggerEnterPlayer(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_isWarping) return;  
        if (TargetWarpHole == null) return;

        StartWarp(other.gameObject);
    }

    public void StartWarp(GameObject player)
    {
        _isWarping = true;

        PlayWarpEffectsInternal();

        player.transform.position = TargetWarpHole.transform.position + Vector3.up; 

        TargetWarpHole._isWarping = true;

        Invoke(nameof(StopWarp), 0.2f);
        TargetWarpHole.Invoke(nameof(TargetWarpHole.StopWarp), 0.2f);
    }

    public void StopWarp()
    {
        _isWarping = false;
    }

    public void PlayWarpEffectsInternal()
    {
        if (WarpSE != null)
        {
            if (_audioSource != null)
                _audioSource.PlayOneShot(WarpSE);
            else
                AudioSource.PlayClipAtPoint(WarpSE, transform.position);
        }

        if (WarpEffect != null)
        {
            ParticleSystem effect = Instantiate(WarpEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
        }
    }
}
