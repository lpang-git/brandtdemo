using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGunBehaviour : MonoBehaviour
{
    /// <summary>
    /// This script handles gun logic and ammo.
    /// </summary>
    
    [Header("Shooting References")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePointTransform;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject userInterfaceObject;
    
    [Space]
    [Header("Settings")]
    
    [Tooltip("Cooldown time in seconds")]
    [SerializeField] private float shootCooldown = 0.4f;
    
    [Tooltip("Maximum amount of bullets possible in chamber")]
    [SerializeField] private int maxAmmo = 3;
    
    [Tooltip("Starting ammo")]
    [SerializeField] private int startingAmmo = 2;
    
    
    [Space]
    [Header("Debug Values")]
    [SerializeField] internal int currentChamberedAmmo;
    [SerializeField] private float nextShootTime;
    [SerializeField] private int totalOwnedAmmo;

    private PlayerController _playerScript;
    
    private InputAction _fireAction;
    private InputAction _reloadAction;

    private PlayerUI _countingUI;
    
    private void Awake()
    {
        _playerScript = player.GetComponent<PlayerController>();
        _fireAction = InputSystem.actions.FindAction("Player/Attack");
        _reloadAction = InputSystem.actions.FindAction("Player/Reload");
        currentChamberedAmmo = startingAmmo;
        totalOwnedAmmo = startingAmmo;
        
        _countingUI = userInterfaceObject.GetComponent<PlayerUI>();
    }

    private void OnEnable()
    {
        _fireAction.Enable();
        _fireAction.performed += OnShoot;
        
        _reloadAction.Enable();
        _reloadAction.performed += Reload;
    }

    private void OnDisable()
    {
        _fireAction.Disable();
        _fireAction.performed -= OnShoot;
        
        _reloadAction.Disable();
        _reloadAction.performed -= Reload;
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        if (Time.time >= nextShootTime && currentChamberedAmmo > 0)
        {
            Instantiate(bulletPrefab, firePointTransform.position, firePointTransform.rotation);
            currentChamberedAmmo--;
            totalOwnedAmmo--;
            nextShootTime = Time.time + shootCooldown; // Set the next shoot time

            _countingUI.UpdateAmmoCount(currentChamberedAmmo, totalOwnedAmmo);
            
            StartCoroutine(LockMovementForDuration(shootCooldown));

            TriggerSoundAndAnimation(1);
            
            Debug.Log("Bullet count:"  + currentChamberedAmmo);
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
    
    private void Reload(InputAction.CallbackContext context)
    {
        InitReload();
    }
    
    private void InitReload()
    {
        TriggerSoundAndAnimation(2);

        switch (currentChamberedAmmo)
        {
            case 0:
                // Chamber is empty, fill it with the maximum allowed amount or what the player has
                currentChamberedAmmo = Mathf.Min(maxAmmo, totalOwnedAmmo);
                break;
            default:
                if (currentChamberedAmmo > 0)
                {
                    // If there's ammo already in the chamber, reload but don't exceed total owned or max ammo
                    currentChamberedAmmo = Mathf.Min(currentChamberedAmmo + maxAmmo, maxAmmo, totalOwnedAmmo);
                }
                else
                {
                    Debug.LogError("Problem with ammo. Check if negative.");
                }
                break;
        }
    }

    private void OnGunEmpty()
    {
        TriggerSoundAndAnimation(3);
        InitReload();
    }
    
    private void TriggerSoundAndAnimation(int fxNum)
    {
        // Shoot = 1, Reload = 2, Empty = 3

        switch (fxNum)
        {
            case 1:
                // play shoot anim & sound
                break;
            case 2:
                // play reload anim & sound
                break;
            case 3:
                // play empty anim & sound
                break;
        }
    }

    internal void OnAmmoPickedUp(int ammoNum)
    {
        totalOwnedAmmo += ammoNum;
        _countingUI.UpdateAmmoCount(currentChamberedAmmo, totalOwnedAmmo);
    }
}
