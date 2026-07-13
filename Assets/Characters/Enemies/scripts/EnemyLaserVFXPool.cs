using System.Collections.Generic;
using UnityEngine;

public class EnemyLaserVfxPool : MonoBehaviour
{
    public static EnemyLaserVfxPool Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private ParticleSystem impactPrefab;
    [SerializeField] private ParticleSystem muzzleFlashPrefab;

    [Header("Initial Pool Sizes")]
    [SerializeField] private int impactPoolSize = 20;
    [SerializeField] private int muzzleFlashPoolSize = 15;

    [Header("Hierarchy Organization")]
    [SerializeField] private Transform availableImpactParent;
    [SerializeField] private Transform activeImpactParent;
    [SerializeField] private Transform availableMuzzleParent;
    [SerializeField] private Transform activeMuzzleParent;

    private readonly Queue<ParticleSystem> availableImpacts =
        new Queue<ParticleSystem>();

    private readonly Queue<ParticleSystem> availableMuzzleFlashes =
        new Queue<ParticleSystem>();

    private readonly HashSet<ParticleSystem> impactsInPool =
        new HashSet<ParticleSystem>();

    private readonly HashSet<ParticleSystem> muzzleFlashesInPool =
        new HashSet<ParticleSystem>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        for (int i = 0; i < impactPoolSize; i++)
            CreateImpact();

        for (int i = 0; i < muzzleFlashPoolSize; i++)
            CreateMuzzleFlash();
    }

    private ParticleSystem CreateImpact()
    {
        if (impactPrefab == null)
            return null;

        ParticleSystem effect = Instantiate(
            impactPrefab,
            availableImpactParent
        );

        effect.name = "PooledLaserImpact";
        effect.gameObject.SetActive(false);

        availableImpacts.Enqueue(effect);
        impactsInPool.Add(effect);

        return effect;
    }

    private ParticleSystem CreateMuzzleFlash()
    {
        if (muzzleFlashPrefab == null)
            return null;

        ParticleSystem effect = Instantiate(
            muzzleFlashPrefab,
            availableMuzzleParent
        );

        effect.name = "PooledLaserMuzzleFlash";
        effect.gameObject.SetActive(false);

        availableMuzzleFlashes.Enqueue(effect);
        muzzleFlashesInPool.Add(effect);

        return effect;
    }

    public ParticleSystem PlayImpact(
        Vector3 position,
        Quaternion rotation)
    {
        ParticleSystem effect = GetImpact();

        if (effect == null)
            return null;

        effect.transform.SetPositionAndRotation(
            position,
            rotation
        );

        effect.gameObject.SetActive(true);

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        effect.Play(true);

        StartCoroutine(
            ReturnWhenFinished(
                effect,
                true
            )
        );

        return effect;
    }

    public ParticleSystem PlayMuzzleFlash(
        Vector3 position,
        Quaternion rotation)
    {
        ParticleSystem effect = GetMuzzleFlash();

        if (effect == null)
            return null;

        effect.transform.SetPositionAndRotation(
            position,
            rotation
        );

        effect.gameObject.SetActive(true);

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        effect.Play(true);

        StartCoroutine(
            ReturnWhenFinished(
                effect,
                false
            )
        );

        return effect;
    }

    private ParticleSystem GetImpact()
    {
        if (availableImpacts.Count == 0)
            CreateImpact();

        if (availableImpacts.Count == 0)
            return null;

        ParticleSystem effect = availableImpacts.Dequeue();
        impactsInPool.Remove(effect);

        effect.transform.SetParent(activeImpactParent, true);

        return effect;
    }

    private ParticleSystem GetMuzzleFlash()
    {
        if (availableMuzzleFlashes.Count == 0)
            CreateMuzzleFlash();

        if (availableMuzzleFlashes.Count == 0)
            return null;

        ParticleSystem effect = availableMuzzleFlashes.Dequeue();
        muzzleFlashesInPool.Remove(effect);

        effect.transform.SetParent(activeMuzzleParent, true);

        return effect;
    }

    private System.Collections.IEnumerator ReturnWhenFinished(
        ParticleSystem effect,
        bool isImpact)
    {
        yield return new WaitUntil(
            () => effect == null || !effect.IsAlive(true)
        );

        if (effect == null)
            yield break;

        if (isImpact)
            ReturnImpact(effect);
        else
            ReturnMuzzleFlash(effect);
    }

    private void ReturnImpact(ParticleSystem effect)
    {
        if (impactsInPool.Contains(effect))
            return;

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        effect.gameObject.SetActive(false);
        effect.transform.SetParent(
            availableImpactParent,
            false
        );

        impactsInPool.Add(effect);
        availableImpacts.Enqueue(effect);
    }

    private void ReturnMuzzleFlash(ParticleSystem effect)
    {
        if (muzzleFlashesInPool.Contains(effect))
            return;

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        effect.gameObject.SetActive(false);
        effect.transform.SetParent(
            availableMuzzleParent,
            false
        );

        muzzleFlashesInPool.Add(effect);
        availableMuzzleFlashes.Enqueue(effect);
    }
}