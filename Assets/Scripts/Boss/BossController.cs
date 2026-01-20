using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
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
    [Tooltip("0 = upLeftHead, 1 = upRightHead, 2 = downLeftHead, 3 = downRightHead, 4 = leftArm, 5 = body, 6 = rightLeg")]
    [SerializeField] private TVTrigger[] monitors;
    [Space]
    [Tooltip("0 = upLeftHead, 1 = upRightHead, 2 = downLeftHead, 3 = downRightHead")]
    [SerializeField] private GameObject[] monitorColliders;

    [Space] 
    [SerializeField] private AudioSource[] sounds;
    
    [Space]
    [Header("Attack Timing")]
    [SerializeField] private float minWait = 4f;
    [SerializeField] private float maxWait = 8f;
    [SerializeField] private float firstDelayUntilAttack = 10f;
    
    [Space]
    [Header("Monitor Timing")]
    [SerializeField] private float minCooldown = 8f;
    [SerializeField] private float maxCooldown = 15f;
    [SerializeField] private float firstDelayUntilMonitorVulnerable = 16f;
    [SerializeField] private float cooldownOnMonitorDown = 15f;
    
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
    private CinemachineImpulseSource _impulseSource;

    private GameObject _previousBox;
    
    private enum Phase { Head, Body, Dead }
    private Phase phase = Phase.Head;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _lFAnimator = leftFire.GetComponent<Animator>();
        _rFAnimator = rightFire.GetComponent<Animator>();
        _impulseSource = GetComponent<CinemachineImpulseSource>();
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
            PlaySoundAndShake(2);
        }
        
        private void OnRightHandSmash()
        {
            PlaySoundAndShake(3);
        }
        
        private void OnRoofSmashed()
        {
            DestroyPreviousAmmoBox();
            PlaySoundAndShake(4);
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

            switch (choice)
            {
                // if shootable monitor is left-sided (0 or 2), disable left monitor colliders
                case 0 or 2:
                    SetMonitorColliders(leftSide: true, isEnabled: false);
                    break;
                // if shootable monitor is right-sided (1 or 3), disable right monitor colliders
                case 1 or 3:
                    SetMonitorColliders(leftSide: false, isEnabled: false);
                    break;
            }
        }

        private void TurnOffMonitor(int choice)
        {
            monitors[choice].SetShootable(false);

            switch (choice)
            {
                // re-enable colliders
                case 0 or 2:
                    SetMonitorColliders(leftSide: true, isEnabled: true);
                    break;
                case 1 or 3:
                    SetMonitorColliders(leftSide: false, isEnabled: true);
                    break;
            }
        }
        
        private void SetMonitorColliders(bool leftSide, bool isEnabled)
        {
            if (monitorColliders == null || monitorColliders.Length < 4) return;
            // left side: 0 (upLeftHead), 2 (downLeftHead)
            // right side: 1 (upRightHead), 3 (downRightHead)
            var side = leftSide ? new[] { 0, 2 } : new[] { 1, 3 };
            foreach (var i in side)
            {
                if (monitorColliders[i] != null)
                    monitorColliders[i].SetActive(isEnabled);
            }
        }

    #endregion
    
    internal void PlaySoundAndShake(int actionId)
    {
       switch (actionId)
        {
            case 0:
                // intro
                sounds[0].Play();
                break;
            case 1:
                // idle sound
                sounds[1].Play();
                break;
            case 2:
                // left attack hit
                _lFAnimator.SetTrigger(Ignite);
                sounds[2].Play();
                _impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
                _impulseSource.ImpulseDefinition.ImpulseDuration = 0.5f;
                CameraShaker.instance.CameraShake(_impulseSource);
                break;
            case 3:
                // right attack hit
                _rFAnimator.SetTrigger(Ignite);
                sounds[3].Play();
                _impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
                _impulseSource.ImpulseDefinition.ImpulseDuration = 0.3f;
                CameraShaker.instance.CameraShake(_impulseSource);
                break;
            case 4:
                // overhead attack hit
                var choice = Random.Range(0, 3);
                var dAnim = debrisObject.GetComponent<Animator>();
            
                switch (choice)
                {
                    case 0: dAnim.SetTrigger(FirstDebris); break;
                    case 1: dAnim.SetTrigger(SecondDebris); break;
                    case 2: dAnim.SetTrigger(ThirdDebris); break;
                }
                sounds[4].Play();
                _impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
                _impulseSource.ImpulseDefinition.ImpulseDuration = 0.6f;
                CameraShaker.instance.CameraShake(_impulseSource);
                break;
            case 5:
                // monitor on head destroyed
                sounds[5].Play();
                break;
            case 6:
                // monitor on body destroyed
                sounds[6].Play();
                break;
            case 7:
                // death
                sounds[7].Play();
                break;
        }
    }
}
