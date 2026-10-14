using DustScrapper.Interaction;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 기지 시설 하나의 창(상인 창, 충전 창 …)의 공통 틀. 가방 화면(InventoryScreen)이 시설을 열 때
    /// 그 시설을 보여 줄 수 있는 창(Shows)을 골라 Bind로 넘기고 켠다. 새 시설은 창 하나를 이어받아 만들고 가방 화면 목록에 넣으면 된다.
    /// </summary>
    public abstract class StationPanel : MonoBehaviour
    {
        /// <summary>이 창이 그 시설을 보여 주는가.</summary>
        public abstract bool Shows(Station station);

        /// <summary>보여 줄 시설과 쓰는 플레이어를 넘긴다. 닫힐 때는 둘 다 null.</summary>
        public abstract void Bind(Station station, PlayerInteractor player);
    }
}
