using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 피해를 받으면 들어온 방향으로 밀려난다. 충격량으로 밀기 때문에 무거운(질량이 큰) 대상은 덜 밀린다.
    /// ActorMotor가 있으면 밀린 속도는 감속 수치만큼 자연스럽게 줄어든다.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class KnockbackReceiver : MonoBehaviour
    {
        [Tooltip("받은 밀어내기에 곱하는 값. 0이면 밀리지 않는다.")]
        [SerializeField, Min(0f)] float scale = 1f;

        Health health;
        Rigidbody2D body;

        void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
        }

        void OnEnable() => health.Damaged += OnDamaged;
        void OnDisable() => health.Damaged -= OnDamaged;

        void OnDamaged(DamageInfo info)
        {
            if (body.bodyType != RigidbodyType2D.Dynamic || info.Knockback <= 0f) return;
            body.AddForce(info.Direction * (info.Knockback * scale), ForceMode2D.Impulse);
        }
    }
}
