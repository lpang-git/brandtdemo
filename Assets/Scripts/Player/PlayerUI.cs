using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentAmmoText;
    [SerializeField] private TextMeshProUGUI totalAmmoText;
    
    internal void UpdateAmmoCount(float currentAmmo, float totalAmmo)
    {
        currentAmmoText.text = $"{currentAmmo}";
        totalAmmoText.text = $"{totalAmmo}";
    }
}
