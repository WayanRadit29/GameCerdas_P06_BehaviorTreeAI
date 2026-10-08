
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class UtilityEnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform safePoint;

    [Header("Perception")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float visionAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 10;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int lowHealthThreshold = 30;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float fleeSpeed = 5f;

    [Header("Utility Scores (Runtime)")]
    [SerializeField] private float fleeScore;
    [SerializeField] private float attackScore;
    [SerializeField] private float chaseScore;
    [SerializeField] private float patrolScore;

    [Header("Debug")]
    [SerializeField] private string currentAction = "NONE";

    private NavMeshAgent agent;
    private int currentHealth;
    private int currentPatrolIndex = 0;
    private float nextAttackTime = 0f;

    private enum UtilityAction
    {
        Patrol,
        Chase,
        Attack,
        Flee
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        bool canSeePlayer = CanSeePlayer();

        float distance = player != null
            ? Vector3.Distance(transform.position, player.position)
            : Mathf.Infinity;

        CalculateUtilityScores(canSeePlayer, distance);

        UtilityAction bestAction = SelectBestAction();

        currentAction = bestAction.ToString().ToUpperInvariant();

        ExecuteAction(bestAction);
    }

    // =====================================
    // 1. PERCEPTION
    // =====================================

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 eyePosition =
            transform.position + Vector3.up * 1.5f;

        Vector3 targetPosition =
            player.position + Vector3.up;

        Vector3 direction =
            targetPosition - eyePosition;

        float distance = direction.magnitude;

        if (distance > visionRange)
            return false;

        float angle =
            Vector3.Angle(transform.forward, direction);

        if (angle > visionAngle * 0.5f)
            return false;

        bool blocked = Physics.Raycast(
            eyePosition,
            direction.normalized,
            distance,
            obstacleMask
        );

        return !blocked;
    }

    // =====================================
    // 2. UTILITY SCORING
    // =====================================

    private void CalculateUtilityScores(
        bool canSeePlayer,
        float distance)
    {
        float healthRatio =
            (float)currentHealth / Mathf.Max(1, maxHealth);

        float thresholdRatio =
            (float)lowHealthThreshold / Mathf.Max(1, maxHealth);

        // FLEE: semakin kritis health, semakin tinggi skor.
        if (currentHealth <= lowHealthThreshold)
        {
            float urgency = 1f -
                Mathf.Clamp01(
                    healthRatio / Mathf.Max(0.01f, thresholdRatio)
                );

            fleeScore = Mathf.Lerp(0.90f, 1f, urgency);
        }
        else
        {
            fleeScore = 0f;
        }

        // ATTACK: hanya relevan jika Player terlihat
        // dan sudah berada di dalam Attack Range.
        if (canSeePlayer && distance <= attackRange)
        {
            float closeness = 1f -
                Mathf.Clamp01(distance / attackRange);

            attackScore = Mathf.Lerp(
                0.75f, 0.85f, closeness
            );
        }
        else
        {
            attackScore = 0f;
        }

        // CHASE: relevan jika Player terlihat,
        // tetapi masih berada di luar Attack Range.
        if (canSeePlayer && distance > attackRange)
        {
            float distanceFactor = Mathf.InverseLerp(
                attackRange,
                Mathf.Max(visionRange, attackRange + 0.1f),
                distance
            );

            chaseScore = Mathf.Lerp(
                0.60f, 0.75f, distanceFactor
            );
        }
        else
        {
            chaseScore = 0f;
        }

        // PATROL: default action jika tidak ada
        // kebutuhan yang lebih mendesak.
        patrolScore = 0.10f;
    }

    // =====================================
    // 3. ACTION SELECTION
    // =====================================

    private UtilityAction SelectBestAction()
    {
        float bestScore = patrolScore;
        UtilityAction bestAction = UtilityAction.Patrol;

        if (chaseScore > bestScore)
        {
            bestScore = chaseScore;
            bestAction = UtilityAction.Chase;
        }

        if (attackScore > bestScore)
        {
            bestScore = attackScore;
            bestAction = UtilityAction.Attack;
        }

        if (fleeScore > bestScore)
        {
            bestScore = fleeScore;
            bestAction = UtilityAction.Flee;
        }

        return bestAction;
    }

    private void ExecuteAction(UtilityAction action)
    {
        switch (action)
        {
            case UtilityAction.Patrol:
                Patrol();
                break;

            case UtilityAction.Chase:
                Chase();
                break;

            case UtilityAction.Attack:
                Attack();
                break;

            case UtilityAction.Flee:
                Flee();
                break;
        }
    }

    // =====================================
    // 4. ACTIONS
    // =====================================

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.2f;

        Transform target = patrolPoints[currentPatrolIndex];

        agent.SetDestination(target.position);

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance <= 0.5f)
        {
            currentPatrolIndex =
                (currentPatrolIndex + 1) % patrolPoints.Length;
        }
    }

    private void Chase()
    {
        if (player == null)
            return;

        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackRange * 0.8f;

        agent.SetDestination(player.position);
    }

    private void Attack()
    {
        if (player == null)
            return;

        agent.isStopped = true;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotation,
                10f * Time.deltaTime
            );
        }

        if (Time.time < nextAttackTime)
            return;

        Debug.Log(name + " Utility Attack! Damage = " + attackDamage);

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }

        nextAttackTime = Time.time + attackCooldown;
    }

    private void Flee()
    {
        if (safePoint == null)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.stoppingDistance = 0.5f;

        agent.SetDestination(safePoint.position);

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance <= 0.7f)
        {
            agent.isStopped = true;
        }
    }

    // =====================================
    // 5. HEALTH / TESTING
    // =====================================

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Clamp(
            currentHealth - damage,
            0,
            maxHealth
        );

        Debug.Log(name + " Health = " + currentHealth);
    }

    [ContextMenu("Test Damage 25")]
    private void TestDamage25()
    {
        TakeDamage(25);
    }

    [ContextMenu("Reset Health")]
    private void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    // =====================================
    // 6. DEBUG GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 left = Quaternion.Euler(
            0f, -visionAngle * 0.5f, 0f
        ) * transform.forward;

        Vector3 right = Quaternion.Euler(
            0f, visionAngle * 0.5f, 0f
        ) * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, left * visionRange);
        Gizmos.DrawRay(transform.position, right * visionRange);
    }
}
