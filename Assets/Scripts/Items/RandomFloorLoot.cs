using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 바닥 물건 자리. 판이 시작되면 표에서 물건 하나를 뽑아 같은 물체의 WorldItem에 넣는다.
    /// 일정 확률로 아무것도 없게 해서(emptyChance) 늘 같은 자리에 물건이 있지 않게 한다.
    /// </summary>
    [RequireComponent(typeof(WorldItem))]
    public sealed class RandomFloorLoot : MonoBehaviour
    {
        [SerializeField] LootTable table;
        [Tooltip("아무것도 없을 확률 (0~1)")]
        [SerializeField, Range(0f, 1f)] float emptyChance = 0.3f;

        // WorldItem.Start가 빈 묶음을 지우기 전에 채워야 해서 Awake에서 한다.
        void Awake()
        {
            if (table == null) return;
            ItemStack stack = Random.value < emptyChance ? ItemStack.Empty : table.RollOne();
            GetComponent<WorldItem>().SetStack(stack);
        }
    }
}
