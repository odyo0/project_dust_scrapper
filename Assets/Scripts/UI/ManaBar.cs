using DustScrapper.Magic;
using UnityEngine;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// MP 막대. 막대 길이는 RectTransform의 오른쪽 기준점(anchorMax.x)으로 조절한다(체력 막대와 같은 방식).
    /// MP가 모자라 마법이 나가지 않으면 막대 배경이 잠깐 경고색으로 깜빡여서 "왜 안 나갔는지"를 바로 알 수 있다.
    /// 모자랄 때는 채운 막대가 거의 비어 있어서 배경을 깜빡인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ManaBar : MonoBehaviour
    {
        [SerializeField] Mana mana;
        [Tooltip("현재 MP 막대")]
        [SerializeField] RectTransform fill;
        [Tooltip("MP가 모자랄 때 깜빡일 그림(막대 배경)")]
        [SerializeField] Graphic warningTarget;

        [SerializeField] Color warningColor = new(1f, 0.35f, 0.3f);
        [Tooltip("모자람 경고가 보이는 시간 (초)")]
        [SerializeField, Min(0.01f)] float warningDuration = 0.25f;

        Color normalColor;
        float warningTimer;

        void Awake()
        {
            if (warningTarget != null) normalColor = warningTarget.color;
        }

        void OnEnable()
        {
            if (mana != null) mana.SpendFailed += OnSpendFailed;
        }

        void OnDisable()
        {
            if (mana != null) mana.SpendFailed -= OnSpendFailed;
            warningTimer = 0f;
            if (warningTarget != null) warningTarget.color = normalColor;
        }

        void Update()
        {
            if (mana == null || fill == null) return;

            Vector2 max = fill.anchorMax;
            max.x = mana.Current / mana.Max;
            fill.anchorMax = max;

            if (warningTimer > 0f)
            {
                warningTimer -= Time.deltaTime;
                if (warningTarget != null) warningTarget.color = warningTimer > 0f ? warningColor : normalColor;
            }
        }

        void OnSpendFailed() => warningTimer = warningDuration;
    }
}
