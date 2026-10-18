using System;
using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 체력. 피해를 받아 깎이고, 0이 되면 죽었다고 알린다.
    /// 플레이어, 적, 표적, 부서지는 물건이 모두 이것 하나를 쓴다.
    /// 맞았을 때의 반응(번쩍임, 밀려남, 부활)은 다른 스크립트가 이벤트를 듣고 처리한다.
    /// 무적(구르기 …)인 동안은 피해를 받지 않는다. 밀려남·디버프도 피해를 따라오므로 같이 막힌다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxHealth = 100f;

        readonly HashSet<object> invulnerableSources = new();

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        /// <summary>지금 무적인가. 무적을 건 곳이 하나라도 남아 있으면 무적이다.</summary>
        public bool IsInvulnerable => invulnerableSources.Count > 0;

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

        /// <summary>
        /// source(건 쪽)가 무적을 켜거나 끈다. 건 쪽마다 따로 기억해서, 구르기가 끝나도 다른 곳이 건 무적은 남는다.
        /// </summary>
        public void SetInvulnerable(object source, bool invulnerable)
        {
            if (invulnerable) invulnerableSources.Add(source);
            else invulnerableSources.Remove(source);
        }

        /// <summary>피해를 준다. 이미 죽었거나, 무적이거나, 피해가 0 이하면 무시하고 false를 돌려준다.</summary>
        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || IsInvulnerable || info.Amount <= 0f) return false;

            Current = Mathf.Max(0f, Current - info.Amount);
            Damaged?.Invoke(info);
            if (IsDead) Died?.Invoke(info);
            return true;
        }

        /// <summary>
        /// 무적과 상관없이 바로 죽인다. 맞아서가 아니라 몸 안에서 오는 죽음(글로우병 …)에 쓴다.
        /// 구르는 순간에 글로우병이 차도 죽음이 사라지지 않게 피해와 길을 나눴다.
        /// </summary>
        public void Kill(DamageInfo info)
        {
            if (IsDead) return;
            Current = 0f;
            Damaged?.Invoke(info);
            Died?.Invoke(info);
        }

        public void RestoreFull()
        {
            Current = maxHealth;
            Restored?.Invoke();
        }
    }
}
