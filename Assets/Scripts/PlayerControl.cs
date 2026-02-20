using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using StarterAssets;

public class PlayerControl : MonoBehaviour
{
    [Space]
    [Header("Components")]
    [SerializeField] private Animator anim;
    [SerializeField] private ThirdPersonController thirdPersonController;
 
    [Space]
    [Header("Combat")]
    public Transform target;
    [SerializeField] private Transform attackPos;
    [Tooltip("Offset Stoping Distance")][SerializeField] private float quickAttackDeltaDistance;
    [Tooltip("Offset Stoping Distance")][SerializeField] private float heavyAttackDeltaDistance;
    [SerializeField] private float knockbackForce = 10f; 
    [SerializeField] private float airknockbackForce = 10f; 
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float reachTime = 0.3f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float comboResetTime = 0.5f;
    [SerializeField] private float comboGraceTime = 0.2f;

    bool isAttacking = false;
    private int lightCombo = 0;
    private float lastAttackEndTime = -999f; //for grace window so it can track when the last attack ended
    private bool canCombo = false;
    private bool bufferedLight = false;
    private bool comboWindowOpenedThisStep = false;

    private Coroutine attackTimeoutCoroutine;
    
    private static readonly int HashLightAttack = Animator.StringToHash("LightAttack");
    private static readonly int HashLightCombo = Animator.StringToHash("LightCombo");
    private static readonly int HashCanCombo = Animator.StringToHash("CanCombo");
    
    [Space]
    [Header("Debug")]
    [SerializeField] private bool debug;
    
    private EnemyBase oldTarget;
    private EnemyBase currentTarget;

    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        if(target == null) return;

