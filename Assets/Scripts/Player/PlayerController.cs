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
        private static readonly int Dead = Animator.StringToHash("dead");
        private static readonly int Trap = Animator.StringToHash("trap");
        private static readonly int DramaticFall = Animator.StringToHash("dramaticFall");
        private static readonly int Climbing = Animator.StringToHash("climbing");
        private static readonly int AimPressed = Animator.StringToHash("aimPressed");
        private static readonly int IsAiming = Animator.StringToHash("isAiming");

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

        [Tooltip("Fall distance where fall becomes lethal.")] 
        [SerializeField] private float dramaticThreshold = 5f;

        [Space] 
        [Header("Debug Values")] 
        [SerializeField] private float fallDistance;
        [SerializeField] private float fallStartPosY;
        [SerializeField] internal Vector2 mousePosition;
        [SerializeField] internal bool controlEnabled;

        internal InputAction moveAction;
        internal InputAction jumpAction;
        internal InputAction lookAction;

        private Vector2 move;
        private bool _skipFirstDependency;
        private Camera _cameraMain;
        
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpRequested;
        private float _fallTime;
        
        private bool _wasGrounded = true;
        private bool _wasAirborne;
        private bool _jumpExecutedThisAirborne = false;
        
        internal bool dyingState;
        private bool _fallingDramatically;
    
        private bool _isLadder  = false;
        private bool _canClimbUp;
        private bool _canClimbDown;

        private PlayerGunBehaviour _aimScript;
    
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
            _aimScript = aimTransform.GetComponent<PlayerGunBehaviour>();

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
                    PlaySoundAndAnims(4);
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
                PlaySoundAndAnims(7);
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

                    // Ensure Jump is scheduled only once per airborne session
                    if (!_jumpExecutedThisAirborne)
                    {
                        PlaySoundAndAnims(2);
                        _jumpExecutedThisAirborne = true;
                    }
                }
            
                if (!IsGrounded)
                {
                    if (!_wasAirborne)
                    {
                        fallStartPosY = transform.position.y;
                        _wasAirborne  = true;
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
                }
                else
                {
                    velocity.y = Mathf.Max(0f, velocity.y);
                    _fallTime = 0f;
                }

                CheckGrounded();
                targetVelocity = new Vector2(velocity.x, targetVelocity.y);
            }
            velocity.x = move.x * playerSpeed;
        }

        private void CheckGrounded()
        {
            if (_wasAirborne && !IsGrounded)
            {
               fallDistance = fallStartPosY - transform.position.y;
               
               if (!_fallingDramatically && fallDistance > dramaticThreshold)
               {
                   Debug.Log("dramatic fall started");
                   _fallingDramatically = true;
                   PlaySoundAndAnims(9);
               }
            }

            switch (_fallingDramatically)
            {
                case true when !_isLadder:
                {
                    if (!IsGrounded) return;
                    Schedule<PlayerDeath>();
                    PlaySoundAndAnims(10);
                    _fallingDramatically = false;
                    fallStartPosY = 0f;
                    break;
                }
                case true when _isLadder:
                    _fallingDramatically = false;
                    fallStartPosY = 0f;
                    break;
                default:
                    // Is either landing OR walking
                    PlaySoundAndAnims(14);
                    _wasAirborne  = false;
                    break;
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
                animator.SetBool(IsAiming, _aimScript.isAiming);
                
                switch (actionId)
                {
                    case 0:
                        // Inhale sound
                        Debug.Log("0");
                        sounds[0].Play();
                        break;
                    case 1:
                        // Exhale sound
                        Debug.Log("1");
                        sounds[1].Play();
                        break;
                    case 2:
                        // jump sound
                        Debug.Log("2");
                        sounds[2].Play();
                        break;
                    case 3:
                        // footstep sound
                        Debug.Log("3");
                        sounds[3].Play();
                        break;
                    case 4:
                        // landing sound
                        Debug.Log("4");
                        sounds[14].Play();
                        break;
                    case 5:
                        // holster/unholster weapon
                        animator.SetTrigger(AimPressed);
                        if (_aimScript.isAiming)
                        {
                            Debug.Log("5.1");
                            sounds[4].Play(); // unholster (aim)
                        }
                        else
                        {
                            Debug.Log("5.2");
                            sounds[5].Play(); // holster (stop aiming)
                        }
                        break;
                    case 6:
                        // die by void
                        Debug.Log("6 pre");
                        StopAllPlayerSounds();
                        Debug.Log("6");
                        sounds[6].Play();
                        break;
                    case 7:
                        // climbing
                        if (_fallingDramatically)
                        {
                            // Ladder can save player from deadly fall
                            animator.SetBool(DramaticFall, false);
                        }
                        animator.SetBool(Climbing, true);
                        Debug.Log("7");
                        sounds[7].Play();
                        break;
                    case 8:
                        // die by enemy
                        Debug.Log("8 pre");
                        StopAllPlayerSounds();
                        
                        if (_fallingDramatically)
                        {
                            // If enemy kills player before they hit ground
                            animator.SetBool(DramaticFall, false);
                        }
                        
                        animator.SetBool(Dead, true);
                        Debug.Log("8");
                        sounds[8].Play();
                        break;
                    case 9:
                        // dramatic fall
                        animator.SetBool(DramaticFall, true);
                        Debug.Log("9");
                        sounds[9].Play();
                        break;
                    case 10:
                        // die by fall damage
                        animator.SetBool(Dead, true);
                        animator.SetBool(DramaticFall, false);
                        Debug.Log("10 pre");
                        StopAllPlayerSounds();
                        Debug.Log("10");
                        sounds[10].Play();
                        break;
                    case 11:
                        // die by spikes
                        if (_fallingDramatically)
                        {
                            // If spikes kill player before they hit ground
                            animator.SetBool(DramaticFall, false);
                        }
                        
                        animator.SetBool(Dead, true);
                        animator.SetBool(Trap, true);
                        Debug.Log("11 pre");
                        StopAllPlayerSounds();
                        Debug.Log("11");
                        sounds[11].Play();
                        break;
                    case 12:
                        // die by Aufseher
                        if (_fallingDramatically)
                        {
                            // If Aufseher kills player before they hit ground
                            animator.SetBool(DramaticFall, false);
                        }
                        
                        animator.SetBool(Dead, true);
                        Debug.Log("12 pre");
                        StopAllPlayerSounds();
                        Debug.Log("12");
                        sounds[12].Play();
                        break;
                    case 13:
                        // respawning
                        Debug.Log("13");
                        sounds[13].Play();
                        break;
                    case 14:
                        // Walking, jumping, idle & landing animation
                        animator.SetBool(Climbing, false);
                        animator.SetBool(Grounded, IsGrounded); // handles jump/land & if run
                        animator.SetFloat(VelocityX, Mathf.Abs(velocity.x) / playerSpeed); // handles idle/run & if landing
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

            internal void ResetAnimatorBools()
            {
                animator.SetBool(Dead, false);
                animator.SetBool(Trap, false);
            }

            protected void OnInhale()
            {
                PlaySoundAndAnims(0);
            }

            protected void OnExhale()
            {
                PlaySoundAndAnims(1);
            }

            protected void OnFootstep()
            {
                PlaySoundAndAnims(3);
            }

            #endregion
    }
}
