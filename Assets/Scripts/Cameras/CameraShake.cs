using UnityEngine;

namespace DustScrapper.Cameras
{
    /// <summary>
    /// 화면 흔들림. 카메라 따라가기(CameraFollow)가 붙은 부모 아래의 카메라에 붙여서 자기 위치(로컬)만 흔든다.
    /// 따라가기와 흔들림을 다른 물체로 나눈 이유: 같은 위치를 둘이 고치면 흔들린 위치를 따라가기가 다시 따라가서 화면이 밀린다.
    /// 두 가지를 받는다.
    /// - 충격(Trauma): 맞았을 때처럼 사방으로 덜덜 떨림. 쌓였다가 서서히 사라진다.
    /// - 튕김(Kick): 총을 쏠 때처럼 한 방향으로 툭 밀렸다 돌아옴.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraShake : MonoBehaviour
    {
        [Header("충격")]
        [Tooltip("충격이 최대일 때 흔들리는 거리 (칸)")]
        [SerializeField, Min(0f)] float maxShakeOffset = 0.35f;
        [Tooltip("충격이 1초에 줄어드는 양 (최대 1)")]
        [SerializeField, Min(0.01f)] float traumaDecay = 1.8f;
        [Tooltip("떨림의 빠르기")]
        [SerializeField, Min(0.1f)] float shakeFrequency = 22f;

        [Header("튕김")]
        [Tooltip("튕김이 쌓일 수 있는 최대 거리 (칸)")]
        [SerializeField, Min(0f)] float maxKickOffset = 0.3f;
        [Tooltip("튕긴 화면이 돌아오는 빠르기. 클수록 빨리 돌아온다.")]
        [SerializeField, Min(0.1f)] float kickReturnSharpness = 16f;

        float trauma;
        Vector2 kick;
        float noiseSeed;

        void Awake()
        {
            noiseSeed = Random.Range(0f, 100f);
        }

        /// <summary>충격을 더한다 (0~1). 흔들림 크기는 충격의 제곱이라 작은 충격은 아주 약하게, 큰 충격은 확 흔들린다.</summary>
        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        /// <summary>화면을 한 방향으로 툭 민다.</summary>
        public void Kick(Vector2 direction, float distance)
        {
            if (direction.sqrMagnitude <= 0f) return;
            kick = Vector2.ClampMagnitude(kick + direction.normalized * distance, maxKickOffset);
        }

        void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            trauma = Mathf.MoveTowards(trauma, 0f, traumaDecay * deltaTime);
            kick = Vector2.Lerp(kick, Vector2.zero, 1f - Mathf.Exp(-kickReturnSharpness * deltaTime));

            Vector2 shake = Vector2.zero;
            if (trauma > 0f)
            {
                float t = Time.time * shakeFrequency;
                float strength = trauma * trauma * maxShakeOffset;
                shake.x = (Mathf.PerlinNoise(noiseSeed, t) * 2f - 1f) * strength;
                shake.y = (Mathf.PerlinNoise(noiseSeed + 37f, t) * 2f - 1f) * strength;
            }

            Vector2 offset = shake + kick;
            transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        }
    }
}
