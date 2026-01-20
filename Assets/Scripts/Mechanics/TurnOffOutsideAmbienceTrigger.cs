using System;
using Platformer.Mechanics;
using UnityEngine;

public class TurnOffOutsideAmbienceTrigger : MonoBehaviour
{
    [SerializeField] private AudioSource sfx;
    [SerializeField] private ParticleSystem dust;

    private Collider2D _collider;
    private int _particleBuffer = 2048;
    private ParticleSystem.Particle[] _particles;

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
        
        sfx.Stop();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (!player)
            return;
        
        sfx.Play();
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
