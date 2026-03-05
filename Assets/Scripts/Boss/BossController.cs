using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class BossController : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private GameObject leftFire;
    [SerializeField] private GameObject rightFire;
    [SerializeField] private GameObject debrisObject;
    [SerializeField] private AudioSource music;
    [SerializeField] private Animator fade;
    
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
    [SerializeField] private float introAttackDelay = 8f;
    
    [Space]
    [Header("Monitor Timing")]
    [SerializeField] private float minCooldown = 8f;
    [SerializeField] private float maxCooldown = 15f;
    [SerializeField] private float introMonitorDelay = 8f;
    [SerializeField] private float destroyedMonitorCooldown = 5f;

    public bool instantKill;
    
    private static readonly int LeftAttack = Animator.StringToHash("leftAttack");
    private static readonly int RightAttack = Animator.StringToHash("rightAttack");
    private static readonly int OverheadAttack = Animator.StringToHash("roofAttack");
    private static readonly int Ignite = Animator.StringToHash("ignite");
    private static readonly int FirstDebris = Animator.StringToHash("one");
    private static readonly int SecondDebris = Animator.StringToHash("two");
    private static readonly int ThirdDebris = Animator.StringToHash("three");
    private static readonly int Fade = Animator.StringToHash("fadeStart");
    
    private Animator _animator;
    private Animator _lFAnimator;
    private Animator _rFAnimator;
    private CinemachineImpulseSource _impulseSource;

    private bool _bossIntro = true;
    private bool _died = false;
    private float _attackCooldown;
    private bool _isAttacking;
    
    private float _monitorChangeCooldown;
    private List<int> _shootables = new();
    private int _currentMonitor;
    private bool _shotCalled;

    private float _time;

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

    private void Start()
    {
        PlaySoundAndShake(0);
    }

    private void Update()
    {
        _time += Time.deltaTime;
        
        if (instantKill)
        {
            Debug_KillAllMonitors();
        }
        
        if (_bossIntro)
        {
            introAttackDelay += _time;
            introMonitorDelay += _time;
            _bossIntro = false;
        }
        
        if (phase == Phase.Dead)
        {
            if (_died) { return; }
            
            _died = true;
            StartCoroutine(Die());
        }
        else
        {
            // Management of: Attacks
            CheckTimeUntilNextAttack();
            
            // Management of: Monitors
            _shootables = GetAndSetShootables();
            if (_shootables.Count == 0) { AdvanceBossPhase(); }
            CheckTimeUntilMonitorChange();
            CheckMonitorStatus(false); // constant check on if monitor is still active
        }
    }

    private IEnumerator Die()
    {
        Debug.Log("Event: Death.");
        music.Stop();
        fade.SetTrigger(Fade);
        PlaySoundAndShake(7);
        _animator.enabled = false;
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(4);
    }

    #region Attacks
    
        private void CheckTimeUntilNextAttack()
        {
            if (_time >= _attackCooldown)
            {
                _isAttacking = false;
            }
            
            if (_isAttacking) return;
            if (!(_time >= introAttackDelay)) return;
            
            var cooldownRandom = Random.Range(minWait, maxWait);
            _attackCooldown = _time + cooldownRandom;
            _isAttacking = true;
            Debug.Log("Event: Attacking.");
            StartAttack();
        }
    
        private void StartAttack()
        {
            if (_died) { return; }
        
            // 0 = left, 1 = right, 2 = overhead
            var choice = Random.Range(0, 3);

            switch (choice)
            {
                case 0: _animator.SetTrigger(LeftAttack); break;
                case 1: _animator.SetTrigger(RightAttack); break;
                case 2: _animator.SetTrigger(OverheadAttack); break;
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

        private void SetMonitorShootable()
        {
            if (_died) { return; }
            
            Debug.Log("Event: Monitor set.");
            // choose random monitor that is available in respective phase
            _currentMonitor = _shootables[Random.Range(0, _shootables.Count)];
            TurnOnMonitor(_currentMonitor);
            _shotCalled = false;
        }
        
        private void CheckTimeUntilMonitorChange()
        {
            if (!(_time >= introMonitorDelay)) return;
            if (!(_time >= _monitorChangeCooldown)) return;
        
            Debug.Log("Event: Monitor Change.");
            CheckMonitorStatus(true);
            var cooldownRandom = Random.Range(minCooldown, maxCooldown);
            _monitorChangeCooldown = _time + cooldownRandom;
            SetMonitorShootable();
        }
        
        private void CheckMonitorStatus(bool isChangingTime)
        {
            var monitorWasShot = monitors[_currentMonitor].IsShot;

            if (isChangingTime) // called when monitor hasn't been shot before cooldown end
            {
                if (monitorWasShot)
                {
                    Debug.Log("Monitor was turned off due to shot, return.");
                    return;
                }
                Debug.Log("Event: Monitor reset.");
                TurnOffMonitor(_currentMonitor);
            }
            else
            {
                if (!monitorWasShot || _shotCalled) return;
                Debug.Log("Event: Monitor was shot.");
                CallMonitorShot();
                TurnOffMonitor(_currentMonitor);
                _monitorChangeCooldown = _time + destroyedMonitorCooldown;
            }
        }
        
        private List<int> GetAndSetShootables()
        {
            var start = (phase == Phase.Head) ? 0 : 4;
            var end = (phase == Phase.Head) ? 3 : 6;
            var list = new List<int>();
            for (var i = start; i <= end; i++)
                if (!monitors[i].IsShot)
                    list.Add(i);
            return list;
        }
        
        private void AdvanceBossPhase()
        {
            switch (phase)
            {
                case Phase.Head:
                {
                    // Check if all head monitors are shot
                    var allHeadsShot = true;
                    for (var i = 0; i <= 3; i++)
                    {
                        if (monitors[i].IsShot) continue;
                        allHeadsShot = false; 
                        break;
                    }
            
                    if (allHeadsShot)
                    {
                        Debug.Log("Event: Advancing to body phase...");
                        phase = Phase.Body;
                    }
                    break;
                }
                case Phase.Body:
                {
                    // Check if all body monitors are shot
                    var allBodyShot = true;
                    for (var i = 4; i <= 6; i++)
                    {
                        if (monitors[i].IsShot) continue;
                        allBodyShot = false; 
                        break;
                    }

                    if (allBodyShot)
                    {
                        Debug.Log("Event: Advancing to death phase...");
                        phase = Phase.Dead;
                    }
                    break;
                }
            }
        }
        
        private void TurnOnMonitor(int choice)
        {
            monitors[choice].SetShootable(true);

            for (var i = 0; i <= 3; i++)
            {
                if (i == choice) continue;
                
                EnableCollider(i);
            }
            DisableCollider(choice);
        }
        
        private void TurnOffMonitor(int choice)
        {
            monitors[choice].SetShootable(false);
        }
        
        private void EnableCollider(int index)
        {
            Debug.Log("index called: "+index);
            monitorColliders[index]?.SetActive(true);
        }

        private void DisableCollider(int index)
        {
            Debug.Log("index called: "+index);
            monitorColliders[index]?.SetActive(false);
        }

        internal void CallMonitorShot()
        {
            PlaySoundAndShake(6);
            _shotCalled = true;
        }

        private void Debug_KillAllMonitors()
        {
            foreach (var monitor in monitors)
            {
                monitor.SetShootable(true);
                monitor.MarkShot();
            }
        }

    #endregion
    
    internal void PlaySoundAndShake(int actionId)
    { 
        switch (actionId) 
        {
            case 0:
                // intro
                _impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Rumble;
                _impulseSource.ImpulseDefinition.ImpulseDuration = 0.85f;
                CameraShaker.instance.CameraShake(_impulseSource);
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
                // monitor vulnerable
                sounds[5].Play();
                break;
            case 6:
                // monitor destroyed
                sounds[6].Play();
                break;
            case 7:
                // death
                sounds[7].Play(); 
                break;        
        }
    }
}
