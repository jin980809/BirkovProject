using System.Collections.Generic;
using UnityEngine;

// 총알이 맞은 자리에 튀는 피격 이펙트를 재사용해서 재생한다.
//  - 맞은 대상이 대미지를 받는 대상(적 등)이면 SoftBody, 그 외(벽/바닥 등)는 Concrete 프리팹을 쓴다.
//  - 프리팹은 빈 오브젝트 아래에 이펙트 여러 개를 자식으로 넣은 묶음이어도 된다.
//    프리팹 밑(자식, 손자 전부)에 있는 활성 상태의 ParticleSystem 을 전부 한 번에 재생한다.
//    루트에 ParticleSystem 이 없어도 되고, 켜져 있는 자식만 재생하므로 쓰지 않을 자식은 꺼 두면 된다.
//  - 프리팹 안에 WarFX 의 CFX_AutoDestructShuriken(재생이 끝나면 오브젝트를 파괴)이 있으면 만들 때 제거한다.
//    이 풀이 끝난 이펙트를 직접 회수해서 다시 쓴다 (파티클이 전부 끝나면 비어 있는 것으로 본다).
//  - 동시에 재생 중인 수가 최대치에 닿으면 가장 오래된 것부터 다시 쓴다.
//  - 이펙트 묶음의 위쪽(+Y)이 맞은 면이 바깥으로 향하는 방향이 되도록 회전시켜 재생한다.
//    유니티 파티클(WarFX 포함)은 루트 회전이 (-90, 0, 0)인 채로 위로 뿜게 만들어져 있으므로,
//    프리팹을 자식으로 끌어다 놓기만 하면 별도 회전 조정 없이 맞은 면 바깥으로 튄다.
//
// 세팅: 씬의 최상위(움직이지 않는 곳)에 빈 오브젝트를 만들어 이 컴포넌트를 붙이고 프리팹 두 개를 연결한다.
//  - 이펙트가 이 오브젝트의 자식으로 만들어지므로 플레이어 밑이나 움직이는 오브젝트 밑에 두면 안 된다.
public class ImpactEffectPool : MonoBehaviour
{
    private class Effect
    {
        public GameObject root;
        public ParticleSystem[] systems;
        public bool inUse;
    }

    [Tooltip("대미지를 받는 대상(적)에 맞았을 때 쓰는 이펙트 프리팹. 자식으로 여러 이펙트를 넣어도 된다")]
    [SerializeField] private GameObject softBodyPrefab;
    [Tooltip("그 외 모든 곳(벽, 바닥, 엄폐물 등)에 맞았을 때 쓰는 이펙트 프리팹. 자식으로 여러 이펙트를 넣어도 된다")]
    [SerializeField] private GameObject concretePrefab;
    [Tooltip("시작할 때 미리 만들어 두는 개수(종류별)")]
    [SerializeField] private int initialSize = 8;
    [Tooltip("종류별 최대 개수. 넘치면 가장 오래된 것부터 다시 재생한다")]
    [SerializeField] private int maxSize = 40;

    private readonly List<Effect> softBodyEffects = new List<Effect>();
    private readonly List<Effect> concreteEffects = new List<Effect>();
    private int softBodyRecycleIndex;
    private int concreteRecycleIndex;

    private void Awake()
    {
        if (GetComponentInParent<PlayerController>() != null)
        {
            Debug.LogWarning(
                "ImpactEffectPool: 플레이어 계층 밑에 있습니다. 여기서 만들어지는 이펙트가 " +
                "플레이어를 따라다닐 수 있으니 최상위(움직이지 않는 곳)로 옮기세요.", this);
        }

        for (int i = 0; i < initialSize; i++)
        {
            CreateEffect(softBodyPrefab, softBodyEffects);
            CreateEffect(concretePrefab, concreteEffects);
        }
    }

    private void Update()
    {
        ReleaseFinished(softBodyEffects);
        ReleaseFinished(concreteEffects);
    }

    // position 은 맞은 지점, normal 은 맞은 면이 바깥으로 향하는 방향 (파티클이 이 방향으로 튄다)
    public void Play(bool hitDamageable, Vector3 position, Vector3 normal)
    {
        Effect effect;
        if (hitDamageable)
        {
            effect = Rent(softBodyPrefab, softBodyEffects, ref softBodyRecycleIndex);
        }
        else
        {
            effect = Rent(concretePrefab, concreteEffects, ref concreteRecycleIndex);
        }

        if (effect == null)
        {
            return;
        }

        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = Vector3.up;
        }

        effect.root.transform.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.up, normal.normalized));

        // 이미 재생 중이던 것을 다시 꺼내 쓰는 경우에도 남은 입자를 지우고 처음부터 다시 재생한다
        for (int i = 0; i < effect.systems.Length; i++)
        {
            effect.systems[i].Clear(false);
            effect.systems[i].Play(false);
        }

        effect.inUse = true;
    }

    private Effect Rent(GameObject prefab, List<Effect> effects, ref int recycleIndex)
    {
        if (prefab == null)
        {
            return null;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            if (!effects[i].inUse)
            {
                return effects[i];
            }
        }

        if (effects.Count < maxSize)
        {
            return CreateEffect(prefab, effects);
        }

        Effect oldest = effects[recycleIndex];
        recycleIndex = (recycleIndex + 1) % effects.Count;
        return oldest;
    }

    private Effect CreateEffect(GameObject prefab, List<Effect> effects)
    {
        if (prefab == null)
        {
            return null;
        }

        GameObject instance = Instantiate(prefab, transform);

        // 재생이 끝나면 오브젝트를 파괴해버리는 WarFX 스크립트는 없앤다 (회수는 이 풀이 한다)
        CFX_AutoDestructShuriken[] autoDestructs = instance.GetComponentsInChildren<CFX_AutoDestructShuriken>(true);
        for (int i = 0; i < autoDestructs.Length; i++)
        {
            Destroy(autoDestructs[i]);
        }

        // 켜져 있는 자식의 파티클만 대상으로 한다. 반복 재생(loop)으로 만들어진 것도 한 번만 재생되게 하고,
        // 만들자마자 자동 재생이 시작된 것은 멈춰서 비워 둔다.
        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(false);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule main = systems[i].main;
            main.loop = false;
            main.playOnAwake = false;
            systems[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        Effect effect = new Effect();
        effect.root = instance;
        effect.systems = systems;
        effect.inUse = false;

        effects.Add(effect);
        return effect;
    }

    // 재생 중인 이펙트의 파티클이 전부 끝났으면(방출도 입자도 없음) 다시 쓸 수 있게 돌려놓는다
    private void ReleaseFinished(List<Effect> effects)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            Effect effect = effects[i];
            if (!effect.inUse)
            {
                continue;
            }

            bool anyAlive = false;
            for (int j = 0; j < effect.systems.Length; j++)
            {
                if (effect.systems[j].IsAlive(false))
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
            {
                effect.inUse = false;
            }
        }
    }
}
