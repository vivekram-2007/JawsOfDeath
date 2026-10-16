using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public float appearRange = 10f;
    public float giveUpRange = 25f;
    public float attackRange = 1.2f;
    public float fleeDistance = 15f;
    public float turnSpeed = 10f;
    public float damage = 10f;
    [Range(0.1f, 1f)] public float hitPoint = 0.9f; // how far through the attack animation the hit lands

    [Header("Wander (after giving up)")]
    public float wanderRadius = 15f;                 // around the zombie's own spawn spot
    [Range(0.1f, 1f)] public float wanderSpeedFraction = 0.5f; // share of chase speed
    public float pauseMin = 2f;                      // idle time between wander points
    public float pauseMax = 4f;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private Renderer[] renderers;
    private Animator animator;
    private bool hasAppeared = false;
    private int lastHitLoop = -1;

    private Vector3 spawnPos;
    private float chaseSpeed;
    private bool wanderHasTarget;
    private float pauseTimer;

    public static bool playerInSafeZone = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // we rotate manually
        agent.stoppingDistance = 0f;
        chaseSpeed = agent.speed;
        spawnPos = transform.position;
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerHealth = player.GetComponent<PlayerHealth>();
        renderers = GetComponentsInChildren<Renderer>();
        SetVisible(false);
    }

    void Update()
    {
        if (player == null) return;

        // Stop everything once the player is dead
        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsAttacking", false);
            agent.isStopped = true;
            lastHitLoop = -1;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        Vector3 flat = player.position - transform.position;
        flat.y = 0;
        float flatDistance = flat.magnitude;

        // Appear once, stay visible forever after (until killed)
        if (!hasAppeared && distance <= appearRange)
        {
            hasAppeared = true;
            SetVisible(true);
        }

        if (!hasAppeared) return; // still dormant, do nothing

        // Gave up: wander slowly around the spawn spot
        if (distance > giveUpRange)
        {
            animator.SetBool("IsAttacking", false);
            lastHitLoop = -1;
            Wander();
            return;
        }

        // Back to normal speed for chase and flee
        agent.speed = chaseSpeed;
        animator.speed = 1f;
        wanderHasTarget = false;

        if (playerInSafeZone)
        {
            // Flee: run in the opposite direction from the player
            Vector3 fleeDirection = (transform.position - player.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDirection * fleeDistance;

            animator.SetBool("IsChasing", true); // reuse running animation
            animator.SetBool("IsAttacking", false);
            agent.isStopped = false;
            agent.SetDestination(fleeTarget);
            FaceDirection(fleeDirection);
            lastHitLoop = -1;
        }
        else
        {
            // Normal chase: always face the player
            FaceDirection(player.position - transform.position);

            animator.SetBool("IsChasing", true);
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (flatDistance <= attackRange)
            {
                animator.SetBool("IsAttacking", true);
                agent.isStopped = true;
                TryHitOnAnimation(flatDistance);
            }
            else
            {
                animator.SetBool("IsAttacking", false);
                lastHitLoop = -1;
            }
        }
    }

    void Wander()
    {
        agent.speed = chaseSpeed * wanderSpeedFraction;
        animator.speed = wanderSpeedFraction;   // slows the run animation to match

        // pausing between points
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            agent.isStopped = true;
            animator.SetBool("IsChasing", false);
            animator.speed = 1f;
            return;
        }

        // pick a new point near spawn
        if (!wanderHasTarget)
        {
            Vector2 r = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = spawnPos + new Vector3(r.x, 0f, r.y);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 5f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                wanderHasTarget = true;
            }
            return; // if it failed, try again next frame
        }

        animator.SetBool("IsChasing", true);
        agent.isStopped = false;

        Vector3 vel = agent.velocity;
        if (vel.sqrMagnitude > 0.05f) FaceDirection(vel);

        // reached the point: pause, then pick another
        if (!agent.pathPending && agent.remainingDistance <= 0.6f)
        {
            wanderHasTarget = false;
            pauseTimer = Random.Range(pauseMin, pauseMax);
        }
    }

    // Deals damage once per attack animation loop, at hitPoint
    void TryHitOnAnimation(float flatDistance)
    {
        AnimatorStateInfo s = animator.GetCurrentAnimatorStateInfo(0);
        if (!s.IsName("attack") || animator.IsInTransition(0))
        {
            lastHitLoop = -1;
            return;
        }

        int loop = Mathf.FloorToInt(s.normalizedTime);
        float frac = s.normalizedTime - loop;

        if (frac >= hitPoint && loop != lastHitLoop)
        {
            lastHitLoop = loop;
            if (playerHealth != null && flatDistance <= attackRange + 0.5f)
                playerHealth.TakeDamage(damage);
        }
    }

    void FaceDirection(Vector3 dir)
    {
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }
}