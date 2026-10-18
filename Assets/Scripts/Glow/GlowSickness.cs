using System;
using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Glow
{
    /// <summary>
    /// 글로우병(0~100%). 정제하지 않은 글로우를 쓰면(가루 마시기, 급한 충전) 오르고, 100%가 되면 그 자리에서 죽는다.
    /// 판이 끝나도 남고(저장), 죽으면 0으로 돌아온다(기획). 낮추는 길(Reduce)은 나중에 붙일 치료가 쓴다.
    /// 화면 표시·발광은 Changed를 듣는 쪽이 따로 맡는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GlowSickness : MonoBehaviour
    {
        [Tooltip("이 수치가 되면 죽는다 (%)")]
        [SerializeField, Min(1f)] float lethal = 100f;
        [Tooltip("비워 두면 같은 물체에서 찾는다.")]
        [SerializeField] Health health;

        public float Value { get; private set; }
        public float Lethal => lethal;

        /// <summary>0~1 (죽는 수치에 대한 비율).</summary>
        public float Ratio => Mathf.Clamp01(Value / lethal);

        public event Action Changed;

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
        }

        /// <summary>저장된 값을 그대로 넣는다(죽는지 따지지 않음).</summary>
        public void Load(float value)
        {
            Value = Mathf.Clamp(value, 0f, lethal);
            Changed?.Invoke();
        }

        public void Add(float amount)
        {
            if (amount <= 0f || (health != null && health.IsDead)) return;
            Value = Mathf.Min(lethal, Value + amount);
            Changed?.Invoke();
            if (Value >= lethal && health != null)
                health.Kill(new DamageInfo(health.Current, transform.position, Vector2.zero, 0f, gameObject));
        }

        /// <summary>낮춘다(치료). 기획 빈칸이라 아직 부르는 곳이 없다.</summary>
        public void Reduce(float amount)
        {
            if (amount <= 0f) return;
            Value = Mathf.Max(0f, Value - amount);
            Changed?.Invoke();
        }
    }
}
