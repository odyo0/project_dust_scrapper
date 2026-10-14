using UnityEngine;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 스크롤 영역의 높이를 내용만큼 쓰되 maxHeight를 넘지 않게 한다.
    /// 칸이 적은 상자는 딱 맞는 높이로, 칸이 많은 보관함은 정해진 높이에서 스크롤되게 해서, 가방 화면이 게임 화면을 필요 이상 가리지 않는다.
    /// </summary>
    [RequireComponent(typeof(LayoutElement))]
    public sealed class ScrollHeightLimit : MonoBehaviour
    {
        [SerializeField] RectTransform content;
        [SerializeField, Min(0f)] float maxHeight = 336f;

        LayoutElement element;

        void Awake() => element = GetComponent<LayoutElement>();

        void LateUpdate()
        {
            float height = Mathf.Min(maxHeight, LayoutUtility.GetPreferredHeight(content));
            if (!Mathf.Approximately(element.preferredHeight, height)) element.preferredHeight = height;
            float width = LayoutUtility.GetPreferredWidth(content);
            if (!Mathf.Approximately(element.preferredWidth, width)) element.preferredWidth = width;
        }
    }
}
