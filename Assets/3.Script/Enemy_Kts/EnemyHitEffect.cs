using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 맞았을 때 모델 둘레에 하얗게 빛나는 아웃라인이 팍 튀었다가 빠르게 사라지는 이펙트. 플레이어든 적이든
// 이 컴포넌트만 붙이면 된다 (다른 스크립트를 고칠 필요 없음).
//
// 언제 재생되나:
//  - 같은 오브젝트에 PlayerVitals 가 있으면, 그 공격 피해(PlayerVitals.Damaged)를 받을 때마다 저절로 재생된다.
//    허기로 인한 지속 피해에는 안 나온다.
//  - 플레이어의 총알(Projectile)이 이 오브젝트(또는 그 콜라이더)에 맞으면 저절로 재생된다.
//  - 그 밖의 경우(근접 공격 등)는 다른 스크립트에서 Play() 를 부르면 된다.
//
// 어떻게 그리나: HitOutline 셰이더(뒷면만 그리고 화면상에서 바깥으로 민 것)를 Graphics.RenderMesh 로 켜져 있는 모든 자식 모델
// 위에 한 번 더 그려서 만든다. 스킨드 메시는 그 프레임의 포즈를 BakeMesh 로 떠서 그린다. 씬의 렌더러 기능이나 레이어를
// 바꾸지 않아서 URP 설정을 손댈 필요가 없고, 이펙트가 도는 짧은 시간에만 비용이 든다. 색을 1 보다 밝게(HDR) 그리므로
// 볼륨에 Bloom 이 켜져 있으면 빛나 보인다.
//
// 세팅: 대상 오브젝트(플레이어 / 적 루트)에 붙이고 Outline Material 에 HitOutline 머티리얼을 연결한다.
public class EnemyHitEffect : MonoBehaviour
{
    [Tooltip("HitOutline 셰이더를 쓰는 머티리얼 (Assets/6.Materials/HitOutline)")]
    [SerializeField] private Material outlineMaterial;
    [Tooltip("아웃라인 색. 밝기는 아래 Intensity 로 올린다")]
    [SerializeField] private Color color = Color.white;
    [Tooltip("색에 곱하는 밝기(HDR). 1 보다 크면 Bloom 이 번져서 빛나 보인다")]
    [SerializeField] private float intensity = 6f;
    [Tooltip("맞은 순간의 아웃라인 두께(화면 픽셀). 이후 점점 얇아진다")]
    [SerializeField] private float widthPixels = 4f;
    [Tooltip("이펙트가 지속되는 시간(초)")]
    [SerializeField] private float duration = 0.25f;
    [Tooltip("사라지는 빠르기. 클수록 처음에 확 튀었다가 급격히 줄어든다 (1 = 일정하게 감소)")]
    [SerializeField] private float decayPower = 2.5f;
    [Tooltip("아웃라인을 그리지 않을 모델의 루트들 (예: 시야 표시 메시, 이펙트용 메시). 그 아래 전부 제외된다")]
    [SerializeField] private Transform[] excludedRoots;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int WidthId = Shader.PropertyToID("_Width");

    private EnemyController enemyController;
    private Material material;
    private float elapsed = -1f; // 음수 = 재생 중이 아님

    private readonly List<SkinnedMeshRenderer> skinnedRenderers = new List<SkinnedMeshRenderer>();
    private readonly List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
    private readonly Dictionary<SkinnedMeshRenderer, Mesh> bakedMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

    private void Awake()
    {
        TryGetComponent(out enemyController);

        if (outlineMaterial == null)
        {
            Debug.LogWarning("HitOutlineEffect: Outline Material 이 연결되지 않아 이펙트가 나오지 않습니다.", this);
        }
    }

    private void OnEnable()
    {
        if (enemyController != null)
        {
            enemyController.Damaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (enemyController != null)
        {
            enemyController.Damaged -= HandleDamaged;
        }

        // 풀에 반납됐다가 다시 켜질 때 이전 이펙트가 남아 번쩍이지 않게 한다
        elapsed = -1f;
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }

        foreach (KeyValuePair<SkinnedMeshRenderer, Mesh> entry in bakedMeshes)
        {
            if (entry.Value != null)
            {
                Destroy(entry.Value);
            }
        }

        bakedMeshes.Clear();
    }

