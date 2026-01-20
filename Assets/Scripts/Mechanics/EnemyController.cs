using System.Collections;
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
        private static readonly int DeathBySpike = Animator.StringToHash("deathBySpike");
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
        private Coroutine _groanRoutine;

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
                PlaySoundAndAnims(1);
                Schedule<PlayerDeath>();
                player.PlaySoundAndAnims(8);
            } else if (bullet != null)
            {
                PlaySoundAndAnims(2);
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
                
                _groanRoutine ??= StartCoroutine(Groaning());
                animator.SetBool(InPursuit, false);
                animator.speed = 1;
                _mover ??= path.CreateMover(control.maxSpeed * 0.5f);
                control.move.x = Mathf.Clamp(_mover.Position.x - transform.position.x, -1, 1);
            }
        }

        private IEnumerator Groaning()
        {
            do
            {
                float wait = Random.Range(3, 8);
                yield return new WaitForSeconds(wait);
                
                PlaySoundAndAnims(0);

            } while (!dead);
        }

        protected void OnPursuitGroan()
        {
            PlaySoundAndAnims(4);
        }

        internal void PlaySoundAndAnims(int actionId)
        {
            switch (actionId)
            {
                case 0:
                    // groan
                    if (_pursuing) 
                        return;
                    
                    audioSources[0].pitch = Random.Range(0.5f, 1.5f);
                    audioSources[0].Play();
                    break;
                case 1:
                    // attack
                    animator.SetTrigger(Attack);
                    audioSources[1].pitch = Random.Range(0.7f, 1f);
                    audioSources[1].Play();
                    break;
                case 2:
                    // death by player
                    animator.SetTrigger(DeathByPlayer);
                    audioSources[2].Play();
                    break;
                case 3:
                    // death by spike
                    animator.SetTrigger(DeathBySpike);
                    audioSources[3].Play();
                    break;
                case 4:
                    // pursuit sound
                    if (!_pursuing)
                        return;
                    audioSources[4].pitch = Random.Range(0.8f, 1.1f);
                    audioSources[4].Play();
                    break;
            }
        }
    }
}