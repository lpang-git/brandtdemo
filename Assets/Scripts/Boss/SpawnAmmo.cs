using UnityEngine;

public class SpawnAmmo : MonoBehaviour
{
    [SerializeField] private GameObject ammo;
    [SerializeField] private Transform boxPoint;

    protected void InstantiateBox()
    {
        Instantiate(ammo, boxPoint.position, boxPoint.rotation);
    }
}
