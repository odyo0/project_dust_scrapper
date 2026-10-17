using System.Collections.Generic;
using DustScrapper.Interaction;
using DustScrapper.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 기지 시설 하나의 창(상인 창, 충전 창 …)의 공통 틀. 가방 화면(InventoryScreen)이 시설을 열 때
    /// 그 시설을 보여 줄 수 있는 창(Shows)을 골라 Bind로 넘기고 켠다. 새 시설은 창 하나를 이어받아 만들고 가방 화면 목록에 넣으면 된다.
    /// 창마다 같은 일은 여기서 한다: 방금 한 일의 결과 한 줄(잠시 뒤 사라짐), 못 쓰는 줄·버튼 흐리기,
    /// 지켜보는 칸(가방·보관함 …)이 바뀌면 그 프레임 끝에 한 번만 다시 그리기. 이어받은 창은 무엇을 보여 주고 누르면 무엇을 하는지만 쓴다.
    /// </summary>
    public abstract class StationPanel : MonoBehaviour
    {
        [Tooltip("방금 한 일의 결과 한 줄 (만든 것, 못 한 이유)")]
        [SerializeField] TMP_Text messageLabel;
        [SerializeField, Min(0f)] float messageDuration = 2f;
        [Tooltip("못 쓰는 줄·버튼의 글자 진하기")]
        [FormerlySerializedAs("unaffordableTextAlpha")]
        [SerializeField, Range(0f, 1f)] float disabledTextAlpha = 0.4f;

        readonly List<IItemSlots> watched = new();
        bool dirty;
        float messageTimer;

        /// <summary>이 창이 그 시설을 보여 주는가.</summary>
        public abstract bool Shows(Station station);

        /// <summary>보여 줄 시설과 쓰는 플레이어를 넘긴다. 닫힐 때는 둘 다 null.</summary>
        public abstract void Bind(Station station, PlayerInteractor player);

        /// <summary>창 내용을 지금 상태로 다시 쓴다. 시설이 없으면(닫힘) 비운다.</summary>
        protected abstract void Redraw();

        /// <summary>그 프레임 끝에 다시 그린다. 한 프레임에 여러 번 바뀌어도 한 번만 그린다.</summary>
        protected void MarkDirty() => dirty = true;

        /// <summary>이 칸들이 바뀌면 다시 그린다. 전에 지켜보던 칸은 놓는다. null은 건너뛴다.</summary>
        protected void Watch(params IItemSlots[] slots)
        {
            foreach (var s in watched) s.Changed -= MarkDirty;
            watched.Clear();
            foreach (var s in slots)
                if (s != null) watched.Add(s);
            foreach (var s in watched) s.Changed += MarkDirty;
        }

        protected void ShowMessage(string text)
        {
            messageLabel.text = text;
            messageTimer = string.IsNullOrEmpty(text) ? 0f : messageDuration;
        }

        /// <summary>버튼을 쓸 수 있게/없게 한다. 버튼 색은 바탕만 바뀌므로 글자도 함께 흐리게 해서 못 쓰는 줄이 한눈에 보이게 한다.</summary>
        protected void SetUsable(Button button, bool usable)
        {
            button.interactable = usable;
            button.GetComponentInChildren<TMP_Text>(true).alpha = usable ? 1f : disabledTextAlpha;
        }

        protected virtual void OnDestroy() => Watch();

        void LateUpdate()
        {
            if (dirty)
            {
                dirty = false;
                Redraw();
            }

            if (messageTimer <= 0f) return;
            // 게임 시간이 아니라 실제 시간으로 센다(창은 게임이 멈춰도 쓸 수 있게).
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f) messageLabel.text = string.Empty;
        }
    }
}
