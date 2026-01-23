using System;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentAmmoText;
    [SerializeField] private TextMeshProUGUI totalAmmoText;
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private GameObject pausePanel;

    [Space] 
    [SerializeField] private AudioClip[] clips;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
    }

    internal void PauseGame()
    {
        Time.timeScale = 0;
        Cursor.visible = true;
        pausePanel.SetActive(true);
        //var hotspot = new Vector2(defaultCursor.width / 2, defaultCursor.height / 2);
        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
    }

    internal void ResumeGame()
    {
        Time.timeScale = 1;
        Cursor.visible = false;
        pausePanel.SetActive(false);
    }

    public void Restart()
    {
        SceneManager.LoadScene(2);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Application.Quit();
    }
    
    internal void UpdateAmmoCount(float chambered, float unchambered)
    {
        currentAmmoText.text = $"{chambered}";
        totalAmmoText.text = $"{unchambered}";
    }

    public void PlaySound(int type)
    {
        switch (type)
        {
            case 0:
                // button click
                _source.PlayOneShot(clips[0]);
                break;
        }
    }
}
