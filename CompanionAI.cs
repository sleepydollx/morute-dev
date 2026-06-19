using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CompanionAI : MonoBehaviour
{
    public enum CompanionState { Following, Waiting, Hiding, Scared, Separated, Lost }

    [Header("State")]
    public CompanionState currentState = CompanionState.Following;

    [Header("Follow Settings")]
    public Transform player;
    public float followDistance   = 3f;
    public float stopDistance     = 2f;  
    public float followSpeed      = 3.5f;
    public float separationDistance = 20f; 

    [Header("Reaction")]
    public float dangerAwarenessRadius = 10f;  // sees enemies within this range
    public float scaredDuration = 5f;

    [Header("Dialogue")]
    public string companionName = "Alex";
    public AudioSource voiceSource;

    [Header("Hint Lines")]
    public string[] idleHints = {
        "I wonder how much this all cost...",
        "I don't like this place.",
        "Did you hear that?",
        "Something doesn't feel right."
    };
    public string[] scaredLines = {
        "Oh no, oh no, oh no—",
        "We need to hide. NOW.",
        "I saw something move over there!"
    };
    public string[] reuniteLines = {
        "Thank god, I thought I lost you!",
        "Don't leave me behind like that!",
        "Stay close, please."
    };
    public string[] hintLines = {
        "Maybe we need a key for that door.",
        "I think I saw something shiny back there.",
        "The note said the basement — I really don't want to go down there."
    };

    [Header("UI")]
    public UnityEngine.UI.Text companionSpeechBubble;

    private NavMeshAgent agent;
    private float stateTimer = 0f;
    private float hintTimer  = 0f;
    private float hintInterval = 25f;
    private bool wasSeparated = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent.speed = followSpeed;
        HideSpeechBubble();
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        CheckForDanger();
        CheckSeparation(distToPlayer);
        HandleHintTimer();

        switch (currentState)
        {
            case CompanionState.Following:  HandleFollow(distToPlayer);  break;
            case CompanionState.Waiting:    HandleWait();                break;
            case CompanionState.Hiding:     HandleHide();                break;
            case CompanionState.Scared:     HandleScared(distToPlayer);  break;
            case CompanionState.Separated:  HandleSeparated(distToPlayer); break;
        }
    }

    // ── State Handlers ────────────────────────────────────────────────────────

    void HandleFollow(float dist)
    {
        if (dist > stopDistance)
            agent.SetDestination(player.position);
        else
            agent.ResetPath();

        FacePlayer();
    }

    void HandleWait()
    {
        agent.ResetPath();
        FacePlayer();
    }

    void HandleHide()
    {
        // Find a nearby hiding spot (any object tagged "HidingSpot")
        GameObject[] spots = GameObject.FindGameObjectsWithTag("HidingSpot");
        if (spots.Length > 0)
        {
            GameObject closest = null;
            float minDist = Mathf.Infinity;
            foreach (var s in spots)
            {
                float d = Vector3.Distance(transform.position, s.transform.position);
                if (d < minDist) { minDist = d; closest = s; }
            }
            if (closest != null) agent.SetDestination(closest.transform.position);
        }

        stateTimer += Time.deltaTime;
        if (stateTimer > 8f)
            ChangeState(CompanionState.Following);
    }

    void HandleScared(float dist)
    {
        agent.speed = followSpeed * 1.8f;
        if (dist > stopDistance)
            agent.SetDestination(player.position);

        stateTimer += Time.deltaTime;
        if (stateTimer > scaredDuration)
        {
            agent.speed = followSpeed;
            ChangeState(CompanionState.Following);
        }
    }

    void HandleSeparated(float dist)
    {
        agent.SetDestination(player.position);

        if (dist <= separationDistance * 0.6f)
        {
            wasSeparated = true;
            ChangeState(CompanionState.Following);
        }
    }

    // ── Checks ────────────────────────────────────────────────────────────────

    void CheckForDanger()
    {
        if (currentState == CompanionState.Scared || currentState == CompanionState.Hiding) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, dangerAwarenessRadius);
        foreach (var h in hits)
        {
            if (h.CompareTag("Enemy"))
            {
                Speak(scaredLines[Random.Range(0, scaredLines.Length)]);
                ChangeState(Random.value > 0.5f ? CompanionState.Scared : CompanionState.Hiding);
                return;
            }
        }
    }

    void CheckSeparation(float dist)
    {
        if (dist > separationDistance && currentState != CompanionState.Separated)
            ChangeState(CompanionState.Separated);

        if (wasSeparated && dist < stopDistance * 2f)
        {
            Speak(reuniteLines[Random.Range(0, reuniteLines.Length)]);
            wasSeparated = false;
        }
    }

    void HandleHintTimer()
    {
        if (currentState != CompanionState.Following) return;
        hintTimer += Time.deltaTime;
        if (hintTimer >= hintInterval)
        {
            hintTimer = 0f;
            hintInterval = Random.Range(20f, 40f);
            string[] pool = Random.value > 0.5f ? hintLines : idleHints;
            Speak(pool[Random.Range(0, pool.Length)]);
        }
    }

    // ── Speech ────────────────────────────────────────────────────────────────

    public void Speak(string line, float duration = 4f)
    {
        StopAllCoroutines();
        StartCoroutine(ShowSpeech(line, duration));
    }

    IEnumerator ShowSpeech(string line, float duration)
    {
        if (companionSpeechBubble != null)
        {
            companionSpeechBubble.text = $"{companionName}: \"{line}\"";
            companionSpeechBubble.enabled = true;
        }
        yield return new WaitForSeconds(duration);
        HideSpeechBubble();
    }

    void HideSpeechBubble()
    {
        if (companionSpeechBubble != null) companionSpeechBubble.enabled = false;
    }

    void FacePlayer()
    {
        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 4f);
    }


    public void WaitHere() => ChangeState(CompanionState.Waiting);
    public void Follow()   => ChangeState(CompanionState.Following);

    void ChangeState(CompanionState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dangerAwarenessRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, separationDistance);
    }
}
