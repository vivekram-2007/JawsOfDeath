using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public float appearRange = 10f;
    public float giveUpRange = 25f;
    public float attackRange = 2f;
    public float fleeDistance = 15f;

    private NavMeshAgent agent;
    private Transform player;
    private Renderer[] renderers;
    private Animator animator;
    private bool hasAppeared = false;

    public static bool playerInSafeZone = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        renderers = GetComponentsInChildren<Renderer>();
        SetVisible(false);
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Appear once, stay visible forever after (until killed)
        if (!hasAppeared && distance <= appearRange)
        {
            hasAppeared = true;
            SetVisible(true);
        }

        if (!hasAppeared) return; // still dormant, do nothing

        // Give up if player got too far away
        if (distance > giveUpRange)
        {
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsAttacking", false);
            agent.isStopped = true;
            return;
        }

        if (playerInSafeZone)
        {
            // Flee: run in the opposite direction from the player
            Vector3 fleeDirection = (transform.position - player.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDirection * fleeDistance;

            animator.SetBool("IsChasing", true); // reuse running animation
            animator.SetBool("IsAttacking", false);
            agent.isStopped = false;
            agent.SetDestination(fleeTarget);
        }
        else
        {
            // Normal chase
            animator.SetBool("IsChasing", true);
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (distance <= attackRange)
            {
                animator.SetBool("IsAttacking", true);
                agent.isStopped = true;
            }
            else
            {
                animator.SetBool("IsAttacking", false);
            }
        }
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }
}