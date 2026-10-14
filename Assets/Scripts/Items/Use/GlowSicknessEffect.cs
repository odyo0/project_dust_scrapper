using DustScrapper.Glow;
using UnityEngine;

namespace DustScrapper.Items.Use
{
    [CreateAssetMenu(menuName = "Dust Scrapper/Items/Use/Glow Sickness", fileName = "GlowSickness")]
    public sealed class GlowSicknessEffect : UseEffect
    {
        [Tooltip("오르는 글로우병 (%)")]
        [SerializeField, Min(0f)] float amount = 15f;

        public override bool Apply(GameObject user)
        {
            if (!user.TryGetComponent(out GlowSickness sickness)) return false;
            sickness.Add(amount);
            return true;
        }
    }
}