    // 처음부터 다시 재생한다. 연속으로 맞으면 그때마다 다시 튄다.
    public void Play()
    {
        if (outlineMaterial == null || !isActiveAndEnabled)
        {
            return;
        }

        if (material == null)
        {
            // 원본 재질을 건드리지 않게 복사본을 쓴다. 처음 맞을 때 만든다 (안 맞는 적은 만들지 않는다)
            material = new Material(outlineMaterial);
        }

        elapsed = 0f;
        CollectRenderers();
    }

    private void HandleDamaged()
    {
        Play();
    }

    // 애니메이션이 이번 프레임의 포즈를 다 만든 뒤에 그려야 아웃라인이 몸과 어긋나지 않는다
    private void LateUpdate()
    {
        if (elapsed < 0f || material == null)
        {
            return;
        }

        elapsed += Time.deltaTime;
        if (duration <= 0f || elapsed >= duration)
        {
            elapsed = -1f;
            return;
        }

        float strength = Mathf.Pow(1f - elapsed / duration, Mathf.Max(0.01f, decayPower));

        Color drawColor = color * (intensity * strength);
        drawColor.a = 1f;
        material.SetColor(ColorId, drawColor);
        material.SetFloat(WidthId, Mathf.Max(1f, widthPixels * strength));

        DrawOutline();
    }

    // 이펙트가 시작될 때 지금 켜져 있는 모델들을 한 번 모아 둔다 (무기를 바꾸는 등의 변화는 다음 피격 때 반영된다)
    private void CollectRenderers()
    {
        skinnedRenderers.Clear();
        meshRenderers.Clear();

        GetComponentsInChildren(false, skinnedRenderers);
        GetComponentsInChildren(false, meshRenderers);
    }

    private bool IsExcluded(Transform target)
    {
        if (excludedRoots == null)
        {
            return false;
        }

        for (int i = 0; i < excludedRoots.Length; i++)
        {
            if (excludedRoots[i] != null && target.IsChildOf(excludedRoots[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void DrawOutline()
    {
        RenderParams renderParams = new RenderParams(material);
        renderParams.layer = gameObject.layer;
        renderParams.shadowCastingMode = ShadowCastingMode.Off;
        renderParams.receiveShadows = false;

        for (int i = 0; i < skinnedRenderers.Count; i++)
        {
            SkinnedMeshRenderer skinned = skinnedRenderers[i];
            if (skinned == null || !skinned.enabled || skinned.sharedMesh == null ||
                !skinned.gameObject.activeInHierarchy || IsExcluded(skinned.transform))
            {
                continue;
            }

            Mesh baked = GetBakedMesh(skinned);
            skinned.BakeMesh(baked, false);
            baked.RecalculateBounds();

            Matrix4x4 matrix = skinned.transform.localToWorldMatrix;
            for (int sub = 0; sub < baked.subMeshCount; sub++)
            {
                Graphics.RenderMesh(renderParams, baked, sub, matrix);
            }
        }

        for (int i = 0; i < meshRenderers.Count; i++)
        {
            MeshRenderer meshRenderer = meshRenderers[i];
            if (meshRenderer == null || !meshRenderer.enabled || !meshRenderer.gameObject.activeInHierarchy ||
                IsExcluded(meshRenderer.transform))
            {
                continue;
            }

            // 시야 메시(캐릭터 시야 표시용)는 캐릭터 모델이 아니므로 그리지 않는다
            if (meshRenderer.GetComponent<PlayerVision>() != null)
            {
                continue;
            }

            if (!meshRenderer.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
            {
                continue;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Matrix4x4 matrix = meshRenderer.localToWorldMatrix;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                Graphics.RenderMesh(renderParams, mesh, sub, matrix);
            }
        }
    }

    private Mesh GetBakedMesh(SkinnedMeshRenderer skinned)
    {
        if (!bakedMeshes.TryGetValue(skinned, out Mesh baked) || baked == null)
        {
            baked = new Mesh();
            baked.MarkDynamic();
            bakedMeshes[skinned] = baked;
        }

        return baked;
    }
}
