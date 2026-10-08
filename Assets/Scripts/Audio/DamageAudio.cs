using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Audio
{
    /// <summary>맞거나 죽을 때 소리를 낸다. 맞는 쪽(Health)에 붙인다. 죽는 피해일 때는 맞는 소리 대신 죽는 소리만 낸다.</summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class DamageAudio : MonoBehaviour
    {
        [SerializeField] SoundCue damaged;
        [SerializeField] SoundCue died;

        Health health;

        void Awake() => health = GetComponent<Health>();

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void OnDamaged(DamageInfo info)
        {
            if (health.IsDead && died != null) return;
            if (damaged != null) damaged.PlayAt(info.Point);
        }

        void OnDied(DamageInfo info)
        {
            if (died != null) died.PlayAt(transform.position);
        }
    }
}
