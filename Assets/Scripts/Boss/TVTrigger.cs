using System;
using UnityEngine;

public class TVTrigger : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject explosion;
    
    public bool IsShot { get; private set; }
    
    private Collider2D _collider;

    private static readonly int Target = Animator.StringToHash("target");
    private static readonly int WasShot = Animator.StringToHash("shot");
    private static readonly int Explode = Animator.StringToHash("explode");

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.gameObject.GetComponent<BulletMovement>();

        if (bullet != null)
        {
            MarkShot();
            explosion.GetComponent<Animator>().SetTrigger(Explode);
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
    }
}
