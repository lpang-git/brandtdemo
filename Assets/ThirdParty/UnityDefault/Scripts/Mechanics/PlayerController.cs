using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using UnityEngine.InputSystem;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        [SerializeField] public AudioClip jumpAudio;
        [SerializeField] public AudioClip respawnAudio;
        [SerializeField] public AudioClip ouchAudio;

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        [SerializeField] public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        [SerializeField] public float jumpTakeOffSpeed = 7;

        [SerializeField] public JumpState jumpState = JumpState.Grounded;
        [SerializeField] protected bool stopJump;
        /*internal new*/ public Collider2D collider2d;
        /*internal new*/ public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        [SerializeField] private bool jump;
        [SerializeField] protected Vector2 move;
        [SerializeField] protected Vector2 mousePosition;
        [SerializeField] protected Rigidbody2D rb;
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] internal Animator animator;
        [SerializeField] private Transform aimTransform;

        [Tooltip("How far is the gun from the player?")]
        [SerializeField] private float gunDistance = 0.2f;

        private readonly PlatformerModel _model = GetModel<PlatformerModel>();

        protected InputAction moveAction;
        protected InputAction jumpAction;
        private InputAction _lookAction;

        protected bool skipFirstDependency;
        protected Camera _camera;

        public Bounds Bounds => collider2d.bounds;

        protected virtual void Awake()
        {
            _camera = Camera.main;
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            aimTransform = transform.Find("Aim");

            moveAction = InputSystem.actions.FindAction("Player/Move");
            jumpAction = InputSystem.actions.FindAction("Player/Jump");
            _lookAction = InputSystem.actions.FindAction("Player/Look");
            
            moveAction.Enable();
            jumpAction.Enable();
            _lookAction.Enable();
        }

        protected override void Update()
        {
            if (!skipFirstDependency)
            {
                if (controlEnabled)
                {
                    AimAtMouse();
                                
                    move.x = moveAction.ReadValue<Vector2>().x;
                    if (jumpState == JumpState.Grounded && jumpAction.WasPressedThisFrame())
                        jumpState = JumpState.PrepareToJump;
                    else if (jumpAction.WasReleasedThisFrame())
                    {
                        stopJump = true;
                        Schedule<PlayerStopJump>().player = this;
                    }
                }
                else
                {
                    move.x = 0;
                }
                UpdateJumpState();
            }
            
            base.Update();
        }

        protected void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * _model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y *= _model.jumpDeceleration;
                }
            }
            UpdateAnimation();

            targetVelocity = move * maxSpeed;
        }

        protected void UpdateAnimation()
        {
            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }

        protected override void OnEnable()
        {
            _lookAction.performed += OnLook;
            
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            _lookAction.performed -= OnLook;
            
            base.OnDisable();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            mousePosition = context.ReadValue<Vector2>();
        }

        protected virtual void AimAtMouse()
        {
            var worldPosition = _camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, _camera.nearClipPlane));
            var aimDirection = (worldPosition - transform.position).normalized;
            var angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            
            aimTransform.eulerAngles = new Vector3(0, 0, angle);
            float directionX = worldPosition.x - transform.position.x;
            float aimThreshold = 0.1f;
            
            spriteRenderer.flipX = directionX < -aimThreshold;
            
            aimTransform.localPosition = directionX < -aimThreshold ? new Vector3(-gunDistance, aimTransform.localPosition.y, aimTransform.localPosition.z) : // Move left
                new Vector3(gunDistance, aimTransform.localPosition.y, aimTransform.localPosition.z); // Move right
        }
    }
}