        if((Vector3.Distance(transform.position, target.position) >= TargetDetectionControl.instance.detectionRange))
            NoTarget();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
            Attack(0);

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.K))
            Attack(1);
    }
    
    public void Attack(int attackState)
    {
        if (isAttacking)
        {
            if (attackState == 0 && (canCombo || !comboWindowOpenedThisStep))
                bufferedLight = true;
            return;
        }
        
        if (attackState != 0)
        {
            HeavyAttack();
            return;
        }

        StartLightComboStep();
    }

    private void StartLightComboStep()
    {
        bufferedLight = false;
        comboWindowOpenedThisStep = false;
        
        if (target == null)
        {
            thirdPersonController.canMove = true;
            TargetDetectionControl.instance.canChangeTarget = true;
            return;
        }

        thirdPersonController.canMove = false;
        TargetDetectionControl.instance.canChangeTarget = false;
        
        bool inGraceWindow = Time.time - lastAttackEndTime <= comboGraceTime;
        bool comboExpired = Time.time - lastAttackEndTime > comboResetTime;
        
        if(!inGraceWindow && comboExpired && !canCombo)
            lightCombo = 0;

        lightCombo++;
        if (lightCombo > 3) lightCombo = 1;
        
        if (debug) Debug.Log($"StartLightComboStep | combo={lightCombo} | inGrace={inGraceWindow} | timeSinceEnd={Time.time - lastAttackEndTime:F2}");
        
        anim.SetInteger(HashLightCombo, lightCombo);
        MoveTowardsTarget(target.position, quickAttackDeltaDistance);
        anim.ResetTrigger(HashLightAttack);
        anim.SetTrigger(HashLightAttack);

        isAttacking = true;
        
        if (attackTimeoutCoroutine != null) StopCoroutine(attackTimeoutCoroutine);
        attackTimeoutCoroutine = StartCoroutine(AttackTimeout(2f));
    }

    private IEnumerator AttackTimeout(float timeout)
    {
        yield return new WaitForSeconds(timeout);
        if (isAttacking)
        {
            if(debug) Debug.Log("AttackTimeout fired - player was stuck");
            OnAttackEnd();
        }
    }
    
    void HeavyAttack()
    {
        if (target == null)
        {
            thirdPersonController.canMove = true;
            TargetDetectionControl.instance.canChangeTarget = true;
            return;
        }
        
        int attackIndex = Random.Range(1, 3);
        if(debug) Debug.Log(attackIndex + " heavy attack index");
        
        FaceThis(target.position);
        anim.SetBool(attackIndex == 1 ? "heavyAttack1" : "heavyAttack2", true);
        isAttacking = true;
        
        if (attackTimeoutCoroutine != null) StopCoroutine(attackTimeoutCoroutine);
        attackTimeoutCoroutine = StartCoroutine(AttackTimeout(2f));
    }

    public void OpenComboWindow() //Animation event
    {
        canCombo = true;
        comboWindowOpenedThisStep = true;
        anim.SetBool(HashCanCombo, true);

        if (bufferedLight && target != null && lightCombo < 3)
        {
            bufferedLight = false;
            StartLightComboStep();
        }
    }

    public void CloseComboWindow() //Animation event
    {
        canCombo = false;
        anim.SetBool(HashCanCombo, false);
        bufferedLight = false; //for preventing delayed clicks
    }
    
    public void OnAttackEnd() //Animation event
    {
        if (!isAttacking) return;

        if (attackTimeoutCoroutine != null)
        {
            StopCoroutine(attackTimeoutCoroutine);
            attackTimeoutCoroutine = null;
        }
        
        bufferedLight = false;
        canCombo = false;
        anim.SetBool(HashCanCombo, false);

        isAttacking = false;
        thirdPersonController.canMove = true;
        TargetDetectionControl.instance.canChangeTarget = true;
        
        comboWindowOpenedThisStep = false;
        lastAttackEndTime =  Time.time;
        
        if(debug) Debug.Log($"OnAttackEnd | combo={lightCombo} | time={Time.time: F2}");
    }
    
    public void ResetAttack() //Animation event
    {
        if (attackTimeoutCoroutine != null)
        {
            StopCoroutine(attackTimeoutCoroutine);
            attackTimeoutCoroutine =  null;
        }
        
        anim.SetBool("heavyAttack1", false);
        anim.SetBool("heavyAttack2", false);
        thirdPersonController.canMove = true;
        TargetDetectionControl.instance.canChangeTarget = true;
        isAttacking = false;
    }

    public void PerformAttack() //Animation event
    {
        Collider[] hitEnemies = Physics.OverlapSphere(attackPos.position, attackRange, enemyLayer);

        foreach (Collider enemy in hitEnemies)
        {
            Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
            EnemyBase enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyRb != null)
            {
                Vector3 knockbackDirection = enemy.transform.position - transform.position;
                knockbackDirection.y = airknockbackForce;
                //enemyRb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode.Impulse);
                //this above is for the knockback of the targets, I might need to tune it down in the future or completely remove it
                enemyBase.SpawnHitVfx(enemyBase.transform.position);
            }
        }
    }
    
    public void ChangeTarget(Transform target_)
    {
        
        if(target != null)
            oldTarget.ActiveTarget(false);
        
        target = target_;
        oldTarget = target_.GetComponent<EnemyBase>();
        currentTarget = target_.GetComponent<EnemyBase>();
        currentTarget.ActiveTarget(true);
    }

    private void NoTarget() // When player gets out of range of current Target
    {
        currentTarget.ActiveTarget(false);
        currentTarget = null;
        oldTarget = null;
        target = null;
    }
    
    public void MoveTowardsTarget(Vector3 target_, float deltaDistance)
    {
        FaceThis(target_);
        Vector3 finalPos = TargetOffset(target_, deltaDistance);
        finalPos.y = transform.position.y;
        transform.DOMove(finalPos, reachTime);
    }

    public void GetClose() //Animation event
    {
        Vector3 getCloseTarget = target != null ? target.position : oldTarget.transform.position;
        FaceThis(getCloseTarget);
        Vector3 finalPos = TargetOffset(getCloseTarget, 1.4f);
        finalPos.y = 0;
        transform.DOMove(finalPos, 0.2f);
    }

    public Vector3 TargetOffset(Vector3 target, float deltaDistance)
    {
        return Vector3.MoveTowards(target, transform.position, deltaDistance);
    }

    public void FaceThis(Vector3 target)
    {
        Quaternion lookAtRotation = Quaternion.LookRotation(target - transform.position);
        lookAtRotation.x = 0;
        lookAtRotation.z = 0;
        transform.DOLocalRotateQuaternion(lookAtRotation, 0.2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPos.position, attackRange);
    }
}
