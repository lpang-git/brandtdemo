using System.Collections;
using Platformer.Mechanics;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private GameObject leftFire;
    [SerializeField] private GameObject rightFire;
    
    [Space]
    [SerializeField] private GameObject[] leftArmColliders;
    [Space]
    [SerializeField] private GameObject[] rightArmColliders;
    [Space]
    [SerializeField] private GameObject[] TVColliders; // 0 = upLeftHead, 1 = lowLeftHead, 2 = upRightHead, 3 = lowRightHead, 4 = leftArm, 5 = body, 6 = rightLeg
    
    [Space]
    [Header("Attack Timing")]
    [SerializeField] private float minWait = 4f;
    [SerializeField] private float maxWait = 8f;
    [SerializeField] private float firstDelayUntilAttack = 10f;
    
    private static readonly int LeftAttack = Animator.StringToHash("leftAttack");
    private static readonly int RightAttack = Animator.StringToHash("rightAttack");
    private static readonly int OverheadAttack = Animator.StringToHash("roofAttack");
    private static readonly int Ignite = Animator.StringToHash("ignite");
    
    private Animator _animator;
    private Animator _lFAnimator;
    private Animator _rFAnimator;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _lFAnimator = leftFire.GetComponent<Animator>();
        _rFAnimator = rightFire.GetComponent<Animator>();
    }

    private void OnEnable()
    {
        StartCoroutine(AttackRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    #region Attacks

        private IEnumerator AttackRoutine()
        {
            yield return new WaitForSeconds(firstDelayUntilAttack);

            while (true)
            {
                // 0 = left, 1 = right, 2 = overhead
                var choice = Random.Range(0, 3);

                switch (choice)
                {
                    case 0: _animator.SetTrigger(LeftAttack); break;
                    case 1: _animator.SetTrigger(RightAttack); break;
                    case 2: _animator.SetTrigger(OverheadAttack); break;
                }
                yield return new WaitForSeconds(Random.Range(minWait, maxWait));
            }
        }
    
        private void OnLeftHandSmash()
        {
            _lFAnimator.SetTrigger(Ignite);
        }
        
        private void OnRightHandSmash()
        {
            _rFAnimator.SetTrigger(Ignite);
        }
        
        private void OnRoofSmashed()
        {
            
        }

    #endregion

    #region Monitors & Health Manager

    

    #endregion
}
