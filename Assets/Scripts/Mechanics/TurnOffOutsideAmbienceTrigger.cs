using System.Collections;
using Platformer.Mechanics;
using UnityEngine;

public class TurnOffOutsideAmbienceTrigger : MonoBehaviour
{
    [SerializeField] private AudioSource sfx;
    [SerializeField] private ParticleSystem dust;
    [Space]
    [SerializeField] private float fadeDuration = 1f;

    private Collider2D _collider;
    private int _particleBuffer = 2048;
    private ParticleSystem.Particle[] _particles;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _particles = new ParticleSystem.Particle[_particleBuffer];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player =  other.GetComponent<PlayerController>();

        if (!player) 
            return;
        
        StartFade(0f);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (!player)
            return;
        
        StartFade(0.16f);
    }
    
    private void StartFade(float targetVolume)
    {
        if (sfx == null) return;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeVolume(targetVolume));
    }
    
    private IEnumerator FadeVolume(float targetVolume)
    {
        var start = sfx.volume;
        var time = 0f;
        
        if (!sfx.isPlaying && targetVolume > 0f) sfx.Play();

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            sfx.volume = Mathf.Lerp(start, targetVolume, time / fadeDuration);
            yield return null;
        }

        sfx.volume = targetVolume;

        _fadeCoroutine = null;
    }

    private void LateUpdate()
    {
        if (!_collider) 
            return;

        var count = dust.GetParticles(_particles);
        var write = 0;

        for (var i = 0; i < count; i++)
        {
            var localPos = _particles[i].position;
            var worldPos = dust.transform.TransformPoint(localPos);
            var pos2D = new Vector2(worldPos.x, worldPos.y);
            // kill particles inside collider
            if (_collider.OverlapPoint(pos2D)) continue;
            _particles[write++] = _particles[i];
        }

        if (write != count)
            dust.SetParticles(_particles, write);
    }
}
