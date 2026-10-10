using UnityEngine;

namespace DustScrapper.Magic
{
    /// <summary>
    /// 속성 하나(불, 어둠 등). 장비가 자기 속성으로 갖고(WeaponData.Element, 비어 있으면 무속성),
    /// 반대 손에 든 장비의 속성에 따라 마법 스킬의 이펙트가 바뀐다(SpellCast.reactsTo).
    /// 지금은 이펙트(탄 색)만 바뀌므로 색만 가진다. 속성을 늘릴 때는 코드가 아니라 이 파일을 새로 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Magic/Element", fileName = "Element")]
    public sealed class ElementData : ScriptableObject
    {
        [SerializeField] string displayName;
        [Tooltip("이 속성의 마법 이펙트 색 (지금은 탄 색)")]
        [SerializeField] Color color = Color.white;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Color Color => color;
    }
}
