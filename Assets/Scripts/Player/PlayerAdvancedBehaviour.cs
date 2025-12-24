using Platformer.Mechanics;
using UnityEngine;

public class PlayerAdvancedBehaviour : PlayerController
{
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
            if (!_isLadder) return; // Vertical movement
            
            LadderMovement(inputVector);
                
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
            base.ComputeVelocity();
        }
        velocity.x = move.x * playerSpeed;
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

    protected override void AimAtMouse()
    {
        base.AimAtMouse();
        
        // Player flips based on mouse position
        
        var worldPosition = cameraMain.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cameraMain.nearClipPlane));
        var aimThreshold = 0.1f;
        var directionX = worldPosition.x - transform.position.x;

        if (directionX > aimThreshold)
            spriteRenderer.flipX = false;
        else if (directionX < -aimThreshold)
            spriteRenderer.flipX = true;
    }
}
