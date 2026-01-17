using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentAmmoText;
    [SerializeField] private TextMeshProUGUI totalAmmoText;

    internal void PauseGame()
    {
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.SetCursor(null, Vector2.zero, default);
    }
    
    internal void UpdateAmmoCount(float chambered, float unchambered)
    {
        currentAmmoText.text = $"{chambered}";
        totalAmmoText.text = $"{unchambered}";
    }
}
