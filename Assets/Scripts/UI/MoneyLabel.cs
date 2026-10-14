using DustScrapper.Progress;
using TMPro;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>지금 가진 돈을 보여 준다. 돈이 바뀔 때만 다시 쓴다. 돈 단위 이름은 기획 빈칸이라 "돈"으로만 적는다.</summary>
    [DisallowMultipleComponent]
    public sealed class MoneyLabel : MonoBehaviour
    {
        [SerializeField] TMP_Text label;

        void OnEnable()
        {
            Wallet.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => Wallet.Changed -= Refresh;

        void Refresh() => label.text = $"돈 {Wallet.Money:N0}";
    }
}
