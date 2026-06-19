using UnityEngine;
using UnityEngine.AI;
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Patrolling, Investigating, Chasing, Searching }

    [Header("State")]
    public EnemyState currentState = EnemyState.Patrolling;

    [Header("Detection")]
    public float sightRange = 12f;
    public float hearingRange = 8f;
    public float fieldOfView = 110f;
    public LayerMask playerLayer;
    public LayerMask obstructionLayer;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 6f;
    public float searchSpeed = 3f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    private int patrolIndex = 0;

    [Header("References")]
    public Transform player;
    private NavMeshAgent agent;
    private SanitySystem playerSanity;

    private Vector3 lastKnownPosition;
    private float stateTimer = 0f;
    private float searchDuration = 10f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (player != null)
            playerSanity = player.GetComponent<SanitySystem>();
    }

    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling:    HandlePatrol();    break;
            case EnemyState.Investigating: HandleInvestigate(); break;
            case EnemyState.Chasing:       HandleChase();     break;
            case EnemyState.Searching:     HandleSearch();    break;
            case EnemyState.Idle:          HandleIdle();      break;
        }

        CheckForPlayer();
        DrainPlayerSanityByProximity();
    }

    void HandlePatrol()
    {
        agent.speed = patrolSpeed;

        if (patrolPoints.Length == 0) return;

        if (agent.remainingDistance < 0.5f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    void HandleInvestigate()
    {
        agent.speed = searchSpeed;
        agent.SetDestination(lastKnownPosition);

        if (agent.remainingDistance < 0.8f)
            ChangeState(EnemyState.Searching);
    }

    void HandleChase()
    {
        agent.speed = chaseSpeed;
        if (player != null)
        {
            agent.SetDestination(player.position);
            lastKnownPosition = player.position;

            if (!CanSeePlayer())
            {
                ChangeState(EnemyState.Searching);
            }
        }
    }

    void HandleSearch()
    {
        agent.speed = searchSpeed;
        stateTimer += Time.deltaTime;

        if (agent.remainingDistance < 0.5f)
        {
            Vector3 randomOffset = Random.insideUnitSphere * 5f;
            randomOffset.y = 0;
            agent.SetDestination(lastKnownPosition + randomOffset);
        }

        if (stateTimer >= searchDuration)
        {
            stateTimer = 0f;
            ChangeState(EnemyState.Patrolling);
        }
    }

    void HandleIdle()
    {
        agent.speed = 0f;
        stateTimer += Time.deltaTime;
        if (stateTimer > 3f)
        {
            stateTimer = 0f;
            ChangeState(EnemyState.Patrolling);
        }
    }


    void CheckForPlayer()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer <= sightRange && CanSeePlayer())
        {
            ChangeState(EnemyState.Chasing);
            return;
        }

        PlayerController pc = player.GetComponent<PlayerController>();
        float activeHearingRange = (pc != null && pc.IsRunning()) ? hearingRange : hearingRange * 0.4f;

        if (distToPlayer <= activeHearingRange && currentState == EnemyState.Patrolling)
        {
            lastKnownPosition = player.position;
            ChangeState(EnemyState.Investigating);
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);

        if (angle > fieldOfView / 2f) return false;

        float dist = Vector3.Distance(transform.position, player.position);
        return !Physics.Raycast(transform.position, dirToPlayer, dist, obstructionLayer);
    }
    public void HearSound(Vector3 soundOrigin, float loudness)
    {
        float dist = Vector3.Distance(transform.position, soundOrigin);
        if (dist <= hearingRange * loudness && currentState != EnemyState.Chasing)
        {
            lastKnownPosition = soundOrigin;
            ChangeState(EnemyState.Investigating);
        }
    }

    void DrainPlayerSanityByProximity()
    {
        if (player == null || playerSanity == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < 5f)
        {
            float drain = Mathf.Lerp(15f, 0f, dist / 5f);
            playerSanity.DrainSanity(drain * Time.deltaTime);
        }
    }

    // ── Utilities ──────────────────────────────────────────────────────

    void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        stateTimer = 0f;
        Debug.Log($"[EnemyAI] → {newState}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}
