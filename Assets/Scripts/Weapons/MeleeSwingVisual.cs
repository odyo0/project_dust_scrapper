using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 근접 공격을 휘두르는 모습. MeleeAttack의 단계를 읽어 칼(주먹)을 돌린다:
    /// 준비 때 한쪽으로 들어 올리고, 휘두름 때 반대쪽까지 베고, 회복 때 제자리로 돌아온다.
    /// 휘두를 때마다 방향을 번갈아 바꿔 연타가 단조롭지 않게 한다.
    /// </summary>
    [RequireComponent(typeof(MeleeAttack))]
    [DisallowMultipleComponent]
    public sealed class MeleeSwingVisual : MonoBehaviour
    {
        [Tooltip("회전시킬 칼 그림의 축")]
        [SerializeField] Transform pivot;
        [Tooltip("휘두름 단계 동안만 보일 베기 효과 (비워 두면 없음)")]
        [SerializeField] SpriteRenderer slashEffect;

        [Tooltip("평소 칼 각도 (도). 조준 방향 기준.")]
        [SerializeField] float restAngle = -20f;

        MeleeAttack attack;

        void Awake()
        {
            attack = GetComponent<MeleeAttack>();
        }

        void OnDisable()
        {
            if (pivot != null) pivot.localRotation = Quaternion.Euler(0f, 0f, restAngle);
            if (slashEffect != null) slashEffect.enabled = false;
        }

        void LateUpdate()
        {
            if (attack.MeleeData == null || pivot == null) return;

            float half = attack.MeleeData.ArcAngle * 0.5f;
            float side = attack.SwingCount % 2 == 0 ? -1f : 1f;
            float start = half * side;
            float end = -half * side;
            float p = attack.PhaseProgress;

            float angle = attack.Phase switch
            {
                MeleePhase.Windup => Mathf.Lerp(restAngle, start, p),
                MeleePhase.Active => Mathf.Lerp(start, end, EaseOut(p)),
                MeleePhase.Recovery => Mathf.Lerp(end, restAngle, p),
                _ => restAngle,
            };
            pivot.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (slashEffect != null) slashEffect.enabled = attack.Phase == MeleePhase.Active;
        }

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
