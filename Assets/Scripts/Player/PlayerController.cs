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
        [SerializeField] internal Health health;

        [Space] 
        [Header("Audio Sources")] 
        [SerializeField] private AudioSource[] sounds;

        [Space] [Header("Settings")] 
        [Tooltip("Horizontal walking speed of the player.")] 
        [SerializeField] internal float playerSpeed = 3;
        
        [Tooltip("Initial jump velocity at the start of a jump.")] 
        [SerializeField] internal float jumpTakeOffSpeed = 3;
        
        [Tooltip("How far is the gun from the player?")]
        [SerializeField] private float gunDistance = 0.2f;
        
        [Tooltip("How fast can the player climb?")]
        [SerializeField] protected float climbingSpeed = 2;
        
        [Tooltip("Coyote time is a short grace period (≈0.06–0.15s) after leaving a platform during which a jump input still counts as grounded.")]
        [SerializeField] private float coyoteTime = 0.12f;
        
        [Tooltip("Jump buffer time is a short window (≈0.06–0.15s) that stores a recent jump press so the jump triggers as soon as the character becomes grounded.")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        
        [Tooltip("Player acceleration while grounded")]
        [SerializeField] internal float groundAccel = 40f;
        
        [Tooltip("Player acceleration while in air")]
        [SerializeField] private float airAccel = 20f;

        [Tooltip("How fast player gravity grows per second. Makes the player fall faster the longer the fall.")]
        [SerializeField] private float fallAccelRamp = 1.5f;
        
        [Tooltip("Cap of player gravity acceleration.")]
        [SerializeField] private float maxFallMultiplier = 4f;
        
        [Space]
        [Header("Debug Values")]
        [SerializeField] internal Vector2 move;
        [SerializeField] internal Vector2 mousePosition;
        [SerializeField] internal bool controlEnabled;

        internal InputAction moveAction;
        private InputAction jumpAction;
        private InputAction lookAction;

        private bool _skipFirstDependency;
        private Camera _cameraMain;
        
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpRequested;
        private float _fallTime;
        
        private bool _wasGrounded = true;
        private bool _jumpExecutedThisAirborne = false;
        
        internal bool dyingState;
        private bool _fallingDramatically;
    
        private bool _isLadder  = false;
        private bool _canClimbUp;
        private bool _canClimbDown;
    
        public Bounds Bounds => collider2d.bounds;
    
        protected virtual void Awake()
        {
            controlEnabled = true;
            
            _cameraMain = Camera.main;
            health = GetComponent<Health>();
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
            if (controlEnabled)
            {
                AimAtMouse();
                var inputVector = moveAction.ReadValue<Vector2>();
                move.x = inputVector.x; // Horizontal movement
                _skipFirstDependency = true;
            
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

                if (!_skipFirstDependency)
                {
                    if (controlEnabled)
                    {
                        AimAtMouse();
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
                CheckLadderCollisions();
            
                if (!_isLadder) return; 
            
                LadderMovement(inputVector); // Vertical movement
                
                // clear any buffered jump while climbing
                _jumpBufferTimer = 0f;
                _jumpRequested = false;
            }
            else
            {
                move.x = 0;
            }
        }

        protected override void FixedUpdate()
        {
            if (dyingState)
            {
                // stop physics movement immediately
                velocity = Vector2.zero;
                targetVelocity = Vector2.zero;
                return;
            }
            base.FixedUpdate();
        }

        private void LadderMovement(Vector2 inputVector)
        {
            switch (inputVector.y)
            {
                case > 0 when _canClimbUp:
                    rb.position += new Vector2(0, climbingSpeed * Time.deltaTime); // Climbing up
                    break;
                case < 0 when _canClimbDown:
                    rb.position += new Vector2(0, -climbingSpeed * Time.deltaTime); // Climbing down
                    break;
            }
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if  (!col.CompareTag("Ladder")) { return; }
        
            _isLadder = true;
        }

        private void OnTriggerExit2D(Collider2D col)
        {
            if (!col.CompareTag("Ladder")) { return; }
        
            _isLadder = false;
        }

        protected override void ComputeVelocity()
        {
            var inputVector = moveAction.ReadValue<Vector2>();
        
            if (_isLadder)
            {
                velocity.y = inputVector.y * climbingSpeed;
                CheckGrounded();
                targetVelocity = move * playerSpeed;
            }
            else
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

                CheckGrounded();
                targetVelocity = new Vector2(velocity.x, targetVelocity.y);
            }
            velocity.x = move.x * playerSpeed;
        }

        private void CheckGrounded()
        {
            if (_fallingDramatically && !_isLadder)
            {
                if (!IsGrounded) return;
                Schedule<PlayerDeath>();
                PlaySoundAndAnims(10);
                _fallingDramatically  = false;
            }
            else
            {
                animator.SetBool(Grounded, IsGrounded);
                animator.SetFloat(VelocityX, Mathf.Abs(velocity.x) / playerSpeed);
                            
                if (!IsGrounded) return;
                sounds[13].Play();
            }
        }
    
        private void CheckLadderCollisions()
        {
            var boxCastSize = new Vector2(0.1f, 0.1f);
            var positionAbove = rb.position + new Vector2(0, boxCastSize.y / 2);
            var positionBelow = rb.position - new Vector2(0, boxCastSize.y / 2);

            // Check for colliders above and below
            _canClimbUp = Physics2D.OverlapBox(positionAbove, boxCastSize, 0, LayerMask.GetMask("Default")) == null;
            _canClimbDown = Physics2D.OverlapBox(positionBelow, boxCastSize, 0, LayerMask.GetMask("Default")) == null;
        }

        private void AimAtMouse()
        {
            var worldPosition = _cameraMain.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, _cameraMain.nearClipPlane));
            var aimDirection = (worldPosition - transform.position).normalized;
            var angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            
            aimTransform.eulerAngles = new Vector3(0, 0, angle);
            var directionX = worldPosition.x - transform.position.x;
            var aimThreshold = 0.1f;
            
            spriteRenderer.flipX = directionX < -aimThreshold;
            
            aimTransform.localPosition = directionX < -aimThreshold ? new Vector3(-gunDistance, aimTransform.localPosition.y, aimTransform.localPosition.z) : // Move left
                new Vector3(gunDistance, aimTransform.localPosition.y, aimTransform.localPosition.z); // Move right
        
            // Player flips based on mouse position
            if (directionX > aimThreshold)
                spriteRenderer.flipX = false;
            else if (directionX < -aimThreshold)
                spriteRenderer.flipX = true;
        }
    
        internal void OnDramaticFall()
        {
            Debug.Log("dramatic fall started");
            _fallingDramatically = true;
            PlaySoundAndAnims(9);
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
    
        #region Sounds & Animations

            internal void PlaySoundAndAnims(int actionId)
            {
                switch (actionId)
                {
                    case 0:
                        // Idle breathing
                        sounds[0].Play();
                        break;
                    case 1:
                        // jump
                        sounds[1].Play();
                        break;
                    case 2:
                        // walk
                        sounds[2].Play();
                        break;
                    case 3:
                        // unholster weapon
                        sounds[3].Play();
                        break;
                    case 4:
                        // holster weapon
                        sounds[4].Play();
                        break;
                    case 5:
                        // shoot
                        sounds[5].Play();
                        break;
                    case 6:
                        // die by void
                        StopAllPlayerSounds();
                        sounds[6].Play();
                        break;
                    case 7:
                        // climbing
                        sounds[7].Play();
                        break;
                    case 8:
                        // die by enemy
                        StopAllPlayerSounds();
                        sounds[8].Play();
                        break;
                    case 9:
                        // dramatic fall
                        sounds[9].Play();
                        break;
                    case 10:
                        // die by fall damage
                        StopAllPlayerSounds();
                        sounds[10].Play();
                        break;
                    case 11:
                        // die by spikes
                        StopAllPlayerSounds();
                        sounds[11].Play();
                        break;
                    case 12:
                        // die by Aufseher
                        StopAllPlayerSounds();
                        sounds[12].Play();
                        break;
                    case 13:
                        // NOTE: sound 13 is landing on feet normally
                        break;
                    case 14:
                        // respawning
                        sounds[14].Play();
                        break;
                }
            }

            private void StopAllPlayerSounds()
            {
                foreach  (var sound in sounds)
                {
                    sound.Stop();
                }
            }

            private void StopAllAnimations()
            {
            
            }
        #endregion
    }
}
