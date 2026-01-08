using System;
using UnityEngine;

public class TVTrigger : MonoBehaviour
{
    [SerializeField] private Animator animator;
    
    public bool IsShot { get; private set; }
    
    private Collider2D collider;

    private static readonly int Target = Animator.StringToHash("target");
    private static readonly int WasShot = Animator.StringToHash("shot");

    private void Awake()
    {
        collider = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        var bullet = other.gameObject.GetComponent<BulletMovement>();

        if (bullet)
        {
            MarkShot();
        }
    }
    
    internal void SetShootable(bool shootable)
    {
        collider.enabled = shootable && !IsShot;
        animator.SetBool(Target, shootable && !IsShot);
    }

    private void MarkShot()
    {
        if (IsShot) return;
        
        IsShot = true;
        collider.enabled = false;
        animator.SetBool(Target, false);
        animator.SetTrigger(WasShot);
    }
}
