using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGunBehaviour : MonoBehaviour
{
    private static readonly int Shoot = Animator.StringToHash("shoot");
    private static readonly int Reload = Animator.StringToHash("reload");
    private static readonly int AimPressed = Animator.StringToHash("aimPressed");
    private static readonly int IsAiming = Animator.StringToHash("isAiming");
    private static readonly int LookState = Animator.StringToHash("lookState");
    
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
    [SerializeField] private float reloadCooldown = 1.5f;
    
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
    
    internal InputAction fireAction, reloadAction, aimAction, lookAction;

    private PlayerUI _countingUI;
    private Animator _animator;

    private Vector2 _mousePosition;
    private Camera _cameraMain;
    private bool _canShoot;
    
    private void Awake()
    {
        if (AmmoSaver.Instance == null)
        {
            Instantiate(ammoSaverPrefab);
        }
        
        _playerScript = player.GetComponent<PlayerController>();
        _animator = _playerScript.animator;
        fireAction = InputSystem.actions.FindAction("Player/Attack");
        reloadAction = InputSystem.actions.FindAction("Player/Reload");
        aimAction =  InputSystem.actions.FindAction("Player/Aim");
        lookAction = InputSystem.actions.FindAction("Player/Look");
        _countingUI = userInterfaceObject.GetComponent<PlayerUI>();
        _cameraMain = Camera.main;
        
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
        if (_playerScript._isPaused)
            return;
        
        if (isAiming)
        {
            AimAtMouse();
        }
        
        Cursor.visible = isAiming;
    }

    private void OnEnable()
    {
        EnableGunControls();
        aimAction.canceled += OnAimRelease;
    }

    private void OnDisable()
    {
        DisableGunControls();
        aimAction.canceled -= OnAimRelease;
    }
    
    private void OnAimHold(InputAction.CallbackContext context)
    {
        isAiming = true;
        _playerScript.controlEnabled = false; // lock movement during aiming
        _animator.SetBool(IsAiming, true);
        
        var hotspot = new Vector2(aimCursor.width / 2, aimCursor.height / 2);
        Cursor.SetCursor(aimCursor, hotspot, CursorMode.Auto);

        TriggerSoundAndAnimation(4);
    }

    private void OnAimRelease(InputAction.CallbackContext context)
    {
        isAiming = false;
        _playerScript.controlEnabled = true;
        _animator.SetBool(IsAiming, false);
        
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // Reset the cursor
        
        TriggerSoundAndAnimation(5);
    }
    
    private void OnLook(InputAction.CallbackContext context)
    {
        _mousePosition = context.ReadValue<Vector2>();
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

    private System.Collections.IEnumerator LockMovementForDuration(float duration) // used when reloading
    {
        _playerScript.controlEnabled = false;
        yield return new WaitForSeconds(duration);
        _playerScript.controlEnabled = true;
    }
    
    private System.Collections.IEnumerator LockShootForDuration(float duration) // used when auto-reloading or when shooting
    {
        _canShoot = false;
        yield return new WaitForSeconds(duration);
        _canShoot = true;
    }
    
    private System.Collections.IEnumerator LockGunForDuration(float duration) // used when reloading
    {
        DisableGunControls();
        yield return new WaitForSeconds(duration);
        EnableGunControls();
    }
    
    private void OnReload(InputAction.CallbackContext context) // reload pressed
    {
        InitReload();
        StartCoroutine(LockGunForDuration(reloadCooldown));
    }
    
    private void OnGunEmpty() // auto-reload
    {
        TriggerSoundAndAnimation(3);
        InitReload();
        StartCoroutine(LockShootForDuration(reloadCooldown));
    }
    
    private void InitReload()
    {
        TriggerSoundAndAnimation(2);

        StartCoroutine(LockMovementForDuration(reloadCooldown));
        
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
    
    private void AimAtMouse()
    {
        var worldPosition = _cameraMain.ScreenToWorldPoint(new Vector3(_mousePosition.x, _mousePosition.y, _cameraMain.nearClipPlane));
        var aimDirection = (worldPosition - transform.position).normalized;
        var angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            
        transform.eulerAngles = new Vector3(0, 0, angle);
        var directionX = worldPosition.x - transform.position.x;
            
        var aimThreshold = 0.1f;
        var upThreshold = 0.5f;
        var downThreshold = -0.5f;
            
        if (!isAiming) { return; } 
            
        // Player flips based on mouse position
        _playerScript.spriteRenderer.flipX = directionX < -aimThreshold;
            
        transform.localPosition = new Vector3(0, transform.localPosition.y, transform.localPosition.z); // Move right
            
        // Determine vertical look state using Y difference in world space
        var yDiff = worldPosition.y - transform.position.y;

        int lookState;
        if (yDiff >= upThreshold) lookState = 2;        // look up
        else if (yDiff <= downThreshold) lookState = 0; // look down
        else lookState = 1;                             // center

        _animator.SetInteger(LookState, lookState);
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
            case 4:
                // unholster
                sounds[4].Play();
                break;
            case 5:
                // holster
                sounds[5].Play();
                break;
        }
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
        aimAction.performed += OnAimHold;
        lookAction.performed += OnLook;
        fireAction.Enable(); reloadAction.Enable(); aimAction.Enable(); lookAction.Enable();
    }

    internal void DisableGunControls()
    {
        fireAction.performed -= OnShoot;
        reloadAction.performed -= OnReload;
        aimAction.performed -= OnAimHold;
        lookAction.performed -= OnLook;
        fireAction.Disable(); reloadAction.Disable(); aimAction.Disable(); lookAction.Disable();
    }
}