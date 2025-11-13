using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject levelsPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Levels Panel")]
    [SerializeField] private Button levelsButton;
    [SerializeField] private Button levelsCloseButton;
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;
    
    [Header("Options Panel")]
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button optionsCloseButton;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    [Header("Audio")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    // PlayerPrefs keys
    private const string SFX_KEY = "SFX_VOLUME";
    private const string MUSIC_KEY = "MUSIC_VOLUME";

    private void Start()
    {
        var savedSfx = PlayerPrefs.HasKey(SFX_KEY) ? PlayerPrefs.GetFloat(SFX_KEY) : 0.75f;
        var savedMusic = PlayerPrefs.HasKey(MUSIC_KEY) ? PlayerPrefs.GetFloat(MUSIC_KEY) : 0.75f;

        sfxSlider.value = savedSfx;
        musicSlider.value = savedMusic;
        ApplySfxVolume(savedSfx);
        ApplyMusicVolume(savedMusic);
        
        // Disable optionsPanel in case it was previously opened
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        startButton.onClick.AddListener(OnStartGame);
        levelsButton.onClick.AddListener(OpenLevelsPanel);
        levelsCloseButton.onClick.AddListener(CloseLevelsPanel);
        optionsButton.onClick.AddListener(OpenOptions);
        optionsCloseButton.onClick.AddListener(CloseOptions);
        quitButton.onClick.AddListener(QuitGame);

        level1Button.onClick.AddListener(() => LoadLevel(2));
        level2Button.onClick.AddListener(() => LoadLevel(3));
        
        sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
    }

    private void OnDisable()
    {
        startButton.onClick.RemoveListener(OnStartGame);
        levelsButton.onClick.RemoveListener(OpenLevelsPanel);
        levelsCloseButton.onClick.RemoveListener(CloseLevelsPanel);
        optionsButton.onClick.RemoveListener(OpenOptions);
        optionsCloseButton.onClick.RemoveListener(CloseOptions);
        quitButton.onClick.RemoveListener(QuitGame);
        
        level1Button.onClick.RemoveAllListeners();
        level2Button.onClick.RemoveAllListeners();
        
        sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
    }

    private void OnStartGame()
    {
        // Default start goes to level 1 (scene index 2)
        LoadLevel(2);
    }

    private void LoadLevel(int sceneBuildIndex)
    {
        SceneManager.LoadScene(sceneBuildIndex);
    }

    private void OpenOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    private void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        // Persist volumes
        PlayerPrefs.SetFloat(SFX_KEY, sfxSlider.value);
        PlayerPrefs.SetFloat(MUSIC_KEY, musicSlider.value);
        PlayerPrefs.Save();
    }

    private void OpenLevelsPanel()
    {
        if (levelsPanel != null) levelsPanel.SetActive(true);
    }

    private void CloseLevelsPanel()
    {
        if (levelsPanel != null) levelsPanel.SetActive(false);
    }

    private void OnSfxChanged(float value)
    {
        ApplySfxVolume(value);
    }

    private void OnMusicChanged(float value)
    {
        ApplyMusicVolume(value);
    }

    private void ApplySfxVolume(float value)
    {
        if (sfxSource != null) sfxSource.volume = value;
    }

    private void ApplyMusicVolume(float value)
    {
        if (musicSource != null) musicSource.volume = value;
    }

    private void QuitGame()
    {
        // In editor this does nothing; in build it quits.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
