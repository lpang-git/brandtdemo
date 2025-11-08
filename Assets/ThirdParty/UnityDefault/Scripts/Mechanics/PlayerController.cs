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
        [SerializeField] private bool stopJump;
        /*internal new*/ public Collider2D collider2d;
        /*internal new*/ public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        [SerializeField] private bool jump;
        [SerializeField] private Vector2 move;
        [SerializeField] private Vector2 mousePosition;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] internal Animator animator;
        [SerializeField] private Transform aimTransform;
        
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _lookAction;

        public Bounds Bounds => collider2d.bounds;

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            aimTransform = transform.Find("Aim");

            _moveAction = InputSystem.actions.FindAction("Player/Move");
            _jumpAction = InputSystem.actions.FindAction("Player/Jump");
            _lookAction = InputSystem.actions.FindAction("Player/Look");
            
            _moveAction.Enable();
            _jumpAction.Enable();
            _lookAction.Enable();
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                AimAtMouse();
                
                move.x = _moveAction.ReadValue<Vector2>().x;
                if (jumpState == JumpState.Grounded && _jumpAction.WasPressedThisFrame())
                    jumpState = JumpState.PrepareToJump;
                else if (_jumpAction.WasReleasedThisFrame())
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
            base.Update();
        }

        void UpdateJumpState()
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
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
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
        
        private void AimAtMouse()
        {
            // Convert mouse position to world coordinates
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));
            
            Vector3 aimDirection = (worldPosition - transform.position).normalized;
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            aimTransform.eulerAngles =  new Vector3(0, 0, angle);
        }
    }
}