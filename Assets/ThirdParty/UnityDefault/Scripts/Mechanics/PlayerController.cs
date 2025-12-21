using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using UnityEngine.InputSystem;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        private static readonly int Grounded = Animator.StringToHash("grounded");
        private static readonly int VelocityX = Animator.StringToHash("velocityX");

        [Header("References")]
        [SerializeField] protected Rigidbody2D rb;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] internal Animator animator;
        [SerializeField] internal Transform aimTransform;
        [SerializeField] internal Collider2D collider2d;
        [SerializeField] internal AudioSource audioSource;
        [SerializeField] internal Health health;
        
        [Space]
        [Header("Settings")]
        
        [Tooltip("Horizontal walking speed of the player.")]
        [SerializeField]
        internal float playerSpeed = 3;
        
        [Tooltip("Initial jump velocity at the start of a jump.")]
        [SerializeField] public float jumpTakeOffSpeed = 3;
        
        [Tooltip("How far is the gun from the player?")]
        [SerializeField] private float gunDistance = 0.2f;
        
        [Tooltip("How fast can the player climb?")]
        [SerializeField] protected float climbingSpeed = 2;
        
        [Tooltip("Coyote time is a short grace period (≈0.06–0.15s) after leaving a platform during which a jump input still counts as grounded.")]
        [SerializeField] private float coyoteTime = 0.12f;
        
        [Tooltip("Jump buffer time is a short window (≈0.06–0.15s) that stores a recent jump press so the jump triggers as soon as the character becomes grounded.")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        
        [Tooltip("Player acceleration while grounded")]
        [SerializeField] private float groundAccel = 40f;
        
        [Tooltip("Player acceleration while in air")]
        [SerializeField] private float airAccel = 20f;

        [Tooltip("How fast player gravity grows per second. Makes the player fall faster the longer the fall.")]
        [SerializeField] private float fallAccelRamp = 1.5f;
        
        [Tooltip("Cap of player gravity acceleration.")]
        [SerializeField] private float maxFallMultiplier = 4f;
        
        [Tooltip("Duration of fall at which the player enters a lethal fall.")]
        public float dramaticFallTime = 2f; 
        
        [Space]
        [Header("Debug Values")]
        [SerializeField] protected Vector2 move;
        [SerializeField] protected Vector2 mousePosition;
        [SerializeField] internal bool controlEnabled;
        
        [Space]
        [Header("Audio Clips")]
        [SerializeField] public AudioClip jumpAudio;
        [SerializeField] public AudioClip respawnAudio;
        [SerializeField] public AudioClip ouchAudio;

        internal InputAction moveAction;
        private InputAction jumpAction;
        private InputAction lookAction;

        protected bool skipFirstDependency;
        protected Camera cameraMain;
        
        private float _coyoteTimer;
        protected float _jumpBufferTimer;
        protected bool _jumpRequested;
        private float _fallTime;
        
        private bool _wasGrounded = true;
        private bool _jumpExecutedThisAirborne = false; 

        public Bounds Bounds => collider2d.bounds;
        
        protected virtual void Awake()
        {
            controlEnabled = true;
            
            cameraMain = Camera.main;
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            aimTransform = transform.Find("Aim");

            moveAction = InputSystem.actions.FindAction("Player/Move");
            jumpAction = InputSystem.actions.FindAction("Player/Jump");
            lookAction = InputSystem.actions.FindAction("Player/Look");
            
            moveAction.Enable();
            jumpAction.Enable();
            lookAction.Enable();
        }

        protected override void Update()
        {
            if (IsGrounded)
            {
                _coyoteTimer = coyoteTime;
            }
            else
            {
                _coyoteTimer -= Time.deltaTime;
            }

            if (jumpAction.WasPressedThisFrame())
            {
                _jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                _jumpBufferTimer -= Time.deltaTime;
            }

            // request jump only if coyote available & we haven't executed a jump for this airborne
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f && !_jumpExecutedThisAirborne)
            {
                _jumpRequested = true;
                _jumpBufferTimer = 0f;
            }

            if (!skipFirstDependency)
            {
                if (controlEnabled)
                {
                    AimAtMouse();

                    // horizontal input
                    move.x = moveAction.ReadValue<Vector2>().x;

                    if (jumpAction.WasReleasedThisFrame())
                    {
                        Schedule<PlayerStopJump>().player = this;
                    }
                }
                else
                {
                    move.x = 0;
                    _jumpRequested = false;
                }
            }
            if (!_wasGrounded && IsGrounded)
            {
                Schedule<PlayerLanded>().player = this;
                _jumpExecutedThisAirborne = false;
            }
                
            _wasGrounded = IsGrounded;
            
            base.Update();
        }

        protected override void ComputeVelocity()
        {
            var targetX = move.x * playerSpeed;
            var accel = IsGrounded ? groundAccel : airAccel;
            velocity.x = Mathf.MoveTowards(velocity.x, targetX, accel * Time.deltaTime);
            
            if (_jumpRequested)
            {
                velocity.y = jumpTakeOffSpeed;
                _jumpRequested = false;

                // Ensure PlayerJumped is scheduled only once per airborne session
                if (!_jumpExecutedThisAirborne)
                {
                    Schedule<PlayerJumped>().player = this;
                    _jumpExecutedThisAirborne = true;
                }
            }
            
            if (velocity.y < 0f)
            {
                _fallTime += Time.deltaTime;
                var extra = 1f + Mathf.Clamp(_fallTime * fallAccelRamp, 0f, maxFallMultiplier - 1f);
                velocity.y += Physics2D.gravity.y * extra * Time.deltaTime;
            }
            else
            {
                _fallTime = 0f;
                velocity.y += Physics2D.gravity.y * Time.deltaTime;
            }

            UpdateAnimation();
            targetVelocity = new Vector2(velocity.x, targetVelocity.y);
        }
        
        protected void UpdateAnimation()
        {
            animator.SetBool(Grounded, IsGrounded);
            animator.SetFloat(VelocityX, Mathf.Abs(velocity.x) / playerSpeed);
            
            var dramatic = _fallTime >= dramaticFallTime;
            if (!dramatic) return; // Check for dramatic fall
            
            animator.SetBool("DramaticFall", dramatic);
            // when grounded, Schedule<PlayerDeath>();
            Debug.Log("dramatic fall started");
        }

        protected override void OnEnable()
        {
            lookAction.performed += OnLook;
            
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            lookAction.performed -= OnLook;
            
            base.OnDisable();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            mousePosition = context.ReadValue<Vector2>();
        }

        protected virtual void AimAtMouse()
        {
            var worldPosition = cameraMain.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cameraMain.nearClipPlane));
            var aimDirection = (worldPosition - transform.position).normalized;
            var angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            
            aimTransform.eulerAngles = new Vector3(0, 0, angle);
            var directionX = worldPosition.x - transform.position.x;
            var aimThreshold = 0.1f;
            
            spriteRenderer.flipX = directionX < -aimThreshold;
            
            aimTransform.localPosition = directionX < -aimThreshold ? new Vector3(-gunDistance, aimTransform.localPosition.y, aimTransform.localPosition.z) : // Move left
                new Vector3(gunDistance, aimTransform.localPosition.y, aimTransform.localPosition.z); // Move right
        }
    }
}