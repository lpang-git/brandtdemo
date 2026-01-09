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
        private static readonly int Attack = Animator.StringToHash("attack");
        private static readonly int DeathByPlayer = Animator.StringToHash("deathByPlayer");
        private static readonly int InPursuit = Animator.StringToHash("inPursuit");
        
        [Header("References")]
        [SerializeField] private PatrolPath path;
        [SerializeField] internal Collider2D hit;
        [Space]
        [SerializeField] private AudioSource[] audioSources;
        
        [Space]
        [Header("Settings")]
        [SerializeField] private float pursuitRadius = 2f;
        [SerializeField] private float pursuitSpeed = 1.0f; // multiplier of control.maxSpeed

        private PatrolPath.Mover _mover;
        private SpriteRenderer _spriteRenderer;
        private PlayerController _player;
        
        internal AnimationController control;
        internal Collider2D enemyCollider;
        internal Rigidbody2D rb;
        internal Animator animator;
        
        private bool _pursuing = false;
        internal bool dead;

        public Bounds Bounds => enemyCollider.bounds;

        private void Awake()
        {
            control = GetComponent<AnimationController>();
            enemyCollider = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            
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
                animator.SetTrigger(Attack);
                Schedule<PlayerDeath>();
                player.PlaySoundAndAnims(8);
            } else if (bullet != null)
            {
                animator.SetTrigger(DeathByPlayer);
                Schedule<EnemyDeath>().enemy = this;
            }
        }

        private void Update()
        {
            var dist = Vector2.Distance(_player.transform.position, transform.position);
            
            if (!_player)
            {
                Debug.LogError("Player not assigned.");
            }
            
            if (_player)
            {
                _pursuing = dist <= pursuitRadius;
            }
            else
            {
                _pursuing = false;
            }

            if (_pursuing && !dead)
            {
                animator.SetBool(InPursuit, true);
                animator.speed = 1.5f;
                
                _mover = null;
                var dir = Mathf.Clamp(_player.transform.position.x - transform.position.x, -1f, 1f);
                control.move.x = dir * Mathf.Abs(control.maxSpeed) * pursuitSpeed / Mathf.Abs(control.maxSpeed);
                control.move.x = dir;
                
                if (_spriteRenderer) { _spriteRenderer.flipX = dir < 0f; }
            }
            else
            {
                // on patrol
                if (!path || dead) return;
                
                animator.SetBool(InPursuit, false);
                animator.speed = 1;
                _mover ??= path.CreateMover(control.maxSpeed * 0.5f);
                control.move.x = Mathf.Clamp(_mover.Position.x - transform.position.x, -1, 1);
            }
        }

        internal void PlaySound(int actionId)
        {
            switch (actionId)
            {
                case 0:
                    // inhale
                    audioSources[0].Play();
                    break;
                case 1:
                    // exhale
                    audioSources[1].Play();
                    break;
                case 2:
                    // attack sound
                    audioSources[2].Play();
                    break;
                case 3:
                    // death by player sound
                    audioSources[3].Play();
                    break;
                case 4:
                    // death by spike sound
                    audioSources[4].Play();
                    break;
                case 5:
                    // footstep sound
                    audioSources[5].Play();
                    break;
            }
        }
    }
}