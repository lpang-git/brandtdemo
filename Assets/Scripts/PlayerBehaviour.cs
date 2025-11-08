using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviour : PlayerController
{
    [SerializeField] private float climbingSpeed;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float gravity;
    
    private bool _isClimbing;
    private bool _isLadder;
    private InputAction _climbAction;

    private void Awake()
    {
        _climbAction = InputSystem.actions.FindAction("Player/Up");
    }

    protected override void Update()
    {
        base.Update();
        
        if (_isLadder && _climbAction.IsPressed())
        {
            _isClimbing = true;
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        if (_isClimbing)
        {
            rb.gravityScale = 0f; 
            rb.linearVelocity = new Vector2(rb.linearVelocityX, climbingSpeed);
        }
        else
        {
            rb.gravityScale = gravity;
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if  (col.CompareTag("Ladder")!) { return; }
        
        _isLadder = true;
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if  (col.CompareTag("Ladder")!) { return; }
        
        _isLadder = false;
        _isClimbing  = false;
    }
}
