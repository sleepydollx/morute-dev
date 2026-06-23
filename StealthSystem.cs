using UnityEngine;
using System.Collections;
public class StealthSystem : MonoBehaviour
{
    [Header("Visibility")]
    [Range(0f, 1f)] public float visibilityLevel = 0f; 
    public float visibilityRiseSpeed  = 2f;
    public float visibilityFallSpeed  = 1.5f;

    [Header("Modifiers")]
    public float runningVisibilityBonus  = 0.5f;  
    public float crouchVisibilityReduction = 0.4f;
    public float lightVisibilityBonus    = 0.3f;

    [Header("Detection")]
    public float detectionThreshold = 0.85f;
    public LayerMask lightLayer;

    [Header("UI")]
    public UnityEngine.UI.Image visibilityBar;
    public UnityEngine.UI.Text  visibilityLabel;

    private PlayerController playerController;
    private bool isInLight = false;
    private float targetVisibility = 0f;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        CalculateVisibility();
        visibilityLevel = Mathf.MoveTowards(visibilityLevel, targetVisibility,
            (targetVisibility > visibilityLevel ? visibilityRiseSpeed : visibilityFallSpeed) * Time.deltaTime);
        UpdateUI();
    }

    void CalculateVisibility()
    {
        targetVisibility = 0
        float h = Mathf.Abs(Input.GetAxis("Horizontal"));
        float v = Mathf.Abs(Input.GetAxis("Vertical"));
        bool moving = h > 0.1f || v > 0.1f;

        if (moving) targetVisibility += 0.3f;
        if (playerController != null && playerController.IsRunning())  targetVisibility += runningVisibilityBonus;
        if (playerController != null && playerController.IsCrouching()) targetVisibility -= crouchVisibilityReduction;

        isInLight = IsInLight();
        if (isInLight) targetVisibility += lightVisibilityBonus;

        // Flashlight — if on, player is slightly more visible to enemies
        FlashlightSystem fl = GetComponent<FlashlightSystem>();
        if (fl != null && fl.IsOn()) targetVisibility += 0.15f;

        targetVisibility = Mathf.Clamp01(targetVisibility);
    }

    bool IsInLight()
    {
        // Sample ambient light at player position
        // Simple approach: check RenderSettings ambient + nearby lights
        float ambientBrightness = RenderSettings.ambientLight.grayscale;
        if (ambientBrightness > 0.3f) return true;

        Collider[] hits = Physics.OverlapSphere(transform.position, 5f, lightLayer);
        return hits.Length > 0;
    }

    void UpdateUI()
    {
        if (visibilityBar != null)
        {
            visibilityBar.fillAmount = visibilityLevel;
            visibilityBar.color = Color.Lerp(Color.green, Color.red, visibilityLevel);
        }
        if (visibilityLabel != null)
        {
            string status = visibilityLevel < 0.3f ? "Hidden" :
                            visibilityLevel < 0.6f ? "Caution" :
                            visibilityLevel < detectionThreshold ? "Exposed" : "DETECTED";
            visibilityLabel.text = status;
        }
    }

    public float GetVisibility() => visibilityLevel;
    public bool IsDetectable() => visibilityLevel >= detectionThreshold;
}


// ──────────────────────────────────────────────────────────────────────────────


/// <summary>
/// HIDING SPOT — Player can hide inside (closet, under bed, locker, bush).
/// Press E to enter/exit. Hides the player from enemies completely.
/// </summary>
public class HidingSpot : Interactable
{
    [Header("Hiding")]
    public Transform hidePosition;        // Where the player snaps to inside
    public bool blockPlayerView = true;   // Fade to black while hiding?
    public float peekVisibility = 0.05f;  // Low visibility while hidden (not zero — can still be heard)

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip enterSound;
    public AudioClip exitSound;

    private bool playerHiding = false;
    private GameObject playerObj;
    private PlayerController playerController;
    private CharacterController characterController;
    private Vector3 entryPosition;
    private Quaternion entryRotation;

