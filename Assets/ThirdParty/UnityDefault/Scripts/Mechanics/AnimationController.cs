using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// AnimationController integrates physics and animation. It is generally used for simple enemy animation.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class AnimationController : KinematicObject
    {
        private static readonly int VelocityX = Animator.StringToHash("velocityX");
        
        [SerializeField] internal float maxSpeed = 1;
        
        internal Vector2 move;

        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        protected virtual void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
        }

        protected override void ComputeVelocity()
        {
            _spriteRenderer.flipX = move.x switch
            {
                > 0.01f => false,
                < -0.01f => true,
                _ => _spriteRenderer.flipX
            };
            
            _animator.SetFloat(VelocityX, Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }
    }
}