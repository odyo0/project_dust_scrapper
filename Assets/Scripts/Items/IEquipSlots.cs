namespace DustScrapper.Items
{
    /// <summary>
    /// 물건을 "끼우는" 칸(무기 칸, 보조 칸, 몸 장비 …)의 창구. 칸 창구(IItemSlots)에 "맞는 빈 칸에 바로 끼우기"를 더했다.
    /// 바닥 물건 줍기와 가방 우클릭 빠른 옮기기가 이것만 보고 칸을 고르므로, 새 장착 칸은 이것을 따르고
    /// PlayerGear 목록에 넣기만 하면 줍기·빠른 옮기기·저장·판 결과에 저절로 붙는다.
    /// </summary>
    public interface IEquipSlots : IItemSlots
    {
        /// <summary>이 물건을 지금 바로 낄 수 있는 빈 칸이 있는가. 차 있는 칸과 맞바꾸지 않는다(기획: 주운 장비는 가방으로).</summary>
        bool CanAutoEquip(ItemStack stack);

        /// <summary>맞는 빈 칸에 낀다(바닥에서 주울 때). 끼웠으면 빈 묶음, 못 끼면 그대로 돌려준다.</summary>
        ItemStack AutoEquip(ItemStack stack);
    }
}
