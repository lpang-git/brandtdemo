using System;
using UnityEngine;

public class TVTrigger : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private int monitorNumber;
    
    public bool IsShot { get; private set; }
    
    private BossController _bossController;
    private Collider2D _collider;

    private static readonly int Target = Animator.StringToHash("target");
    private static readonly int WasShot = Animator.StringToHash("shot");

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _bossController = GetComponentInParent<BossController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.gameObject.GetComponent<BulletMovement>();

        if (bullet != null)
        {
            MarkShot();
        }
    }
    
    internal void SetShootable(bool shootable)
    {
        _collider.enabled = shootable && !IsShot;
        animator.SetBool(Target, shootable && !IsShot);
    }

    internal void MarkShot()
    {
        if (IsShot) return;
        
        IsShot = true;
        _collider.enabled = false;
        animator.SetBool(Target, false);
        animator.SetTrigger(WasShot);
        _bossController.CallMonitorShot();
    }
}
