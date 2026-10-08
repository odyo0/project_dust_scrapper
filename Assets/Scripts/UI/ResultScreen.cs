using System.Text;
using DustScrapper.Player;
using DustScrapper.Raid;
using DustScrapper.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 판이 끝나면 결과를 보여 준다: 탈출 성공/쓰러짐, 판 시간, 가져온 것(또는 잃은 것) 목록과 가치 합계, 다시 시작 버튼.
    /// 보이는 동안 입력 층에 창이 열렸다고 알려 커서를 보인다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultScreen : MonoBehaviour
    {
        [SerializeField] RaidSession session;
        [Tooltip("결과가 나올 때 켤 묶음. 이 스크립트가 붙은 물체 자신이면 안 된다.")]
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text listHeaderLabel;
        [SerializeField] TMP_Text listLabel;
        [SerializeField] TMP_Text totalLabel;
        [SerializeField] Button restartButton;

        [SerializeField] Color extractedColor = new(0.6f, 0.95f, 0.6f);
        [SerializeField] Color diedColor = new(1f, 0.35f, 0.3f);

        PlayerInputReader input;

        void Awake()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) input = player.GetComponent<PlayerInputReader>();
            restartButton.onClick.AddListener(session.Restart);
            root.SetActive(false);
        }

        void OnEnable() => session.Ended += Show;
        void OnDisable() => session.Ended -= Show;

        void Show(RaidResult result)
        {
            titleLabel.text = result.Extracted ? "탈출 성공" : "쓰러졌다";
            titleLabel.color = result.Extracted ? extractedColor : diedColor;

            int seconds = Mathf.FloorToInt(result.Duration);
            timeLabel.text = $"판 시간 {seconds / 60}:{seconds % 60:00}";

            listHeaderLabel.text = result.Extracted ? "가져온 것" : "잃은 것";
            var list = new StringBuilder();
            foreach (var stack in result.Items)
            {
                list.Append(stack.Item.DisplayName);
                if (stack.Count > 1) list.Append(" ×").Append(stack.Count);
                if (stack.Item is GunData gun) list.Append("  (탄창 ").Append(stack.LoadedAmmo).Append('/').Append(gun.MagazineSize).Append(')');
                list.Append('\n');
            }
            listLabel.text = result.Items.Count > 0 ? list.ToString() : "없음";
            totalLabel.text = $"가치 합계 {result.TotalValue}";

            root.SetActive(true);
            if (input != null) input.SetMenuOpen(this, true);
            // 키보드(Enter·Space)로도 누를 수 있게 버튼을 골라 둔다.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        }
    }
}
