using UnityEngine;

namespace DustScrapper.Navigation
{
    /// <summary>
    /// 벽 레이어에 있지만 길찾기에서는 지나갈 수 있다고 보는 것(문). 적은 문 앞까지 길을 찾아 오고, 문이 스스로 열린다.
    /// 이 표시가 붙은 물체(또는 그 부모)의 충돌체는 NavGrid가 칸 지도를 만들 때 무시한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NavPassable : MonoBehaviour
    {
    }
}
