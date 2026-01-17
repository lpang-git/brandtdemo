using UnityEngine;

public class SpawnAmmo : MonoBehaviour
{
    [SerializeField] private GameObject ammo;
    [SerializeField] private Transform boxPoint;
    [SerializeField] private BossController boss;

    protected void InstantiateBox()
    {
        boss.SpawnAmmoBox(ammo, boxPoint);
    }
}
