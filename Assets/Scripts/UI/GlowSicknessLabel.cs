using DustScrapper.Glow;
using TMPro;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>글로우병 수치("글로우병 23%"). 0이면 숨기고, 높을수록 경고색으로 바뀐다. 바뀔 때만 다시 쓴다.</summary>
    [DisallowMultipleComponent]
    public sealed class GlowSicknessLabel : MonoBehaviour
    {
        [SerializeField] GlowSickness sickness;
        [SerializeField] TMP_Text label;
        [SerializeField] Color lowColor = new(0.45f, 0.95f, 0.85f);
        [SerializeField] Color highColor = new(1f, 0.35f, 0.3f);

        void OnEnable()
        {
            sickness.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => sickness.Changed -= Refresh;

        void Refresh()
        {
            label.enabled = sickness.Value > 0f;
            label.text = $"글로우병 {Mathf.FloorToInt(sickness.Value)}%";
            label.color = Color.Lerp(lowColor, highColor, sickness.Ratio);
        }
    }
}
