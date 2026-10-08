using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 탄약. 따로 갖는 수치는 아직 없지만 종류를 나눠 둔다: 총 데이터(GunData)는 탄약만 넣을 수 있게 해서
    /// 전리품을 탄으로 잘못 연결하는 일을 막는다. 총마다 다른 탄을 쓰고, 탄 종류는 이 파일을 새로 만들어 늘린다.
    /// </summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Items/Ammo", fileName = "Ammo")]
    public sealed class AmmoData : ItemData
    {
    }
}
