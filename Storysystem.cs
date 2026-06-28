using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    public enum Chapter { Prologue, One, Two, Three, Finale }
    public Chapter currentChapter = Chapter.Prologue;

    [System.Serializable]
    public class StoryEvent
    {
        public string eventId;
        public Chapter requiredChapter;
        public bool hasTriggered;
        public UnityEngine.Events.UnityEvent onTrigger;
    }

    [Header("Events")]
    public List<StoryEvent> storyEvents = new List<StoryEvent>();

    [Header("Chapter Scenes")]
    public string[] chapterSceneNames = { "Before I Close My Eyes", "ChapterOne", "ChapterTwo", "ChapterThree", "Finale" };

    [Header("Endings")]
    public string endingGoodScene   = "EndingGood";
    public string endingBadScene    = "EndingBad";
    public string endingSecretScene = "EndingSecret";

    // Secret ending condition — collect all lore items
    private int totalLoreItems = 5;
    private int collectedLoreItems = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartChapter(currentChapter);
    }

    // ── Chapter flow ──────────────────────────────────────────────────────────

    public void StartChapter(Chapter chapter)
    {
        currentChapter = chapter;
        Debug.Log($"[Story] Starting chapter: {chapter}");

        switch (chapter)
        {
            case Chapter.Prologue:
                SetupPrologue(); break;
            case Chapter.One:
                SetupChapterOne(); break;
            case Chapter.Two:
                SetupChapterTwo(); break;
            case Chapter.Three:
                SetupChapterThree(); break;
            case Chapter.Finale:
                StartCoroutine(PlayFinale()); break;
        }
    }

    public void AdvanceChapter()
    {
        int next = (int)currentChapter + 1;
        if (next > (int)Chapter.Finale) return;

        Chapter nextChapter = (Chapter)next;
        string sceneName = chapterSceneNames != null && next < chapterSceneNames.Length
            ? chapterSceneNames[next] : "";

        if (!string.IsNullOrEmpty(sceneName))
            StartCoroutine(LoadChapterScene(sceneName, nextChapter));
        else
            StartChapter(nextChapter);
    }

    IEnumerator LoadChapterScene(string sceneName, Chapter chapter)
    {
        FadeSystem.Instance?.FadeOut(1.5f);
        yield return new WaitForSeconds(1.5f);
        SceneManager.LoadScene(sceneName);
        yield return new WaitForSeconds(0.5f);
        FadeSystem.Instance?.FadeIn(1.5f);
        StartChapter(chapter);
    }


    void SetupPrologue()
    {
        ObjectiveTracker.Instance?.AddObjective("prologue_exit", "Find a way out of the room");
        HUDController hud = FindObjectOfType<HUDController>();
        hud?.ShowObjective("Find a way out of the room");
    }

    void SetupChapterOne()
    {
        ObjectiveTracker.Instance?.AddObjective("ch1_key", "Find the basement key");
        ObjectiveTracker.Instance?.AddObjective("ch1_basement", "Enter the basement");
        AtmosphereSystem.Instance?.SetWeather(AtmosphereSystem.WeatherState.Overcast);
    }

    void SetupChapterTwo()
    {
        ObjectiveTracker.Instance?.AddObjective("ch2_clues", "Find 3 clues about the disappearance");
        AtmosphereSystem.Instance?.SetWeather(AtmosphereSystem.WeatherState.Rainy);
    }

    void SetupChapterThree()
    {
        ObjectiveTracker.Instance?.AddObjective("ch3_ritual", "Stop the ritual");
        AtmosphereSystem.Instance?.SetWeather(AtmosphereSystem.WeatherState.Stormy);
    }

    IEnumerator PlayFinale()
    {
        FadeSystem.Instance?.FadeOut(2f);
        yield return new WaitForSeconds(2f);

        SanitySystem sanity = FindObjectOfType<SanitySystem>();
        float finalSanity = sanity != null ? sanity.GetCurrentSanity() : 50f;

        if (collectedLoreItems >= totalLoreItems)
            SceneManager.LoadScene(endingSecretScene);
        else if (finalSanity >= 50f)
            SceneManager.LoadScene(endingSorrowScene);
        else
            SceneManager.LoadScene(endingBadScene);
    }


    public void TriggerEvent(string eventId)
    {
        StoryEvent ev = storyEvents.Find(e => e.eventId == eventId);
        if (ev == null || ev.hasTriggered) return;
        if (ev.requiredChapter != currentChapter) return;

        ev.hasTriggered = true;
        ev.onTrigger?.Invoke();
        Debug.Log($"[Story] Event fired: {eventId}");
    }

    public void RegisterLoreItem()
    {
        collectedLoreItems++;
        Debug.Log($"[Story] Lore items: {collectedLoreItems}/{totalLoreItems}");
    }

    public bool IsChapterAtLeast(Chapter chapter) => currentChapter >= chapter;
}


public class FadeSystem : MonoBehaviour
{
    public static FadeSystem Instance { get; private set; }
    public UnityEngine.UI.Image fadeImage;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (fadeImage != null) SetAlpha(0f);
    }

    public void FadeOut(float duration) => StartCoroutine(Fade(0f, 1f, duration));
    public void FadeIn(float duration)  => StartCoroutine(Fade(1f, 0f, duration));

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        SetAlpha(from);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}


public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalLocalPos;
    private bool shaking = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => originalLocalPos = transform.localPosition;

    public void Shake(float duration, float magnitude)
    {
        if (!shaking) StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        shaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = originalLocalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        shaking = false;
    }
}