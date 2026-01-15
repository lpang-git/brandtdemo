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
        private static readonly int DramaticFall = Animator.StringToHash("dramaticFall");
        private static readonly int Climbing = Animator.StringToHash("climbing");
        private static readonly int AimPressed = Animator.StringToHash("aimPressed");
        private static readonly int IsAiming = Animator.StringToHash("isAiming");
        private static readonly int HasStartedClimbing = Animator.StringToHash("climbStart");
        private static readonly int LookState = Animator.StringToHash("lookState");
        private static readonly int DeadByEnemy = Animator.StringToHash("deadByEnemy");
        private static readonly int DeadBySpikes = Animator.StringToHash("deadBySpikes");
        private static readonly int DeadByFall = Animator.StringToHash("deadByFall");
        private static readonly int DeadByVoid = Animator.StringToHash("deadByVoid");
        private static readonly int DeadByAufseher = Animator.StringToHash("deadByAufseher");
        private static readonly int DeadByFire = Animator.StringToHash("deadByFire");

        [Header("References")] 
        [SerializeField] protected Rigidbody2D rb;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] internal Animator animator;
        [SerializeField] internal Transform aimTransform;
        [SerializeField] internal Collider2D collider2d;
        [SerializeField] internal Health health;
        [SerializeField] internal DialogueManager dialogueManager;

        [Space] 
        [Header("Audio Sources")] 
        [SerializeField] private AudioSource[] sounds;

        [Space] 
        [Header("Settings")] 
        [Tooltip("Horizontal walking speed of the player.")] 
        [SerializeField] internal float playerSpeed = 3;
        
        [Tooltip("How high does the player jump?")] 
        [SerializeField] internal float jumpHeight = 4.5f;
        
        [Tooltip("How far does the player jump?")] 
        [SerializeField] private float jumpSpeed = 1.2f;
        
        [Tooltip("Short time frame that stores a recent jump press so the jump triggers as soon as the character becomes grounded.")]
        [SerializeField] private float jumpBufferTime = 0.12f;
        
        [Tooltip("Short grace period after leaving a platform during which a jump input still counts as grounded.")]
        [SerializeField] private float groundLeaveTime = 0.12f;
        
        [Tooltip("Player acceleration while grounded")]
        [SerializeField] internal float groundAccel = 40f;
        
        [Tooltip("Player acceleration while in air")]
        [SerializeField] private float airAccel = 2f;

        [Tooltip("How fast player gravity grows per second. Makes the player fall faster the longer the fall.")]
        [SerializeField] private float fallAccelRamp = 1.5f;
        
        [Tooltip("Cap of player gravity acceleration.")]
        [SerializeField] private float maxFallMultiplier = 4f;

        [Tooltip("Fall distance where fall becomes lethal.")] 
        [SerializeField] private float dramaticThreshold = 5f;
        
        [Tooltip("How fast can the player climb?")]
        [SerializeField] protected float climbingSpeed = 2;

        /// hidden values ///

        // basics
        internal bool controlEnabled;
        internal InputAction moveAction;
        internal InputAction jumpAction;
        private InputAction lookAction;
        internal bool dyingState;
        private Vector2 move;
        private bool _skipFirstDependency;
        
        // jump values
        private bool _wasGrounded = true;
        private float _groundLeaveTimer;
        private float _jumpBufferTimer;
        private bool _jumpRequested;
        private float _fallTime;
        private bool _jumpExecutedThisAirborne = false;
        private bool _wasAirborne;
        
        // aim values
        private Vector2 mousePosition;
        
        // dialogue values
        internal bool playerInRangeOfDialogue;
        
        // dramatic fall values
        internal bool fallingDramatically;
        private float fallDistance;
        private float fallStartPosY;
        
        // ladder values
        private bool _isLadder  = false;
        private bool _canClimbUp;
        private bool _canClimbDown;
        private bool _activeClimbing;
        private bool _passiveClimbing;
        
        // private references
        private PlayerGunBehaviour _aimScript;
        private Camera _cameraMain;
        
    
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
                EnableControls();
                
                LookAtWalkingDirection();
                AimAtMouse();
                var inputVector = moveAction.ReadValue<Vector2>();
                move.x = inputVector.x; // Horizontal movement
                _skipFirstDependency = true;
            
                if (IsGrounded)
                {
                    _groundLeaveTimer = groundLeaveTime;
                }
                else
                {
                    _groundLeaveTimer -= Time.deltaTime;
                }

                if (jumpAction.WasPressedThisFrame())
                {
                    _jumpBufferTimer = jumpBufferTime;
                }
                else
                {
                    _jumpBufferTimer -= Time.deltaTime;
                }

                // request jump only if groundLeave is over & we haven't executed a jump for this airborne
                if (_jumpBufferTimer > 0f && _groundLeaveTimer > 0f && !_jumpExecutedThisAirborne)
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
            
                var verticalInput = inputVector.y != 0f;
                var nowOnLadder = _isLadder;

                if (nowOnLadder && verticalInput && !_activeClimbing && !IsGrounded) // on ladder, actively climbing
                {
                    _passiveClimbing = false;
                    _activeClimbing = true;
                    if (fallingDramatically)
                    {
                        // Ladder can save player from deadly fall
                        animator.SetBool(DramaticFall, false);
                    }
                    animator.SetTrigger(HasStartedClimbing);
                    animator.SetBool(Climbing, true);
                    animator.speed = 1f;
                } else if (nowOnLadder && !verticalInput && !_passiveClimbing && !IsGrounded) // on ladder, not actively climbing
                {
                    _activeClimbing = false;
                    _passiveClimbing = true;
                    if (fallingDramatically)
                    {
                        // Ladder can save player from deadly fall
                        animator.SetBool(DramaticFall, false);
                    }
                    animator.SetTrigger(HasStartedClimbing);
                    animator.SetBool(Climbing, true);
                    animator.speed = 0.5f;
                } else if ((!nowOnLadder && _activeClimbing) || (!nowOnLadder && _passiveClimbing) || nowOnLadder && IsGrounded) // not on ladder
                {
                    _activeClimbing = false;
                    _passiveClimbing =  false;
                    animator.SetBool(Climbing, false);
                    animator.speed = 1f;
                }
                
                if (!_isLadder) return; 
            
                LadderMovement(inputVector); // Vertical movement
                
                // clear any buffered jump while climbing
                _jumpBufferTimer = 0f;
                _jumpRequested = false;
            }
            else
            {
                DisableControls();
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
            velocity.y = inputVector.y * climbingSpeed;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            var npc = col.GetComponent<NPC>();
            var ammoBox = col.GetComponent<AmmoPickUpTrigger>();
            
            if (col.CompareTag("Ladder"))
            {
                _isLadder = true;
            } else if (npc)
            {
                playerInRangeOfDialogue = true;
                dialogueManager.SetNPC(npc);
            } else if (ammoBox)
            {
                _aimScript.OnAmmoPickedUp(ammoBox.ammoNum);
                ammoBox.gameObject.GetComponent<Collider2D>().enabled = false;
                ammoBox.OnPickedUp();
                PlaySoundAndAnims(15);
            } else if (col.CompareTag("BossCollider"))
            {
                if (dyingState)
                    return;
                Schedule<PlayerDeath>();
                dyingState = true;
                PlaySoundAndAnims(12);
            } else if (col.CompareTag("Fire"))
            {
                if (dyingState)
                    return;
                Schedule<PlayerDeath>();
                dyingState = true;
                PlaySoundAndAnims(16);
            }
        }

        private void OnTriggerExit2D(Collider2D col)
        {
            var npc = col.GetComponent<NPC>();
            
            if (col.CompareTag("Ladder"))
            {
                _isLadder = false;
            } else if (npc)
            {
                playerInRangeOfDialogue = false;
                dialogueManager.EndDialogue();
            }
        }

        protected override void ComputeVelocity()
        {
            var inputVector = moveAction.ReadValue<Vector2>();
        
            if (_isLadder)
            {
                _aimScript.aimAction.Disable();
                velocity.y = inputVector.y * climbingSpeed;
                CheckGrounded();
                targetVelocity = move * playerSpeed;
            }
            else
            {
                _aimScript.aimAction.Enable();
                var targetX = move.x * playerSpeed;
                var accel = IsGrounded ? groundAccel : airAccel;
                velocity.x = Mathf.MoveTowards(velocity.x, targetX, accel * Time.deltaTime);
            
                if (_jumpRequested)
                {
                    velocity.y = jumpHeight;
                    _jumpRequested = false;

                    // Ensure Jump is scheduled only once per airborne session
                    if (!_jumpExecutedThisAirborne)
                    {
                        PlaySoundAndAnims(2);
                        _jumpExecutedThisAirborne = true;
                        velocity.x = move.x * playerSpeed * jumpSpeed;
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
                        velocity.y = Mathf.Max(velocity.y, -5);
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
        }

        private void CheckGrounded()
        {
            if (_wasAirborne && !IsGrounded)
            {
               fallDistance = fallStartPosY - transform.position.y;
               
               if (!fallingDramatically && fallDistance > dramaticThreshold && !dyingState)
               {
                   Debug.Log("dramatic fall started");
                   fallingDramatically = true;
                   PlaySoundAndAnims(9);
               }
            }
            
            switch (fallingDramatically)
            {
                case true when !_isLadder:
                {
                    if (!IsGrounded || dyingState) return;
                    dyingState = true;
                    Schedule<PlayerDeath>();
                    PlaySoundAndAnims(10);
                    fallingDramatically = false;
                    fallStartPosY = 0f;
                    break;
                }
                case true when _isLadder:
                    if (fallingDramatically)
                    {
                        // If climbing stop fall
                        animator.SetBool(DramaticFall, false);
                    }
                    fallingDramatically = false;
                    fallStartPosY = 0f;
                    break;
                default:
                {
                    // Is either landing OR walking
                    PlaySoundAndAnims(14);
                
                    if (!IsGrounded) return;
                    _wasAirborne = false;
                    break;
                }
            }
        }

        protected override void PerformMovement(Vector2 moveV, bool yMovement)
        {
            base.PerformMovement(moveV, yMovement);
            
            if (IsGrounded || !verticalBlocked) return;
            
            const float extraClearance = 0.001f;
            var penetration = Mathf.Max(0f, (shellRadius + 0.001f) - blockingDistance);
            var nudge = penetration + extraClearance;
            var escape = blockingNormal * nudge;
            body.MovePosition(body.position + escape);

            var into = Vector2.Dot(velocity, blockingNormal);
            if (into < 0f) velocity -= into * blockingNormal;

            if (Mathf.Abs(blockingNormal.y) < 0.5f)
                velocity.y = Mathf.Max(0f, velocity.y);

            _fallTime = 0f;
            _wasAirborne = false;
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

        private void LookAtWalkingDirection()
        {
            if (velocity.x == 0) return;
            spriteRenderer.flipX = velocity.x < 0;
        }
        
        private void AimAtMouse()
        {
            var worldPosition = _cameraMain.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, _cameraMain.nearClipPlane));
            var aimDirection = (worldPosition - transform.position).normalized;
            var angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            
            aimTransform.eulerAngles = new Vector3(0, 0, angle);
            var directionX = worldPosition.x - transform.position.x;
            
            var aimThreshold = 0.1f;
            var upThreshold = 0.5f;
            var downThreshold = -0.5f;
            
            if (!_aimScript.isAiming) { return; } 
            
            // Player flips based on mouse position
            spriteRenderer.flipX = directionX < -aimThreshold;
            
            aimTransform.localPosition = new Vector3(0, aimTransform.localPosition.y, aimTransform.localPosition.z); // Move right
            
            // Determine vertical look state using Y difference in world space
            var yDiff = worldPosition.y - transform.position.y;

            int lookState;
            if (yDiff >= upThreshold) lookState = 2;        // look up
            else if (yDiff <= downThreshold) lookState = 0; // look down
            else lookState = 1;                             // center

            animator.SetInteger(LookState, lookState);
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

        private void EnableControls()
        {
            moveAction.Enable();
            jumpAction.Enable();
            lookAction.Enable();
            dialogueManager.interAction.Enable();
            _aimScript.aimAction.Enable();
            _aimScript.fireAction.Enable();
            _aimScript.reloadAction.Enable();
        }

        private void DisableControls()
        {
            moveAction.Disable();
            jumpAction.Disable();
            lookAction.Disable();
            dialogueManager.interAction.Disable();
            _aimScript.aimAction.Disable();
            _aimScript.fireAction.Disable();
            _aimScript.reloadAction.Disable();
        }
    
        #region Sounds & Animations

            internal void PlaySoundAndAnims(int actionId)
            {
                animator.SetBool(IsAiming, _aimScript.isAiming);
                
                switch (actionId)
                {
                    case 0:
                        // Inhale sound
                        sounds[0].Play();
                        break;
                    case 1:
                        // Exhale sound
                        sounds[1].Play();
                        break;
                    case 2:
                        // jump sound
                        sounds[2].Play();
                        break;
                    case 3:
                        // footstep sound
                        sounds[3].Play();
                        break;
                    case 4:
                        // landing sound
                        sounds[14].Play();
                        break;
                    case 5:
                        // holster/unholster weapon
                        animator.SetTrigger(AimPressed);
                        if (_aimScript.isAiming)
                        {
                            sounds[4].Play(); // unholster (aim)
                        }
                        else
                        {
                            sounds[5].Play(); // holster (stop aiming)
                        }
                        break;
                    case 6:
                        // die by void
                        {
                            // If void kills player, no dramatic impact will happen
                            animator.SetBool(DramaticFall, false);
                            fallingDramatically  = false;
                        }
                        animator.SetTrigger(DeadByVoid);
                        animator.SetBool(Dead, true);
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
                        
                        if (fallingDramatically)
                        {
                            // If enemy kills player before they hit ground
                            animator.SetBool(DramaticFall, false);
                            fallingDramatically  = false;
                        }
                        animator.SetTrigger(DeadByEnemy);
                        animator.SetBool(Dead, true);
                        sounds[8].Play();
                        break;
                    case 9:
                        // dramatic fall
                        animator.SetBool(DramaticFall, true);
                        sounds[9].Play();
                        break;
                    case 10:
                        // die by fall damage
                        animator.SetTrigger(DeadByFall);
                        animator.SetBool(Dead, true);
                        StopAllPlayerSounds();
                        sounds[10].Play();
                        break;
                    case 11:
                        // die by spikes
                        if (fallingDramatically)
                        {
                            // If spikes kill player before they hit ground
                            animator.SetBool(DramaticFall, false);
                            fallingDramatically  = false;
                        }
                        animator.SetTrigger(DeadBySpikes);
                        animator.SetBool(Dead, true);
                        StopAllPlayerSounds();
                        sounds[11].Play();
                        break;
                    case 12:
                        // die by Aufseher
                        if (fallingDramatically)
                        {
                            // If Aufseher kills player before they hit ground
                            animator.SetBool(DramaticFall, false);
                            fallingDramatically  = false;
                        }
                        animator.SetTrigger(DeadByAufseher);
                        animator.SetBool(Dead, true);
                        StopAllPlayerSounds();
                        sounds[12].Play();
                        break;
                    case 13:
                        // respawning
                        sounds[13].Play();
                        break;
                    case 14:
                        // Walking, jumping, idle & landing animation
                        animator.SetBool(Grounded, IsGrounded); // handles jump/land & if run
                        animator.SetFloat(VelocityX, Mathf.Abs(velocity.x) / playerSpeed); // handles idle/run & if landing
                        break;
                    case 15:
                        // ammo box opened
                        sounds[15].Play();
                        break;
                    case 16:
                        // death by fire
                        {
                            // If fire kills player before they hit ground
                            animator.SetBool(DramaticFall, false);
                            fallingDramatically  = false;
                        }
                        StopAllPlayerSounds();
                        animator.SetTrigger(DeadByFire);
                        animator.SetBool(Dead, true);
                        sounds[16].Play();
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
                animator.SetBool(DramaticFall, false);
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
