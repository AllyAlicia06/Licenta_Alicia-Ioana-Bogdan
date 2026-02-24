using System.Collections;
using UnityEngine;

public class TargetDetectionControl : MonoBehaviour
{
    public static TargetDetectionControl instance;

    [Header("Components")]
    public PlayerControl playerControl;

    [Space]
    [Header("Target Detection")]
    public LayerMask whatIsEnemy;
    public bool canChangeTarget = true;

    [Tooltip("Detection Range: \n Player range for detecting potential targets.")]
    [Range(0f, 15f)] public float detectionRange = 10f;
    
    [Space]
    [Header("Camera Targeting")]
    [SerializeField] private Camera cam;
    [SerializeField] private bool useAimAssist = true;

    [Tooltip("Aim assist radius near (close distance).")]
    [SerializeField] private float aimAssistRadiusNear = 0.6f;

    [Tooltip("Aim assist radius far (at max distance).")]
    [SerializeField] private float aimAssistRadiusFar = 2.0f;

    [Tooltip("Distance where we reach aimAssistRadiusFar (clamped by detectionRange).")]
    [SerializeField] private float radiusAtDistance = 10f;

    [Tooltip("How often we refresh target selection (seconds). 0.1 = 10 times/sec")]
    [SerializeField] private float refreshInterval = 0.1f;

    [Space]
    [Header("Debug")]
    public bool debug;

    private Coroutine loop;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (loop != null) StopCoroutine(loop);
        loop = StartCoroutine(RunEveryXms());
    }

    private IEnumerator RunEveryXms()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            GetEnemyInInputDirection();
        }
    }
    
    public void GetEnemyInInputDirection()
    {
        if (!canChangeTarget) return;
        if (playerControl == null) return;

        Camera c = GetCamera();
        if (c == null) return;
        
        Ray ray = c.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 rayOrigin = ray.origin;
        Vector3 rayDir = ray.direction.normalized;
        
        //direct hit
        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, detectionRange, whatIsEnemy, QueryTriggerInteraction.Ignore))
        {
            var enemyBase = hit.collider.GetComponentInParent<EnemyBase>();
            if (enemyBase != null && WithinPlayerRange(enemyBase.transform))
            {
                if (playerControl.target != enemyBase.transform)
                    playerControl.ChangeTarget(enemyBase.transform);

                return;
            }
        }
        
        //aim assist
        if (!useAimAssist) return;

        Transform best = GetEnemyByAimAssist(rayOrigin, rayDir);
        if (best != null && playerControl.target != best)
        {
            playerControl.ChangeTarget(best);
            if (debug) Debug.Log("Target (aim assist): " + best.name);
        }
    }

    private Camera GetCamera()
    {
        if (cam != null) return cam;
        cam = Camera.main;
        return cam;
    }

    private bool WithinPlayerRange(Transform t)
    {
        if (t == null) return false;
        return Vector3.Distance(playerControl.transform.position, t.position) <= detectionRange;
    }

    private Transform GetEnemyByAimAssist(Vector3 rayOrigin, Vector3 rayDir)
    {
        float radius = GetAimAssistRadius();
        Vector3 centerPoint = rayOrigin + rayDir * detectionRange;

        Collider[] cols = Physics.OverlapSphere(centerPoint, radius, whatIsEnemy, QueryTriggerInteraction.Ignore);
        if (cols == null || cols.Length == 0) return null;

        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (var col in cols)
        {
            var enemyBase = col.GetComponentInParent<EnemyBase>();
            if (enemyBase == null) continue;

            Transform enemy = enemyBase.transform;

            if (!WithinPlayerRange(enemy)) continue;

            float score = DistancePointToRay(enemy.position, rayOrigin, rayDir);
            if (score < bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }

    private float GetAimAssistRadius()
    {
        float dist = Mathf.Min(detectionRange, radiusAtDistance);
        float t = (radiusAtDistance <= 0.0001f) ? 1f : Mathf.Clamp01(dist / radiusAtDistance);
        return Mathf.Lerp(aimAssistRadiusNear, aimAssistRadiusFar, t);
    }

    private static float DistancePointToRay(Vector3 point, Vector3 rayOrigin, Vector3 rayDirNormalized)
    {
        Vector3 toPoint = point - rayOrigin;
        float proj = Vector3.Dot(toPoint, rayDirNormalized);

        if (proj <= 0f) return toPoint.magnitude;

        Vector3 closest = rayOrigin + rayDirNormalized * proj;
        return Vector3.Distance(point, closest);
    }

    private void OnDrawGizmosSelected()
    {
        if (!debug) return;

        Camera c = cam != null ? cam : Camera.main;
        if (c == null) return;

        Vector3 origin = c.transform.position;
        Vector3 dir = c.transform.forward.normalized;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + dir * detectionRange);

        if (useAimAssist)
        {
            Gizmos.color = Color.cyan;
            Vector3 centerPoint = origin + dir * detectionRange;
            Gizmos.DrawWireSphere(centerPoint, GetAimAssistRadius());
        }
    }
}