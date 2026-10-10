using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 맞을 때마다 피해·남은 체력·디버프·때린 쪽을 콘솔에 남긴다. 사격장 표적처럼 수치를 확인하려는 곳에만 붙인다.
    /// 플레이 화면에는 아무것도 띄우지 않는다(디버그 정보는 콘솔에 둔다는 UI 원칙).
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class DamageLog : MonoBehaviour
    {
        Health health;

        void Awake() => health = GetComponent<Health>();
        void OnEnable() => health.Damaged += OnDamaged;
        void OnDisable() => health.Damaged -= OnDamaged;

        void OnDamaged(DamageInfo info)
        {
            string effect = info.Effect != null ? $", 디버프 {info.Effect.DisplayName}" : string.Empty;
            string source = info.Source != null ? info.Source.name : "?";
            Debug.Log($"[{name}] 피해 {info.Amount:0.#} (남은 체력 {health.Current:0.#}/{health.Max:0}), 밀어내기 {info.Knockback:0.#}{effect}, 때린 쪽 {source}", this);
        }
    }
}
