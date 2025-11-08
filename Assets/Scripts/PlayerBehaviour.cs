using Platformer.Gameplay;
using Platformer.Mechanics;
using UnityEngine;
using static Platformer.Core.Simulation;

public class PlayerBehaviour : PlayerController
{
    [SerializeField] private float climbingSpeed = 2;
    
    private bool _isLadder  = false;
    private bool _canClimbUp;
    private bool _canClimbDown;

    protected override void Update()
    {
        if (controlEnabled)
        {
            AimAtMouse();
            
            var inputVector = moveAction.ReadValue<Vector2>();
            
            move.x = inputVector.x; // Horizontal movement
            
            skipFirstDependency = true;
            base.Update();
            
            CheckLadderCollisions();
            if (_isLadder) // Vertical movement
            {
                LadderMovement(inputVector);
                return; // avoid jumping checks
            }

            // Jump Logic
            if (jumpState == JumpState.Grounded && jumpAction.WasPressedThisFrame())
            {
                jumpState = JumpState.PrepareToJump;
            }
            else if (jumpAction.WasReleasedThisFrame())
            {
                stopJump = true;
                Schedule<PlayerStopJump>().player = this;
            }

            UpdateJumpState();
        }
        else
        {
            move.x = 0;
        }
    }

    private void LadderMovement(Vector2 inputVector)
    {
        if (inputVector.y > 0 && _canClimbUp)
        {
            rb.position += new Vector2(0, climbingSpeed * Time.deltaTime); // Climbing up
        }
        else if (inputVector.y < 0 && _canClimbDown)
        {
            rb.position += new Vector2(0, -climbingSpeed * Time.deltaTime); // Climbing down
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
            UpdateAnimationAndSprite();
            targetVelocity = move * maxSpeed;
        }
        else
        {
            base.ComputeVelocity();
        }
        velocity.x = move.x * maxSpeed;
    }
    
    private void CheckLadderCollisions()
    {
        Vector2 boxCastSize = new Vector2(0.1f, 0.1f);
        Vector2 positionAbove = rb.position + new Vector2(0, boxCastSize.y / 2);
        Vector2 positionBelow = rb.position - new Vector2(0, boxCastSize.y / 2);

        // Check for colliders above and below
        _canClimbUp = Physics2D.OverlapBox(positionAbove, boxCastSize, 0, LayerMask.GetMask("Default")) == null;
        _canClimbDown = Physics2D.OverlapBox(positionBelow, boxCastSize, 0, LayerMask.GetMask("Default")) == null;
    }
}
