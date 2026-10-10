using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 디버프가 걸려 있는 동안 머리 위에 작은 표시를 띄운다(예: 비틀거리면 노란 표시가 빙글빙글 돈다).
    /// 글자 없이 "지금 저 적은 공격하지 못한다"를 바로 읽을 수 있게 하는 게임 정보다. 색은 디버프 데이터가 정한다.
    /// 몸의 회전(조준)을 따라 돌지 않게 월드 기준으로 자리와 회전을 맞춘다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusIndicator : MonoBehaviour
    {
        [SerializeField] StatusEffects effects;
        [Tooltip("머리 위에 띄울 그림 (평소에는 꺼 둔다)")]
        [SerializeField] SpriteRenderer icon;
        [Tooltip("캐릭터 중심에서 표시까지의 높이 (칸)")]
        [SerializeField] float height = 0.7f;
        [Tooltip("표시가 도는 빠르기 (도/초)")]
        [SerializeField] float spinSpeed = 360f;

        float angle;

        void Awake()
        {
            if (effects == null) effects = GetComponentInParent<StatusEffects>();
            if (icon != null) icon.enabled = false;
        }

        void LateUpdate()
        {
            if (effects == null || icon == null) return;

            Color color = effects.IndicatorColor;
            bool show = color.a > 0f;
            icon.enabled = show;
            if (!show) return;

            icon.color = color;
            angle = (angle + spinSpeed * Time.deltaTime) % 360f;
            icon.transform.SetPositionAndRotation(effects.transform.position + Vector3.up * height, Quaternion.Euler(0f, 0f, angle));
        }
    }
}
