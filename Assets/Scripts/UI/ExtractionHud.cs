using DustScrapper.Raid;
using TMPro;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 신호기를 켠 뒤 화면 위쪽에 남은 버티기 시간을 띄운다. 플레이어가 구역 밖이면 시간이 멈췄다는 것을 경고색으로 알린다.
    /// 판이 끝나면 숨긴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExtractionHud : MonoBehaviour
    {
        [SerializeField] RaidSession session;
        [Tooltip("신호기가 꺼져 있을 때 숨길 묶음. 이 스크립트가 붙은 물체 자신이면 안 된다.")]
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text timerLabel;
        [SerializeField] TMP_Text stateLabel;
        [SerializeField] Color inZoneColor = Color.white;
        [SerializeField] Color outOfZoneColor = new(1f, 0.35f, 0.3f);

        int shownSeconds = -1;
        bool shownInZone;

        void LateUpdate()
        {
            ExtractionPoint active = FindActive();
            bool show = active != null && !session.IsOver;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show) return;

            // 글자는 보이는 초나 구역 안팎이 바뀔 때만 새로 만든다.
            int seconds = Mathf.CeilToInt(active.Remaining);
            bool inZone = active.PlayerInZone;
            if (seconds == shownSeconds && inZone == shownInZone) return;
            shownSeconds = seconds;
            shownInZone = inZone;

            timerLabel.SetText("{0}:{1:00}", seconds / 60, seconds % 60);
            timerLabel.color = inZone ? inZoneColor : outOfZoneColor;
            stateLabel.text = inZone ? "탈출 신호 송출 중" : "구역 밖 - 신호 멈춤";
            stateLabel.color = timerLabel.color;
        }

        ExtractionPoint FindActive()
        {
            foreach (var point in session.ExtractionPoints)
            {
                if (point.IsActive && !point.IsCompleted) return point;
            }
            return null;
        }
    }
}
