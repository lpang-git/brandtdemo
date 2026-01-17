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
    [SerializeField] private GameObject ammoSaverPrefab;
    
    [Space]
    [Header("Settings")]
    
    [Tooltip("Cooldown for shooting in seconds")]
    [SerializeField] private float shootCooldown = 0.4f;
    
    [Tooltip("Cooldown (in seconds) in which the player cannot move after they have pressed aim.")]
    [SerializeField] private float gunAimCooldown = 1f;
    
    [Tooltip("Cooldown (in seconds) in which the player cannot move after they have pressed reload.")]
    [SerializeField] private float reloadCooldown = 1.25f;
    
    [Tooltip("Maximum amount of bullets possible in chamber")]
    [SerializeField] private int maxAmmo = 3;
    
    [Tooltip("Only in case levels are skipped. This value is used to assume the player has a certain amount of bullet at start of the corresponding level.")]
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

    private bool _canShoot;
    
    private void Awake()
    {
        if (AmmoSaver.Instance == null)
        {
            Instantiate(ammoSaverPrefab);
        }
        
        _playerScript = player.GetComponent<PlayerController>();
        fireAction = InputSystem.actions.FindAction("Player/Attack");
        reloadAction = InputSystem.actions.FindAction("Player/Reload");
        aimAction =  InputSystem.actions.FindAction("Player/Aim");
        _countingUI = userInterfaceObject.GetComponent<PlayerUI>();
        
        currentChamberedAmmo = AmmoSaver.Instance != null ? AmmoSaver.Instance.chambered : startingAmmo;
        currentUnchamberedAmmo = AmmoSaver.Instance != null ? AmmoSaver.Instance.unchambered : 0;
        _countingUI.UpdateAmmoCount(currentChamberedAmmo, currentUnchamberedAmmo);
    }

    private void Start()
    {
        Cursor.visible = false;
        _canShoot = true;
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
        
        if (!_canShoot)
            return;
        
        if (Time.time >= nextShootTime && currentChamberedAmmo > 0)
        {
            Instantiate(bulletPrefab, firePointTransform.position, firePointTransform.rotation);
            currentChamberedAmmo--;
            nextShootTime = Time.time + shootCooldown;
            
            SaveAmmoToManager();
            _countingUI.UpdateAmmoCount(currentChamberedAmmo, currentUnchamberedAmmo);

            StartCoroutine(LockActionsForDuration(shootCooldown));
            StartCoroutine(LockShootForDuration(shootCooldown));
            
            TriggerSoundAndAnimation(1);
            
            Debug.Log("Bullet count (chambered): "  + currentChamberedAmmo);
        }
        else if (currentChamberedAmmo == 0)
        {
            Debug.Log("Out of Ammo! Reloading automatically.");
            OnGunEmpty();
        }
    }

    private System.Collections.IEnumerator LockActionsForDuration(float duration)
    {
        _playerScript.controlEnabled = false;
        yield return new WaitForSeconds(duration);
        _playerScript.controlEnabled = true;
    }
    
    private System.Collections.IEnumerator LockShootForDuration(float duration)
    {
        _canShoot = false;
        yield return new WaitForSeconds(duration);
        _canShoot = true;
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

        StartCoroutine(LockActionsForDuration(reloadCooldown));
        StartCoroutine(LockShootForDuration(reloadCooldown));
        
        // Calculate how many bullets needed to fill chamber
        var needed = maxAmmo - currentChamberedAmmo;
        if (needed > 0 && currentUnchamberedAmmo > 0)
        {
            var transfer = Mathf.Min(needed, currentUnchamberedAmmo);
            currentChamberedAmmo += transfer;
            currentUnchamberedAmmo -= transfer;
        }
        SaveAmmoToManager();
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
        
        StartCoroutine(LockActionsForDuration(gunAimCooldown));
        
        _playerScript.PlaySoundAndAnims(5);
    }

    internal void OnAmmoPickedUp(int ammoNum)
    {
        currentUnchamberedAmmo += ammoNum;
        SaveAmmoToManager();
        _countingUI.UpdateAmmoCount(currentChamberedAmmo, currentUnchamberedAmmo);
    }
    
    private void SaveAmmoToManager()
    {
        if (AmmoSaver.Instance == null) return;
        AmmoSaver.Instance.chambered = currentChamberedAmmo;
        AmmoSaver.Instance.unchambered = currentUnchamberedAmmo;
    }

    internal void EnableGunControls()
    {
        fireAction.performed += OnShoot;
        reloadAction.performed += OnReload;
        aimAction.performed += OnAim;
        fireAction.Enable(); reloadAction.Enable(); aimAction.Enable();
    }

    internal void DisableGunControls()
    {
        fireAction.performed -= OnShoot;
        reloadAction.performed -= OnReload;
        aimAction.performed -= OnAim;
        fireAction.Disable(); reloadAction.Disable(); aimAction.Disable();
    }
}