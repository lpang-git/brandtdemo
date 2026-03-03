using Platformer.Core;
using UnityEngine;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentAmmoText;
    [SerializeField] private TextMeshProUGUI totalAmmoText;
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider volumeSlider;

    [Space] 
    [SerializeField] private AudioClip[] clips;

    private AudioSource _source;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
    }

    private void Start()
    {
        volumeSlider.onValueChanged.AddListener(SetVolume);
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Simulation.Clear();
        ResumeGame();
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Application.Quit();
    }
    
    private void SetVolume(float sliderValue)
    {
        sliderValue = Mathf.Clamp(sliderValue, 0, 10);

        var dBValue = sliderValue switch
        {
            >= 2 => -20 + ((sliderValue - 2) * 2),
            1 => -30,
            _ => -80
        };

        audioMixer.SetFloat("MasterVolume", dBValue);
        
        var volume = Mathf.Pow(10, dBValue / 20);
        PlayerPrefs.SetFloat("Master", volume);

        // Log the volume for debugging
        Debug.Log("Setting AudioMixer volume to: " + dBValue + " dB");
    }

    
    internal void UpdateAmmoCount(float chambered, float unchambered)
    {
        currentAmmoText.text = $"{chambered}";
        totalAmmoText.text = $"{unchambered}";
    }

    public void PlaySound(int type)
    {
        /*switch (type)
        {
            case 0:
                // button click
                _source.Play(clips[0]);
                break;
        }*/
    }
}
