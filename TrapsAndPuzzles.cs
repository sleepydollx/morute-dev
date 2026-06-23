
public class BearTrap : MonoBehaviour
{
    [Header("Trap Settings")]
    public float snapRadius = 0.8f;
    public float immobilizeDuration = 4f;
    public float sanityDrain = 15f;
    public bool canCatchEnemy = true;

    [Header("Animation & Audio")]
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip snapSound;
    public AudioClip struggleSound;

    [Header("Visuals")]
    public GameObject openMesh;
    public GameObject closedMesh;

    private bool isTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true;
            StartCoroutine(TrapPlayer(other.gameObject));
        }
        else if (canCatchEnemy && other.CompareTag("Enemy"))
        {
            isTriggered = true;
            TrapEnemy(other.gameObject);
        }
    }

    IEnumerator TrapPlayer(GameObject player)
    {
        PlaySnap();

        PlayerController pc = player.GetComponent<PlayerController>();
        SanitySystem sanity = player.GetComponent<SanitySystem>();

        // Disable movement (set walk speed to 0 temporarily)
        float originalSpeed = pc != null ? pc.walkSpeed : 0f;
        if (pc != null) { pc.walkSpeed = 0f; pc.runSpeed = 0f; }

        sanity?.DrainSanity(sanityDrain);
        CameraShake.Instance?.Shake(0.5f, 0.08f);

        if (audioSource != null && struggleSound != null)
            audioSource.PlayOneShot(struggleSound);

        yield return new WaitForSeconds(immobilizeDuration);

        // Restore movement
        if (pc != null) { pc.walkSpeed = originalSpeed; pc.runSpeed = originalSpeed * 2f; }

        // Disarm trap after use
        gameObject.SetActive(false);
    }

    void TrapEnemy(GameObject enemy)
    {
        PlaySnap();
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            StartCoroutine(ReleaseEnemy(agent, 6f));
        }
    }

    IEnumerator ReleaseEnemy(UnityEngine.AI.NavMeshAgent agent, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (agent != null) agent.enabled = true;
        gameObject.SetActive(false);
    }

    void PlaySnap()
    {
        if (openMesh != null) openMesh.SetActive(false);
        if (closedMesh != null) closedMesh.SetActive(true);
        if (audioSource != null && snapSound != null) audioSource.PlayOneShot(snapSound);
        if (animator != null) animator.SetTrigger("Snap");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}


public class PressurePlate : MonoBehaviour
{
    [Header("Settings")]
    public bool stayActivated = false;
    public float reactivateDelay = 2f;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onActivate;
    public UnityEngine.Events.UnityEvent onDeactivate;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    [Header("Visual")]
    public MeshRenderer plateRenderer;
    public Color activeColor   = Color.red;
    public Color inactiveColor = Color.grey;

    private bool isActive = false;
    private int objectsOnPlate = 0;

    void Start()
    {
        if (plateRenderer != null)
            plateRenderer.material.color = inactiveColor;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy")) return;
        objectsOnPlate++;
        if (!isActive) Activate();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") && !other.CompareTag("Enemy")) return;
        objectsOnPlate = Mathf.Max(objectsOnPlate - 1, 0);
        if (objectsOnPlate == 0 && !stayActivated)
            StartCoroutine(DelayedDeactivate());
    }

    void Activate()
    {
        isActive = true;
        onActivate?.Invoke();
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);
        if (plateRenderer != null) plateRenderer.material.color = activeColor;
    }

    IEnumerator DelayedDeactivate()
    {
        yield return new WaitForSeconds(reactivateDelay);
        isActive = false;
        onDeactivate?.Invoke();
        if (plateRenderer != null) plateRenderer.material.color = inactiveColor;
    }
}


public class CombinationLock : MonoBehaviour
{
    [Header("Puzzle")]
    public int[] correctCode = { 4, 8, 1, 5 };  // ← CHANGE THIS to your code
    public string unlocksObjectiveId = "unlock_safe";

    [Header("UI")]
    public UnityEngine.UI.Text displayText;
    public UnityEngine.UI.Text feedbackText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip digitSound;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    [Header("Reward")]
    public GameObject rewardObject; 
    public float sanityRestore = 20f;

    private int[] enteredCode = new int[4];
    private int currentDigit = 0;
    private bool solved = false;
    private bool playerNear = false;

    void Update()
    {
        if (!playerNear || solved) return;

        // Number keys 0-9
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) && currentDigit < 4)
            {
                enteredCode[currentDigit] = i;
                currentDigit++;
                UpdateDisplay();
                audioSource?.PlayOneShot(digitSound);
            }
        }

        // Confirm with Enter
        if (Input.GetKeyDown(KeyCode.Return) && currentDigit == 4)
            CheckCode();

        // Clear with Backspace
        if (Input.GetKeyDown(KeyCode.Backspace) && currentDigit > 0)
        {
            currentDigit--;
            UpdateDisplay();
        }
    }

    void UpdateDisplay()
    {
        if (displayText == null) return;
        string display = "";
        for (int i = 0; i < 4; i++)
            display += i < currentDigit ? enteredCode[i].ToString() : "_";
        displayText.text = display;
    }

    void CheckCode()
    {
        bool correct = true;
        for (int i = 0; i < 4; i++)
            if (enteredCode[i] != correctCode[i]) { correct = false; break; }

        if (correct)
        {
            solved = true;
            audioSource?.PlayOneShot(correctSound);
            if (feedbackText != null) feedbackText.text = "UNLOCKED";

            if (rewardObject != null) rewardObject.SetActive(true);

            SanitySystem sanity = FindObjectOfType<SanitySystem>();
            sanity?.RestoreSanity(sanityRestore);

            ObjectiveTracker.Instance?.CompleteObjective(unlocksObjectiveId);
            StoryManager.Instance?.TriggerEvent(unlocksObjectiveId);
        }
        else
        {
            audioSource?.PlayOneShot(wrongSound);
            if (feedbackText != null) feedbackText.text = "WRONG";
            SanitySystem sanity = FindObjectOfType<SanitySystem>();
            sanity?.DrainSanity(5f);
            currentDigit = 0;
            UpdateDisplay();
        }
    }

    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) playerNear = true; }
    void OnTriggerExit(Collider other)  { if (other.CompareTag("Player")) playerNear = false; }
}
