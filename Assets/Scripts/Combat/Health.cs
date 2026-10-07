using System;
using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 체력. 피해를 받아 깎이고, 0이 되면 죽었다고 알린다.
    /// 플레이어, 적, 표적, 부서지는 물건이 모두 이것 하나를 쓴다.
    /// 맞았을 때의 반응(번쩍임, 밀려남, 부활)은 다른 스크립트가 이벤트를 듣고 처리한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>피해를 받았을 때. 죽는 피해일 때도 Died보다 먼저 온다.</summary>
        public event Action<DamageInfo> Damaged;

        /// <summary>체력이 0이 됐을 때. 한 번 죽으면 되살아나기 전까지 다시 오지 않는다.</summary>
        public event Action<DamageInfo> Died;

        /// <summary>체력을 다시 채웠을 때(부활 등).</summary>
        public event Action Restored;

        void Awake()
        {
            Current = maxHealth;
        }

        /// <summary>피해를 준다. 이미 죽었거나 피해가 0 이하면 무시하고 false를 돌려준다.</summary>
        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || info.Amount <= 0f) return false;

            Current = Mathf.Max(0f, Current - info.Amount);
            Damaged?.Invoke(info);
            if (IsDead) Died?.Invoke(info);
            return true;
        }

        public void RestoreFull()
        {
            Current = maxHealth;
            Restored?.Invoke();
        }
    }
}
