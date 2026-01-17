using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGunBehaviour : MonoBehaviour
{
    private static readonly int Shoot = Animator.StringToHash("shoot");
    private static readonly int Reload = Animator.StringToHash("reload");
    
    [Header("Shooting References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePointTransform;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject userInterfaceObject;
    [SerializeField] private Texture2D aimCursor;
    
    [Space]
    [Header("Settings")]
    
    [Tooltip("Cooldown for shooting in seconds")]
    [SerializeField] private float shootCooldown = 0.4f;
    
    [Tooltip("Cooldown (in seconds) in which the player cannot move after they have pressed aim.")]
    [SerializeField] private float gunAimCooldown = 1f;
    
    [Tooltip("Cooldown (in seconds) in which the player cannot move after they have pressed reload.")]
    [SerializeField] private float reloadCooldown = 1.5f;
    
    [Tooltip("Maximum amount of bullets possible in chamber")]
    [SerializeField] private int maxAmmo = 3;
    
    [Tooltip("Starting ammo in chamber")]
    [SerializeField] private int startingAmmo = 2;
    
    [Space]
    [Header("Audio Sources")]
    [SerializeField] private AudioSource[] sounds;
    
    [Space]
    [Header("Debug Values")]
    [SerializeField] internal int currentChamberedAmmo;
    [SerializeField] internal int currentUnchamberedAmmo;
    [SerializeField] private float nextShootTime;
    [SerializeField] private float nextHolsterTime;
    [SerializeField] internal bool isAiming = false;

    private PlayerController _playerScript;
    
    internal InputAction fireAction;
    internal InputAction reloadAction;
    internal InputAction aimAction;

    private PlayerUI _countingUI;
    
    private void Awake()
    {
        _playerScript = player.GetComponent<PlayerController>();
        fireAction = InputSystem.actions.FindAction("Player/Attack");
        reloadAction = InputSystem.actions.FindAction("Player/Reload");
        aimAction =  InputSystem.actions.FindAction("Player/Aim");
        currentChamberedAmmo = startingAmmo;
        currentUnchamberedAmmo = 0;
        _countingUI = userInterfaceObject.GetComponent<PlayerUI>();
        _countingUI.UpdateAmmoCount(currentChamberedAmmo, currentUnchamberedAmmo);
    }

    private void Start()
    {
        Cursor.visible = false;
    }

    private void Update()
    {
        if (_playerScript.velocity.x != 0 || _playerScript.velocity.y != 0)
        {
            isAiming = false;
        }
        Cursor.visible = isAiming;
    }

    private void OnEnable()
    {
        fireAction.Enable();
        fireAction.performed += OnShoot;
        
        reloadAction.Enable();
        reloadAction.performed += OnReload;
        
        aimAction.Enable();
        aimAction.performed += OnAim;
    }

    private void OnDisable()
    {
        fireAction.Disable();
        fireAction.performed -= OnShoot;
        
        reloadAction.Disable();
        reloadAction.performed -= OnReload;
        
        aimAction.Disable();
        aimAction.performed -= OnAim;
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (!isAiming)
        {
            TriggerSoundAndAnimation(0);
            return;
        }
        
        if (Time.time >= nextShootTime && currentChamberedAmmo > 0)
        {
            Instantiate(bulletPrefab, firePointTransform.position, firePointTransform.rotation);
            currentChamberedAmmo--;
            nextShootTime = Time.time + shootCooldown;

            _countingUI.UpdateAmmoCount(currentChamberedAmmo, currentUnchamberedAmmo);

            StartCoroutine(LockMovementForDuration(shootCooldown));
            StartCoroutine(LockJumpForDuration(shootCooldown));
            
            TriggerSoundAndAnimation(1);
            
            Debug.Log("Bullet count (chambered): "  + currentChamberedAmmo);
        }
        else if (currentChamberedAmmo == 0)
        {
            Debug.Log("Out of Ammo! Reloading automatically.");
            OnGunEmpty();
        }
    }

    private System.Collections.IEnumerator LockMovementForDuration(float duration)
    {
        _playerScript.moveAction.Disable();
        yield return new WaitForSeconds(duration);
        _playerScript.moveAction.Enable();
    }
    
    private System.Collections.IEnumerator LockJumpForDuration(float duration)
    {
        _playerScript.jumpAction.Disable();
        yield return new WaitForSeconds(duration);
        _playerScript.jumpAction.Enable();
    }
    
    private System.Collections.IEnumerator LockShootForDuration(float duration)
    {
        fireAction.Disable();
        yield return new WaitForSeconds(duration);
        fireAction.Enable();
    }
    
    private void OnReload(InputAction.CallbackContext context)
    {
        if (!isAiming)
        {
            TriggerSoundAndAnimation(0);
            return;
        }
        
        InitReload();
    }
    
    private void InitReload()
    {
        TriggerSoundAndAnimation(2);

        StartCoroutine(LockMovementForDuration(reloadCooldown));
        StartCoroutine(LockJumpForDuration(reloadCooldown));
        StartCoroutine(LockShootForDuration(reloadCooldown));
        
        // Calculate how many bullets needed to fill chamber
        var needed = maxAmmo - currentChamberedAmmo;
        if (needed > 0 && currentUnchamberedAmmo > 0)
        {
            var transfer = Mathf.Min(needed, currentUnchamberedAmmo);
            currentChamberedAmmo += transfer;
            currentUnchamberedAmmo -= transfer;
        }
        _countingUI.UpdateAmmoCount(currentChamberedAmmo, currentUnchamberedAmmo);
    }

    private void OnGunEmpty()
    {
        TriggerSoundAndAnimation(3);
        InitReload();
    }
    
    private void TriggerSoundAndAnimation(int fxNum)
    {
        switch (fxNum)
        {
            case 0:
                // not aiming, cannot reload or shoot
                sounds[0].Play();
                break;
            case 1:
                // shoot
                sounds[1].Play();
                player.GetComponent<Animator>().SetTrigger(Shoot);
                break;
            case 2:
                // reload
                sounds[2].Play();
                player.GetComponent<Animator>().SetTrigger(Reload);
                break;
            case 3:
                // empty magazine
                sounds[3].Play();
                break;
        }
    }

    private void OnAim(InputAction.CallbackContext context)
    {
        isAiming = !isAiming;
        Cursor.SetCursor(aimCursor, Vector2.zero, default);
        
        StartCoroutine(LockMovementForDuration(gunAimCooldown));
        StartCoroutine(LockJumpForDuration(gunAimCooldown));
        
        _playerScript.PlaySoundAndAnims(5);
    }

    internal void OnAmmoPickedUp(int ammoNum)
    {
        currentUnchamberedAmmo += ammoNum;
        _countingUI.UpdateAmmoCount(currentChamberedAmmo, currentUnchamberedAmmo);
    }
}