using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private GameObject leftFire;
    [SerializeField] private GameObject rightFire;
    [SerializeField] private GameObject debrisObject;
    
    [Space]
    [SerializeField] private GameObject[] leftArmColliders;
    [Space]
    [SerializeField] private GameObject[] rightArmColliders;
    [Space]
    [SerializeField] private TVTrigger[] monitors;
    
    [Space]
    [Header("Attack Timing")]
    [SerializeField] private float minWait = 4f;
    [SerializeField] private float maxWait = 8f;
    [SerializeField] private float firstDelayUntilAttack = 10f;
    
    [Space]
    [Header("Monitor Timing")]
    [SerializeField] private float minCooldown = 12f;
    [SerializeField] private float maxCooldown = 18f;
    [SerializeField] private float firstDelayUntilMonitorVulnerable = 16f;
    [SerializeField] private float cooldownOnMonitorDown = 20f;
    
    private static readonly int LeftAttack = Animator.StringToHash("leftAttack");
    private static readonly int RightAttack = Animator.StringToHash("rightAttack");
    private static readonly int OverheadAttack = Animator.StringToHash("roofAttack");
    private static readonly int Ignite = Animator.StringToHash("ignite");
    private static readonly int FirstDebris = Animator.StringToHash("one");
    private static readonly int SecondDebris = Animator.StringToHash("two");
    private static readonly int ThirdDebris = Animator.StringToHash("three");
    
    private Animator _animator;
    private Animator _lFAnimator;
    private Animator _rFAnimator;

    private GameObject _previousBox;
    
    private enum Phase { Head, Body, Dead }
    private Phase phase = Phase.Head;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _lFAnimator = leftFire.GetComponent<Animator>();
        _rFAnimator = rightFire.GetComponent<Animator>();
    }

    private void OnEnable()
    {
        StartCoroutine(AttackRoutine());
        StartCoroutine(MonitorRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
    
    private void Die()
    {
        // boss death logic
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
            var choice = Random.Range(0, 3);
            var dAnim = debrisObject.GetComponent<Animator>();
            
            DestroyPreviousAmmoBox();
            
            switch (choice)
            {
                case 0: dAnim.SetTrigger(FirstDebris); break;
                case 1: dAnim.SetTrigger(SecondDebris); break;
                case 2: dAnim.SetTrigger(ThirdDebris); break;
            }
        }

        internal void SpawnAmmoBox(GameObject ammoBox, Transform point)
        {
            _previousBox = Instantiate(ammoBox, point.position, point.rotation);
        }

        private void DestroyPreviousAmmoBox()
        {
            if (_previousBox != null)
            {
                Destroy(_previousBox);
            }
        }

    #endregion

    #region Monitors & Health Manager
        
        private IEnumerator MonitorRoutine()
        {
            yield return new WaitForSeconds(firstDelayUntilMonitorVulnerable);

            while (phase != Phase.Dead)
            {
                var shootables = GetShootablesForCurrentPhase();
                if (shootables.Count == 0)
                {
                    AdvancePhase();
                    yield return null;
                    continue;
                }

                var choiceIndex = shootables[Random.Range(0, shootables.Count)];
                TurnOnMonitor(choiceIndex);

                var randomCooldown = Random.Range(minCooldown, maxCooldown);
                yield return new WaitForSeconds(randomCooldown);

                var wasShot = monitors[choiceIndex].IsShot;

                if (!wasShot)
                    TurnOffMonitor(choiceIndex);

                // use different cooldown depending on whether it was shot
                var postCooldown = wasShot ? cooldownOnMonitorDown - randomCooldown : 0;
                yield return new WaitForSeconds(postCooldown);
            }
        }
    
        private List<int> GetShootablesForCurrentPhase()
        {
            // 0 = upLeftHead, 1 = upRightHead, 2 = downLeftHead, 3 = downRightHead, 4 = leftArm, 5 = body, 6 = rightLeg
            var start = (phase == Phase.Head) ? 0 : 4;
            var end = (phase == Phase.Head) ? 3 : 6;
            var list = new List<int>();
            for (var i = start; i <= end; i++)
                if (!monitors[i].IsShot)
                    list.Add(i);
            return list;
        }
        
        private void AdvancePhase()
        {
            switch (phase)
            {
                case Phase.Head:
                {
                    // if all heads are shot, go to body
                    phase = Phase.Body;
                    // un-target any body monitors so routine can pick them
                    for (var i = 4; i <= 6; i++) monitors[i].SetShootable(false);
                    break;
                }
                case Phase.Body:
                    phase = Phase.Dead;
                    Die();
                    break;
            }
        }
        
        private void TurnOnMonitor(int choice)
        {
            monitors[choice].SetShootable(true);
        }

        private void TurnOffMonitor(int choice)
        {
            monitors[choice].SetShootable(false);
        }

    #endregion
}
