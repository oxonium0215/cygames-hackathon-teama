using UnityEngine;

namespace Game.Gimmicks
{
    public class Coin : MonoBehaviour
    {
    public const string PLAYER_TAG = "Player";
    public const float DEFAULT_ROTATION_SPEED = 180f;

    [Header("Rotate")]
    public float RotationSpeed = DEFAULT_ROTATION_SPEED;
    [Header("Float")]
    public float FloatAmplitude = 0.001f;  // 揺れる幅
    public float FloatFrequency = 2f;     // 揺れる速さ

    [Header("Effects")]
    public AudioClip PickupSE;              
    public ParticleSystem PickupEffect;
    private AudioSource _audioSource;         
    private bool _isCollected;
    
    


    private void Update()
    {
        RotateSelf();
        FloatUpDown();
    }

    private void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterPlayer(other);
    }


    public void RotateSelf()
    {
        if (_isCollected) return;
        transform.Rotate(0f, RotationSpeed * Time.deltaTime, 0f ,Space.World);
    }
    
    public void FloatUpDown()
    {
        if (_isCollected) return;

        float newY = transform.position.y + Mathf.Sin(Time.time * FloatFrequency) * FloatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    public void OnTriggerEnterPlayer(Collider other)
    {
        if (_isCollected) return;
        if (!other.CompareTag("Player")) return;

        _isCollected = true;
        PlayPickupEffectsInternal(); 
        Destroy(gameObject);
    }


    public void PlayPickupEffectsInternal()
    {
        // SE
        if (PickupSE != null)
        {
            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(PickupSE);
            }
            else
            {
                AudioSource.PlayClipAtPoint(PickupSE, transform.position);
            }
        }

        if (PickupEffect != null)
        {
            ParticleSystem effect = Instantiate(PickupEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
        }
    }
}
}
