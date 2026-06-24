using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

// ============================================================
//  MAIN MENU MANAGER
//  Attach this to a MainMenuManager GameObject in your scene.
//
//  SETUP:
//  1. Create a Canvas with these panels:
//     - MainPanel     (Play, Continue, Settings, Quit buttons)
//     - SettingsPanel (volume, sensitivity sliders)
//     - CreditsPanel  (your studio name, game title)
//  2. Drag each panel + button into the Inspector fields below
//  3. Set your game scene name in "gameSceneName"
// ============================================================

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "Chapter1";   // ← CHANGE to your first game scene

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject confirmQuitPanel;

    [Header("Main Panel Buttons")]
    public Button playButton;
    public Button continueButton;     // Only shows if a save exists
    public Button settingsButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("Settings Panel")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider mouseSensSlider;
    public Text   masterVolumeLabel;
    public Text   musicVolumeLabel;
    public Text   sfxVolumeLabel;
    public Text   mouseSensLabel;
    public Button settingsBackButton;

    [Header("Credits Panel")]
    public Button creditsBackButton;

    [Header("Confirm Quit Panel")]
    public Button confirmQuitYesButton;
    public Button confirmQuitNoButton;

    [Header("Title UI")]
    public Text gameTitleLabel;       // e.g. "FAWN'S VEIL"
    public Text studioLabel;          // e.g. "A Fawn's Veil Studio Game"
    public Text versionLabel;         // e.g. "v0.1.0"
    public Text pressAnyKeyLabel;     // Optional "Press any key" prompt

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioClip   menuMusic;
    public AudioClip   hoverSound;
    public AudioClip   clickSound;
    public AudioClip   backSound;

    [Header("Intro")]
    public bool  showIntroOnStart = true;
    public float introFadeDuration = 2f;
    public CanvasGroup fadeCanvasGroup;   // Full-screen black fade overlay

    private bool introComplete = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        SetupLabels();
        SetupButtons();
        SetupSliders();
        ShowPanel(mainPanel);

        // Show Continue only if a save exists
        if (continueButton != null)
            continueButton.gameObject.SetActive(SaveSystem.Instance != null && SaveSystem.Instance.SaveExists());

        // Start music
        if (musicSource != null && menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        if (showIntroOnStart)
            StartCoroutine(PlayIntro());
        else
            introComplete = true;
    }

    void Update()
    {
        // "Press any key" to skip intro
        if (!introComplete && Input.anyKeyDown)
        {
            StopAllCoroutines();
            SkipIntro();
        }
    }

    // ── Labels ────────────────────────────────────────────────────────────────

    void SetupLabels()
    {
        if (gameTitleLabel != null) gameTitleLabel.text  = "FAWN'S VEIL";     // ← CHANGE
        if (studioLabel    != null) studioLabel.text     = "A Fawn's Veil Studio Game"; // ← CHANGE
        if (versionLabel   != null) versionLabel.text    = "v0.1.0";          // ← CHANGE
        if (pressAnyKeyLabel != null) pressAnyKeyLabel.text = "PRESS ANY KEY";
    }

    // ── Button wiring ─────────────────────────────────────────────────────────

    void SetupButtons()
    {
        playButton?.onClick.AddListener(OnPlayClicked);
        continueButton?.onClick.AddListener(OnContinueClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
        creditsButton?.onClick.AddListener(OnCreditsClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);

        settingsBackButton?.onClick.AddListener(OnSettingsBack);
        creditsBackButton?.onClick.AddListener(OnCreditsBack);

        confirmQuitYesButton?.onClick.AddListener(OnConfirmQuit);
        confirmQuitNoButton?.onClick.AddListener(OnCancelQuit);

        // Add hover sounds to all buttons
        AddHoverSounds(playButton, continueButton, settingsButton,
                       creditsButton, quitButton, settingsBackButton,
                       creditsBackButton, confirmQuitYesButton, confirmQuitNoButton);
    }

    // ── Slider setup ──────────────────────────────────────────────────────────

    void SetupSliders()
    {
        float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVol  = PlayerPrefs.GetFloat("MusicVolume",  0.7f);
        float sfxVol    = PlayerPrefs.GetFloat("SFXVolume",    1f);
        float mouseSens = PlayerPrefs.GetFloat("MouseSens",    2f);

        SetSlider(masterVolumeSlider, masterVol, 0f, 1f, OnMasterVolumeChanged);
        SetSlider(musicVolumeSlider,  musicVol,  0f, 1f, OnMusicVolumeChanged);
        SetSlider(sfxVolumeSlider,    sfxVol,    0f, 1f, OnSFXVolumeChanged);
        SetSlider(mouseSensSlider,    mouseSens, 0.5f, 5f, OnMouseSensChanged);

        UpdateSliderLabels(masterVol, musicVol, sfxVol, mouseSens);
    }

    void SetSlider(Slider s, float value, float min, float max, UnityEngine.Events.UnityAction<float> callback)
    {
        if (s == null) return;
        s.minValue = min;
        s.maxValue = max;
        s.value    = value;
        s.onValueChanged.AddListener(callback);
    }

    void OnPlayClicked()
    {
        PlayClick();
        StartCoroutine(LoadScene(gameSceneName));
    }

    void OnContinueClicked()
    {
        PlayClick();
        StartCoroutine(LoadScene(gameSceneName, loadSave: true));
    }

    void OnSettingsClicked()
    {
        PlayClick();
        ShowPanel(settingsPanel);
    }

    void OnCreditsClicked()
    {
        PlayClick();
        ShowPanel(creditsPanel);
    }

    void OnQuitClicked()
    {
        PlayClick();
        if (confirmQuitPanel != null)
            ShowPanel(confirmQuitPanel);
        else
            QuitGame();
    }

    void OnSettingsBack()
    {
        PlayBack();
        PlayerPrefs.Save();
        ShowPanel(mainPanel);
    }

    void OnCreditsBack()
    {
        PlayBack();
        ShowPanel(mainPanel);
    }

    void OnConfirmQuit()  { PlayClick(); QuitGame(); }
    void OnCancelQuit()   { PlayBack();  ShowPanel(mainPanel); }


    void OnMasterVolumeChanged(float val)
    {
        AudioListener.volume = val;
        PlayerPrefs.SetFloat("MasterVolume", val);
        if (masterVolumeLabel != null) masterVolumeLabel.text = $"Master  {Mathf.RoundToInt(val * 100)}%";
    }

    void OnMusicVolumeChanged(float val)
    {
        if (musicSource != null) musicSource.volume = val;
        PlayerPrefs.SetFloat("MusicVolume", val);
        if (musicVolumeLabel != null) musicVolumeLabel.text = $"Music  {Mathf.RoundToInt(val * 100)}%";
    }

    void OnSFXVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat("SFXVolume", val);
        if (sfxVolumeLabel != null) sfxVolumeLabel.text = $"SFX  {Mathf.RoundToInt(val * 100)}%";
    }

    void OnMouseSensChanged(float val)
    {
        PlayerPrefs.SetFloat("MouseSens", val);
        if (mouseSensLabel != null) mouseSensLabel.text = $"Sensitivity  {val:F1}";
    }

    void UpdateSliderLabels(float master, float music, float sfx, float sens)
    {
        if (masterVolumeLabel != null) masterVolumeLabel.text = $"Master  {Mathf.RoundToInt(master * 100)}%";
        if (musicVolumeLabel  != null) musicVolumeLabel.text  = $"Music  {Mathf.RoundToInt(music * 100)}%";
        if (sfxVolumeLabel    != null) sfxVolumeLabel.text    = $"SFX  {Mathf.RoundToInt(sfx * 100)}%";
        if (mouseSensLabel    != null) mouseSensLabel.text    = $"Sensitivity  {sens:F1}";
    }

    // ── Panel switching ───────────────────────────────────────────────────────

    void ShowPanel(GameObject target)
    {
        if (mainPanel       != null) mainPanel.SetActive(false);
        if (settingsPanel   != null) settingsPanel.SetActive(false);
        if (creditsPanel    != null) creditsPanel.SetActive(false);
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
        if (target          != null) target.SetActive(true);
    }

    // ── Scene loading ─────────────────────────────────────────────────────────

    IEnumerator LoadScene(string sceneName, bool loadSave = false)
    {
        // Fade out
        if (fadeCanvasGroup != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 1.5f;
                fadeCanvasGroup.alpha = t;
                yield return null;
            }
        }

        // Fade music
        if (musicSource != null)
            StartCoroutine(FadeOutMusic(1f));

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (!load.isDone) yield return null;

        if (loadSave) SaveSystem.Instance?.Load();
    }

    IEnumerator FadeOutMusic(float duration)
    {
        if (musicSource == null) yield break;
        float start = musicSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        musicSource.Stop();
    }
    IEnumerator PlayIntro()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            yield return new WaitForSeconds(0.5f);

            float t = 0f;
            while (t < introFadeDuration)
            {
                t += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / introFadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
        }

        if (pressAnyKeyLabel != null)
            StartCoroutine(BlinkText(pressAnyKeyLabel));

        introComplete = true;
    }

    void SkipIntro()
    {
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        introComplete = true;
        if (pressAnyKeyLabel != null)
            StartCoroutine(BlinkText(pressAnyKeyLabel));
    }

    IEnumerator BlinkText(Text label)
    {
        while (true)
        {
            label.enabled = !label.enabled;
            yield return new WaitForSeconds(0.7f);
        }
    }

    void PlayClick() => sfxSource?.PlayOneShot(clickSound);
    void PlayBack()  => sfxSource?.PlayOneShot(backSound);

    void AddHoverSounds(params Button[] buttons)
    {
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            UnityEngine.EventSystems.EventTrigger trigger =
                btn.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>()
                ?? btn.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var entry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            entry.callback.AddListener(_ => sfxSource?.PlayOneShot(hoverSound, 0.4f));
            trigger.triggers.Add(entry);
        }
    }

    // ── Quit ─────────────────────────────────────────────────────────────────

    void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting game.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}