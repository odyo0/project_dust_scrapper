using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DustScrapper.UI
{
    /// <summary>
    /// 견본 버튼을 복제해 세로 목록을 만든다(제작법 줄, 파는 물건 줄 …). 견본은 꺼 두고, 자식에 글자(TMP_Text)가 하나 있어야 한다.
    /// 창은 다시 그릴 때 Clear로 비우고 줄마다 Add한다.
    /// </summary>
    public sealed class ButtonRows
    {
        readonly Button template;
        readonly RectTransform parent;
        readonly List<Button> rows = new();

        public ButtonRows(Button template, RectTransform parent)
        {
            this.template = template;
            this.parent = parent;
            template.gameObject.SetActive(false);
        }

        public void Clear()
        {
            foreach (var row in rows) UnityEngine.Object.Destroy(row.gameObject);
            rows.Clear();
        }

        /// <summary>글자가 text이고 누르면 onClick을 부르는 줄을 맨 아래에 붙인다.</summary>
        public Button Add(string text, Action onClick)
        {
            Button row = UnityEngine.Object.Instantiate(template, parent);
            row.gameObject.SetActive(true);
            row.GetComponentInChildren<TMP_Text>(true).text = text;
            row.onClick.AddListener(() => onClick());
            rows.Add(row);
            return row;
        }
    }
}
