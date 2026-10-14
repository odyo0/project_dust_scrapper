using DustScrapper.Magic;
using UnityEngine;

namespace DustScrapper.Items.Use
{
    [CreateAssetMenu(menuName = "Dust Scrapper/Items/Use/Restore Mana", fileName = "RestoreMana")]
    public sealed class RestoreManaEffect : UseEffect
    {
        [SerializeField, Min(0f)] float amount = 40f;

        public override bool Apply(GameObject user)
        {
            if (!user.TryGetComponent(out Mana mana)) return false;
            mana.Restore(amount);
            return true;
        }
    }
}
