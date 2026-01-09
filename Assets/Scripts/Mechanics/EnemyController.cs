using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// A simple controller for enemies. Provides movement control over a patrol path.
    /// </summary>
    [RequireComponent(typeof(AnimationController), typeof(Collider2D))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private PatrolPath path;
        [SerializeField] internal AudioClip ouch;
        
        [SerializeField] private float pursuitRadius = 2f;
        [SerializeField] private float pursuitSpeed = 1.0f; // multiplier of control.maxSpeed
        
        internal PatrolPath.Mover mover;
        internal AnimationController control;
        internal Collider2D enemyCollider;
        internal AudioSource enemyAudio;
        internal SpriteRenderer spriteRenderer;
        
        private bool _pursuing = false;
        private PlayerController _player;

        public Bounds Bounds => enemyCollider.bounds;

        private void Awake()
        {
            control = GetComponent<AnimationController>();
            enemyCollider = GetComponent<Collider2D>();
            enemyAudio = GetComponent<AudioSource>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) _player = playerObj.GetComponent<PlayerController>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.gameObject.GetComponent<PlayerController>();
            var bullet = other.gameObject.GetComponent<BulletMovement>();
            
            if (player != null)
            {
                if (player.dyingState) { return; }
                player.dyingState = true;
                Schedule<PlayerDeath>();
                player.PlaySoundAndAnims(8);
            } else if (bullet != null)
            {
                Schedule<EnemyDeath>().enemy = this;
            }
        }

        private void Update()
        {
            if (!_player)
            {
                Debug.LogError("Player not assigned.");
            }
            
            if (_player)
            {
                var dist = Vector2.Distance(_player.transform.position, transform.position);
                _pursuing = dist <= pursuitRadius;
            }
            else
            {
                _pursuing = false;
            }

            if (_pursuing)
            {
                mover = null;
                var dir = Mathf.Clamp(_player.transform.position.x - transform.position.x, -1f, 1f);
                control.move.x = dir * Mathf.Abs(control.maxSpeed) * pursuitSpeed / Mathf.Abs(control.maxSpeed);
                control.move.x = dir;
                
                if (spriteRenderer) { spriteRenderer.flipX = dir < 0f; }
            }
            else
            {
                // resume patrol
                if (!path) return;
                
                mover ??= path.CreateMover(control.maxSpeed * 0.5f);
                control.move.x = Mathf.Clamp(mover.Position.x - transform.position.x, -1, 1);
            }
        }

    }
}