using UnityEngine;
using System.Collections;

public class AtmosphereSystem : MonoBehaviour
{ 
    public static AtmosphereSystem Instance { get; private set; }

    public enum WeatherState { Clear, Overcast, Rainy, Stormy }

    [Header("Time of Day")]
    public Light sunLight;
    public Gradient skyColorOverDay;       
    public float dayDurationSeconds = 300f; 
    [Range(0f, 1f)] public float timeOfDay = 0.5f; 
    public bool pauseTime = false;

    [Header("Weather")]
    public WeatherState currentWeather = WeatherState.Clear;
    public ParticleSystem rainParticles;
    public AudioSource rainAudioSource;
    public AudioClip[] thunderClips;
    public float thunderInterval = 15f;

    [Header("Fog")]
    public bool dynamicFog = true;
    public float clearFogDensity   = 0.01f;
    public float overcastFogDensity = 0.03f;
    public float rainyFogDensity    = 0.06f;
    public float stormyFogDensity   = 0.1f;

    [Header("Wind")]
    public AudioSource windAudioSource;
    public float windVolumeClear = 0f;
    public float windVolumeStormy = 0.8f;

    [Header("Sanity")]
    public float stormySanityDrain = 2f;

    private SanitySystem playerSanity;
    private float thunderTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        playerSanity = FindObjectOfType<SanitySystem>();
        thunderTimer = thunderInterval;
        ApplyWeather(currentWeather, immediate: true);
    }

    void Update()
    {
        if (!pauseTime)
        {
            timeOfDay += Time.deltaTime / dayDurationSeconds;
            if (timeOfDay >= 1f) timeOfDay = 0f;
        }

        UpdateSunlight();
        UpdateThunder();

        if (currentWeather == WeatherState.Stormy && playerSanity != null)
            playerSanity.DrainSanity(stormySanityDrain * Time.deltaTime);
    }

    void UpdateSunlight()
    {
        if (sunLight == null) return;

        sunLight.transform.rotation = Quaternion.Euler(timeOfDay * 360f - 90f, 170f, 0f);

        float brightness = Mathf.Clamp01(Mathf.Sin(timeOfDay * Mathf.PI));
        sunLight.intensity = Mathf.Lerp(0.05f, 1.2f, brightness);
        if (RenderSettings.skybox != null && skyColorOverDay != null)
            RenderSettings.skybox.SetColor("_Tint", skyColorOverDay.Evaluate(timeOfDay));
    }

    void UpdateThunder()
    {
        if (currentWeather != WeatherState.Stormy || thunderClips.Length == 0) return;

        thunderTimer -= Time.deltaTime;
        if (thunderTimer <= 0f)
        {
            AudioClip clip = thunderClips[Random.Range(0, thunderClips.Length)];
            if (rainAudioSource != null) rainAudioSource.PlayOneShot(clip, 0.8f);

            StartCoroutine(LightningFlash());
            thunderTimer = thunderInterval + Random.Range(-5f, 10f);
        }
    }

    IEnumerator LightningFlash()
    {
        if (sunLight == null) yield break;
        float original = sunLight.intensity;

        sunLight.intensity = 5f;
        yield return new WaitForSeconds(0.08f);
        sunLight.intensity = original;
        yield return new WaitForSeconds(0.12f);
        sunLight.intensity = 5f;
        yield return new WaitForSeconds(0.05f);
        sunLight.intensity = original;
    }

    public void SetWeather(WeatherState newWeather)
    {
        currentWeather = newWeather;
        ApplyWeather(newWeather, immediate: false);
    }

    void ApplyWeather(WeatherState weather, bool immediate)
    {
        float targetFog = clearFogDensity;
        float targetWind = windVolumeClear;
        bool rainActive = false;

        switch (weather)
        {
            case WeatherState.Clear:
                targetFog = clearFogDensity; targetWind = windVolumeClear; break;
            case WeatherState.Overcast:
                targetFog = overcastFogDensity; targetWind = 0.2f; break;
            case WeatherState.Rainy:
                targetFog = rainyFogDensity; targetWind = 0.5f; rainActive = true; break;
            case WeatherState.Stormy:
                targetFog = stormyFogDensity; targetWind = windVolumeStormy; rainActive = true; break;
        }

        if (immediate)
        {
            RenderSettings.fogDensity = targetFog;
            if (windAudioSource != null) windAudioSource.volume = targetWind;
        }
        else
        {
            StartCoroutine(TransitionFog(targetFog, 5f));
            StartCoroutine(TransitionWind(targetWind, 5f));
        }

        if (rainParticles != null)
        {
            if (rainActive && !rainParticles.isPlaying) rainParticles.Play();
            else if (!rainActive && rainParticles.isPlaying) rainParticles.Stop();
        }

        if (rainAudioSource != null)
        {
            float rainVol = (weather == WeatherState.Rainy) ? 0.4f : (weather == WeatherState.Stormy) ? 0.7f : 0f;
            if (!immediate) StartCoroutine(TransitionAudioVolume(rainAudioSource, rainVol, 3f));
            else rainAudioSource.volume = rainVol;
        }

        RenderSettings.fog = true;
    }

    IEnumerator TransitionFog(float target, float duration)
    {
        float start = RenderSettings.fogDensity;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            RenderSettings.fogDensity = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        RenderSettings.fogDensity = target;
    }

    IEnumerator TransitionWind(float target, float duration)
    {
        if (windAudioSource == null) yield break;
        float start = windAudioSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            windAudioSource.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
    }

    IEnumerator TransitionAudioVolume(AudioSource src, float target, float duration)
    {
        float start = src.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
    }

    public bool IsNight() => timeOfDay < 0.25f || timeOfDay > 0.75f;
    public float GetTimeOfDay() => timeOfDay;
}
