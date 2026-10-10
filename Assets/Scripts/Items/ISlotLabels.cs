namespace DustScrapper.Items
{
    /// <summary>
    /// 칸마다 이름이 있는 칸 묶음(몸 장비: 모자, 상의 …). 가방 화면이 비어 있는 칸에도 무엇을 끼는 칸인지 보여 줄 때 읽는다.
    /// 이름이 없는 칸 묶음(가방, 상자, 무기 칸)은 따르지 않는다.
    /// </summary>
    public interface ISlotLabels
    {
        string GetSlotLabel(int index);
    }
}
