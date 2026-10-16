namespace DustScrapper.Zones
{
    /// <summary>
    /// 구역 바깥벽에 난 구멍(바깥문, 유리창). 열려 있거나 깨져 있으면 구역 안팎으로 소리가 더 샌다.
    /// 구역(MapZone)은 무엇이 열렸는지만 묻고, 문인지 창인지는 모른다.
    /// </summary>
    public interface IZoneOpening
    {
        bool IsOpen { get; }
    }
}
