using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 주울 수 있는 모든 물건의 공통 데이터. 가치만 있는 전리품은 이것을 그대로 쓰고,
    /// 탄약(AmmoData)·무기(WeaponData)는 이것을 이어받아 자기 수치를 더한다.
    /// 그래서 전리품·탄약·무기가 모두 같은 가방 칸과 같은 바닥 물건(WorldItem)으로 다뤄진다.
    /// 물건을 늘릴 때는 코드가 아니라 이 데이터 파일을 새로 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Items/Loot", fileName = "Loot")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("저장 파일에 적는 이름표. 한 번 정하면 바꾸지 않는다(바꾸면 저장된 물건을 못 찾는다). 비우면 파일 이름을 쓴다.")]
        [SerializeField] string saveId;

        [SerializeField] string displayName;

        [Tooltip("가방 한 칸에 겹쳐 담을 수 있는 최대 개수. 1이면 겹치지 않는다.")]
        [SerializeField, Min(1)] int maxStack = 1;

        [Tooltip("한 개의 가치. 가져온 것·잃은 것을 셀 때 쓴다.")]
        [SerializeField, Min(0)] int value;

        [Tooltip("진짜 그림이 생기기 전까지 바닥과 가방에서 쓰는 임시 색")]
        [SerializeField] Color placeholderColor = Color.white;

        public string SaveId => string.IsNullOrEmpty(saveId) ? name : saveId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public virtual int MaxStack => maxStack;
        public int Value => value;
        public Color PlaceholderColor => placeholderColor;
    }
}
