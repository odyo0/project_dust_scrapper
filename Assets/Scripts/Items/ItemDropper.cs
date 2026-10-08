using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 물건 묶음을 이 물체 발밑에 바닥 물건(WorldItem)으로 내려놓는다. 가방 화면에서 밖으로 끌어 버릴 때 쓴다.
    /// 여러 개를 버려도 한 점에 겹치지 않게 조금 흩뜨리고, 벽 안으로 들어가면 발밑에 둔다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemDropper : MonoBehaviour
    {
        [SerializeField] WorldItem worldItemPrefab;

        [Tooltip("내려놓을 때 흩어지는 거리 (칸)")]
        [SerializeField, Min(0f)] float scatter = 0.35f;

        [Tooltip("이 레이어(벽) 안에는 내려놓지 않는다")]
        [SerializeField] LayerMask blockers;

        public void Drop(ItemStack stack)
        {
            if (stack.IsEmpty) return;
            if (worldItemPrefab == null)
            {
                Debug.LogError($"{name}: 바닥 물건 프리팹이 없어 {stack.Item.name}을(를) 내려놓지 못했다.", this);
                return;
            }

            Vector2 origin = transform.position;
            Vector2 position = origin + Random.insideUnitCircle * scatter;
            if (Physics2D.OverlapCircle(position, 0.2f, blockers) != null) position = origin;

            WorldItem item = Instantiate(worldItemPrefab, position, Quaternion.identity);
            item.SetStack(stack);
        }
    }
}
