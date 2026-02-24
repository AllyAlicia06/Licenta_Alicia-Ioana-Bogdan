using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    public enum State { Idle, Chase, Attack }

    [Header("Visual")]
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private GameObject activeTargetObject;

    [Header("Target")]
    [SerializeField] private string targetTag = "Player";
    private Transform target;

    [Header("Detection")]
    [SerializeField] private float viewRange = 12f;
    [SerializeField] private float loseRange = 16f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    
    [Header("Hit Reaction")]
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private float hitReactCooldown = 0.15f;
    private float lastHitTime = -999f;
    
    [Header("Attack Animation Timing")]
    [SerializeField] private float attackAnimLockTime = 1.65f;
    private float attackAnimTimer = 0f;
    private float attackTimer = 0f;
    private bool isPlayingAttackAnim = false;

    [Header("Hit Animation Timing")]
    [SerializeField] private float hitAnimLockTime = 1.25f;
    private float hitAnimTimer = 0f;
    private bool isPlayingHitAnim = false;

    [Header("Knockback")]
    [SerializeField] private float knockbackRecoverTime = 0.15f;
    [SerializeField] private float navmeshSnapRadius = 1.0f;
    private bool inKnockback = false;

    [Header("Damage")]
    [SerializeField] private Transform[] hitPoints;
    [SerializeField] private float hitRadius = 0.25f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float perPunchCooldown = 0.05f;

    private float lastPunchTime = -999f;

    private Coroutine knockbackCoroutine;
    private NavMeshAgent agent;
    private State state = State.Idle;
    
    [Header("Facing Offset")]
    [SerializeField] private float attackYawOffsetDegrees = 10f;


    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }
        
        agent.stoppingDistance = 0.05f;
        agent.autoBraking = false;

        agent.nextPosition = transform.position;
        agent.isStopped = false;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        agent.updateRotation = true;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        GameObject player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null) target = player.transform;

        ActiveTarget(false);
    }
    
    void LateUpdate()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        
        if (inKnockback || isPlayingAttackAnim || isPlayingHitAnim)
        {
            agent.nextPosition = transform.position;
            return;
        }

        transform.position = agent.nextPosition;
    }
    
    private void HardStopAgentAtCurrentPosition(bool lockRotation)
    {
        if (agent == null || !agent.enabled) return;

        agent.ResetPath();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.nextPosition = transform.position;
        agent.updateRotation = !lockRotation ? true : false;
    }
    
    void Update()
    {
        if (target == null) return;
        if (inKnockback) return;
        
        attackTimer -= Time.deltaTime;
        attackAnimTimer -= Time.deltaTime;
        hitAnimTimer -= Time.deltaTime;
        
        if (isPlayingHitAnim && hitAnimTimer <= 0f)
        {
            isPlayingHitAnim = false;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        switch (state)
        {
            case State.Idle:
                animator.SetFloat(speedParam, 0f);
                if (distance <= viewRange)
                {
                    state = State.Chase;
                    ActiveTarget(true);
                }
                break;

            case State.Chase:
                if (isPlayingHitAnim)
                {
                    HardStopAgentAtCurrentPosition(lockRotation: false);
                    animator.SetFloat(speedParam, 0f);
                    FaceTarget(10f);
                    break;
                }
                
                if (distance > loseRange)
                {
                    state = State.Idle;
                    ActiveTarget(false);
                    agent.ResetPath();
                    break;
                }

                if (distance <= attackRange && !isPlayingAttackAnim && !isPlayingHitAnim)
                {
                    state = State.Attack;
                    agent.ResetPath();
                    break;
                }

                agent.isStopped = false;
                agent.updateRotation = true;

                agent.nextPosition = transform.position;

                if (agent.enabled && agent.isOnNavMesh)
                    agent.SetDestination(target.position);

                animator.SetFloat(speedParam, agent.velocity.magnitude);
                break;

            case State.Attack:
                if (isPlayingHitAnim)
                {
                    HardStopAgentAtCurrentPosition(lockRotation: false);
                    animator.SetFloat(speedParam, 0f);
                    FaceTarget(10f);
                    break;
                }
                
                HardStopAgentAtCurrentPosition(lockRotation: true);
                animator.SetFloat(speedParam, 0f);

                FaceTarget(isPlayingAttackAnim ? 2f : 10f, attackYawOffsetDegrees);

                if (distance > attackRange && !isPlayingAttackAnim)
                {
                    state = State.Chase;
                    break;
                }

                if (attackTimer <= 0f && attackAnimTimer <= 0f && !isPlayingAttackAnim)
                {
                    attackTimer = attackCooldown;
                    attackAnimTimer = attackAnimLockTime;
                    isPlayingAttackAnim = true;
                    animator.SetTrigger(attackTrigger);
                }
                break;
        }
    }

    private void FaceTarget(float speed = 10f, float yawOffsetDeg = 0f)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir);

        if (Mathf.Abs(yawOffsetDeg) > 0.001f)
            lookRot *= Quaternion.Euler(0f, yawOffsetDeg, 0f);

        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, speed * Time.deltaTime);
    }

    public void SpawnHitVfx(Vector3 pos)
    {
        Instantiate(hitVfx, pos, Quaternion.identity);
    }

    public void ActiveTarget(bool value)
    {
        if (activeTargetObject != null)
            activeTargetObject.SetActive(value);
    }
    
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction, force));
    }
    
    private IEnumerator KnockbackRoutine(Vector3 direction, float force)
    {
        inKnockback = true;

        float duration = 0.2f;
        float elapsed = 0f;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        while (elapsed < duration)
        {
            float strength = Mathf.Lerp(force, 0f, elapsed / duration);
            Vector3 delta = direction * strength * Time.deltaTime;

            Vector3 next = transform.position + delta;
            
            if (NavMesh.SamplePosition(next, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                next = hit.position;

            transform.position = next;

            if (agent != null && agent.enabled)
                agent.nextPosition = next;

            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (agent != null && agent.enabled)
        {
            Vector3 finalPos = transform.position;

            if (NavMesh.SamplePosition(finalPos, out NavMeshHit hit, 0.5f, NavMesh.AllAreas))
                finalPos = hit.position;

            agent.Warp(finalPos);
            agent.nextPosition = finalPos;
            transform.position = finalPos;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (state == State.Attack || isPlayingAttackAnim || isPlayingHitAnim)
            {
                agent.isStopped = true;
                agent.updateRotation = false;
            }
            else
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
        }

        inKnockback = false;
        knockbackCoroutine = null;
    }
    
    public void PlayHitReaction()
    {
        if (animator == null) return;
        if (Time.time - lastHitTime < hitReactCooldown) return;
        lastHitTime = Time.time;
        
        if (isPlayingAttackAnim)
        {
            isPlayingAttackAnim = false;
            attackAnimTimer = 0f;
            attackTimer = attackCooldown;
        }
        
        isPlayingHitAnim = true;
        hitAnimTimer = hitAnimLockTime;

        HardStopAgentAtCurrentPosition(lockRotation: false);

        animator.ResetTrigger(hitTrigger);
        animator.SetTrigger(hitTrigger);
    }
    
    public void TryHitAt() //Animation event
    {
        if (Time.time - lastPunchTime < perPunchCooldown) return;
        lastPunchTime = Time.time;
        
        foreach (Transform hitPoint in hitPoints)
        {
            if (hitPoint == null) continue;
        
            Collider[] hits = Physics.OverlapSphere(hitPoint.position, hitRadius, playerLayer, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0)
            {
                //damage system goes here later
                Debug.Log($"Enemy hit player");
                return;
            }
        }
    }
    
    public void OnAttackAnimEnd() //Animation event
    {
        if (agent != null && agent.enabled)
        {
            agent.nextPosition = transform.position;
            agent.velocity = Vector3.zero;
        }
        
        isPlayingAttackAnim = false;
        attackAnimTimer = 0f;

        float distance = target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
        
        if (distance <= attackRange)
        {
            state = State.Attack;
            agent.isStopped = true;
            agent.updateRotation = false;
        }
        else
        {
            state = State.Chase;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
        }
    }
    
    public void OnHitAnimEnd() //Animation event
    {
        isPlayingHitAnim = false;
        hitAnimTimer = 0f;

        if (agent != null && agent.enabled)
        {
            agent.nextPosition = transform.position;
            agent.velocity = Vector3.zero;
        }
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        //detection ranges
        Gizmos.color = new Color(1f, 1f, 0f, 0.35f); //view range
        Gizmos.DrawWireSphere(transform.position, viewRange);

        Gizmos.color = new Color(0.6f, 0.6f, 0.6f, 0.35f); //lose range
        Gizmos.DrawWireSphere(transform.position, loseRange);

        //attack range
        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}