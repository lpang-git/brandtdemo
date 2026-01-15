using System;
using System.Collections;
using UnityEngine;

public class AmmoPickUpTrigger : MonoBehaviour
{
    [SerializeField] internal int ammoNum;
    [SerializeField] internal Animator ammoAnimator;
    
    private static readonly int Opened = Animator.StringToHash("Opened");

    internal void OnPickedUp()
    {
        ammoAnimator.SetTrigger(Opened);

        StartCoroutine(WaitForDestroy());
    }
    
    private IEnumerator WaitForDestroy()
    {
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }
}
