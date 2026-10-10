namespace DustScrapper.Equipment
{
    /// <summary>몸 장비를 끼는 부위. 부위 하나에 칸이 여러 개일 수 있다(악세서리 2칸).</summary>
    public enum BodySlot
    {
        Head,
        Top,
        Bottom,
        Cloak,
        Shoes,
        Accessory,
        /// <summary>가방(배낭). 끼면 가방 칸이 늘어난다.</summary>
        Backpack,
    }
}
