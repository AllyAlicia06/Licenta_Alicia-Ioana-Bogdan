using System.Collections;
using UnityEngine;

public class SlashVFXSpawner : MonoBehaviour
{
    [SerializeField] private Transform slashSpawnPoint;
    [SerializeField] private ParticleSystem[] slashParticles;

    [Header("Detach")]
    [SerializeField, Range(0f, 0.2f)] private float detachAfterSeconds = 0.02f;
    [SerializeField] private bool detach = true;

    private ParticleSystem activeParticle;
    private Coroutine detachCoroutine;

    public void SlashVfx_Start(int variant) //Animation Event
    {
        if (!slashSpawnPoint || slashParticles == null || slashParticles.Length == 0) return;

        variant = Mathf.Clamp(variant, 0, slashParticles.Length - 1);
        var prefab = slashParticles[variant];
        if (!prefab) return;

        if (activeParticle)
        {
            if (detachCoroutine != null) StopCoroutine(detachCoroutine);
            Destroy(activeParticle.gameObject);
            activeParticle = null;
        }

        activeParticle = Instantiate(prefab, slashSpawnPoint);
        activeParticle.transform.localPosition = Vector3.zero;
        activeParticle.transform.localRotation = Quaternion.identity;

        var main = activeParticle.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        activeParticle.Play();

        if (detach)
            detachCoroutine = StartCoroutine(DetachAfterDelay(activeParticle, detachAfterSeconds));
    }

    private IEnumerator DetachAfterDelay(ParticleSystem ps, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        else
            yield return null;

        if (!ps) yield break;

        ps.transform.SetParent(null, true);
    }

    public void SlashVfx_Stop() //Animation event
    {
        if (!activeParticle) return;

        if (detachCoroutine != null)
        {
            StopCoroutine(detachCoroutine);
            detachCoroutine = null;
        }
        
        if (detach && activeParticle.transform.parent != null)
            activeParticle.transform.SetParent(null, true);

        activeParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        float destroyAfter =
            activeParticle.main.duration + activeParticle.main.startLifetime.constantMax + 0.1f;

        Destroy(activeParticle.gameObject, destroyAfter);
        activeParticle = null;
    }
}