    void Start()
    {
        promptText = "Press E to hide";
    }

    protected override void Interact()
    {
        if (!playerHiding) EnterHide();
        else ExitHide();
    }

    void EnterHide()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        playerController    = playerObj.GetComponent<PlayerController>();
        characterController = playerObj.GetComponent<CharacterController>();

        entryPosition = playerObj.transform.position;
        entryRotation = playerObj.transform.rotation;

        // Move player into hiding spot
        characterController.enabled = false;
        if (hidePosition != null)
        {
            playerObj.transform.position = hidePosition.position;
            playerObj.transform.rotation = hidePosition.rotation;
        }
        characterController.enabled = true;

        // Disable movement
        if (playerController != null) playerController.enabled = false;

        // Override visibility
        StealthSystem stealth = playerObj.GetComponent<StealthSystem>();
        if (stealth != null) stealth.visibilityLevel = peekVisibility;

        if (blockPlayerView) FadeSystem.Instance?.FadeOut(0.3f);
        audioSource?.PlayOneShot(enterSound);

        playerHiding = true;
        promptText = "Press E to exit hiding spot";
        InteractionPromptUI.Instance?.Show(promptText);
    }

    void ExitHide()
    {
        if (playerObj == null) return;

        characterController.enabled = false;
        playerObj.transform.position = entryPosition;
        playerObj.transform.rotation = entryRotation;
        characterController.enabled = true;

        if (playerController != null) playerController.enabled = true;

        if (blockPlayerView) FadeSystem.Instance?.FadeIn(0.3f);
        audioSource?.PlayOneShot(exitSound);

        playerHiding = false;
        promptText = "Press E to hide";
    }

    public bool IsOccupied() => playerHiding;
}

public class DistractionThrow : MonoBehaviour
{
    [Header("Throw")]
    public GameObject throwablePrefab;
    public float throwForce = 12f;
    public float throwCooldown = 3f;
    public int maxThrowables = 3;
    public KeyCode throwKey = KeyCode.G;

    [Header("Noise")]
    public float noiseLoudness = 1.5f;
    public float noiseRadius   = 12f;

    [Header("UI")]
    public UnityEngine.UI.Text ammoLabel;

    private int currentThrowables;
    private float cooldownTimer = 0f;
    private Camera playerCamera;

    void Start()
    {
        currentThrowables = maxThrowables;
        playerCamera = Camera.main;
        UpdateUI();
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(throwKey) && currentThrowables > 0 && cooldownTimer <= 0f)
            Throw();
    }

    void Throw()
    {
        if (throwablePrefab == null || playerCamera == null) return;

        Vector3 spawnPos = playerCamera.transform.position + playerCamera.transform.forward * 0.8f;
        GameObject obj = Instantiate(throwablePrefab, spawnPos, Random.rotation);

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);

        // Attach noise alert to the thrown object
        obj.AddComponent<ThrownObjectNoise>().Initialize(noiseLoudness, noiseRadius);

        currentThrowables--;
        cooldownTimer = throwCooldown;
        UpdateUI();
    }

    public void AddThrowable(int amount = 1)
    {
        currentThrowables = Mathf.Min(currentThrowables + amount, maxThrowables);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (ammoLabel != null) ammoLabel.text = $"[G] Distract x{currentThrowables}";
    }
}

/// <summary>Auto-alerts enemies on impact. Added dynamically to thrown objects.</summary>
public class ThrownObjectNoise : MonoBehaviour
{
    private float loudness;
    private float radius;
    private bool hasHit = false;

    public void Initialize(float l, float r) { loudness = l; radius = r; }

    void OnCollisionEnter(Collision col)
    {
        if (hasHit) return;
        hasHit = true;

        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
        foreach (var e in enemies)
            e.HearSound(transform.position, loudness);

        Destroy(gameObject, 5f);
    }
}
