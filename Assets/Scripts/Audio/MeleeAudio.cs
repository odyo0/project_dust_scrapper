using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Audio
{
    /// <summary>
    /// 근접 무기 프리팹에 붙어서 휘두르기 단계를 소리로 바꾼다.
    /// 준비 소리는 "곧 때린다"는 예고라서, 적의 할퀴기처럼 준비가 긴 무기에 넣으면 보이지 않아도 피할 수 있다.
    /// </summary>
    [RequireComponent(typeof(MeleeWeapon))]
    [DisallowMultipleComponent]
    public sealed class MeleeAudio : MonoBehaviour
    {
        [Tooltip("준비 동작을 시작할 때 (비워 두면 없음)")]
        [SerializeField] SoundCue windup;
        [Tooltip("실제로 휘두를 때")]
        [SerializeField] SoundCue swing;

        MeleeWeapon weapon;

        void Awake() => weapon = GetComponent<MeleeWeapon>();
        void OnEnable() => weapon.PhaseChanged += OnPhaseChanged;
        void OnDisable() => weapon.PhaseChanged -= OnPhaseChanged;

        void OnPhaseChanged(MeleePhase phase)
        {
            SoundCue cue = phase switch
            {
                MeleePhase.Windup => windup,
                MeleePhase.Active => swing,
                _ => null,
            };
            if (cue != null) cue.PlayAt(transform.position);
        }
    }
}
