using UnityEngine;

namespace DustScrapper.Lantern
{
    /// <summary>
    /// 랜턴 하나가 Space로 하는 일(적 드러내기, 점멸 이동, 보호막 …)의 틀. 랜턴 데이터(LanternData)가 이것 하나를 꽂아 쓴다.
    /// 새 랜턴은 코드를 고치지 않고 효과 데이터 파일 하나와 랜턴 데이터 파일 하나로 만든다(효과 종류가 새로 필요할 때만 이것을 이어받는다).
    /// </summary>
    public abstract class LanternEffect : ScriptableObject
    {
        /// <summary>효과를 낸다. 실제로 냈으면 true(그때만 배터리가 닳는다).</summary>
        public abstract bool Activate(GameObject user);
    }
}
