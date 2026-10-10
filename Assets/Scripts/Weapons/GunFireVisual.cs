using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 총을 쏠 때의 총 자체 연출: 총 그림이 뒤로 튀었다 돌아오고, 총구 불꽃이 아주 잠깐 보인다.
    /// 화면 반동은 여기서 하지 않는다(카메라는 플레이어 쪽에서만 움직여야 해서 PlayerCombatFeedback이 맡는다).
    /// </summary>
    [RequireComponent(typeof(GunFire))]
    [DisallowMultipleComponent]
    public sealed class GunFireVisual : MonoBehaviour
    {
        [Tooltip("뒤로 튈 총 그림")]
        [SerializeField] Transform body;
        [Tooltip("총구 불꽃 그림 (평소에는 꺼 둔다)")]
        [SerializeField] SpriteRenderer muzzleFlash;

        [Tooltip("한 발 쏠 때 총이 뒤로 튀는 거리 (칸)")]
        [SerializeField, Min(0f)] float kickDistance = 0.08f;
        [Tooltip("튄 총이 제자리로 돌아오는 빠르기. 클수록 빨리 돌아온다.")]
        [SerializeField, Min(0.1f)] float returnSharpness = 25f;
        [SerializeField, Min(0f)] float flashDuration = 0.04f;

        GunFire gun;
        Vector3 bodyRestPosition;
        Vector3 flashBaseScale = Vector3.one;
        float flashTimer;

        void Awake()
        {
            gun = GetComponent<GunFire>();
            if (body != null) bodyRestPosition = body.localPosition;
            if (muzzleFlash != null)
            {
                flashBaseScale = muzzleFlash.transform.localScale;
                muzzleFlash.enabled = false;
            }
        }

        void OnEnable() => gun.Fired += OnFired;

        void OnDisable()
        {
            gun.Fired -= OnFired;
            if (body != null) body.localPosition = bodyRestPosition;
            if (muzzleFlash != null) muzzleFlash.enabled = false;
            flashTimer = 0f;
        }

        void Update()
        {
            if (body != null)
            {
                float t = 1f - Mathf.Exp(-returnSharpness * Time.deltaTime);
                body.localPosition = Vector3.Lerp(body.localPosition, bodyRestPosition, t);
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f && muzzleFlash != null) muzzleFlash.enabled = false;
            }
        }

        void OnFired(Vector2 direction)
        {
            if (body != null) body.localPosition = bodyRestPosition - Vector3.right * kickDistance;

            if (muzzleFlash != null)
            {
                // 매번 조금씩 다르게 보이도록 크기를 흔든다.
                muzzleFlash.transform.localScale = flashBaseScale * Random.Range(0.8f, 1.2f);
                muzzleFlash.enabled = true;
                flashTimer = flashDuration;
            }
        }
    }
}
