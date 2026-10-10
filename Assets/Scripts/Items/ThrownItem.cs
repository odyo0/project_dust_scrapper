using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 던진 물건(칼 등)을 실은 탄. 탄(Projectile)이 멈추면 그 자리에 실은 물건을 바닥 물건(WorldItem)으로 떨어뜨린다.
    /// 벽에 맞았으면 벽 안에 묻히지 않게 날아온 쪽으로 조금 물러난 자리에 둔다. 다시 주우면 무기 칸으로 돌아온다.
    /// 날아가기·맞히기는 탄이 하고, 이것은 "멈춘 뒤 물건으로 돌아가기"만 맡는다.
    /// </summary>
    [RequireComponent(typeof(Projectile))]
    [DisallowMultipleComponent]
    public sealed class ThrownItem : MonoBehaviour
    {
        [SerializeField] WorldItem worldItemPrefab;
        [Tooltip("멈춘 자리에서 날아온 쪽으로 물러나 내려놓는 거리 (칸)")]
        [SerializeField, Min(0f)] float backOff = 0.3f;
        [Tooltip("이 레이어(벽) 안에는 내려놓지 않는다")]
        [SerializeField] LayerMask blockers;

        Projectile projectile;
        ItemStack stack;

        void Awake()
        {
            projectile = GetComponent<Projectile>();
            projectile.Stopped += OnStopped;
        }

        void OnDestroy()
        {
            if (projectile != null) projectile.Stopped -= OnStopped;
        }

        /// <summary>실을 물건. 던지기 직전(Launch 전에) 부른다.</summary>
        public void SetStack(ItemStack value) => stack = value;

        void OnStopped(Vector2 at)
        {
            if (stack.IsEmpty) return;
            if (worldItemPrefab == null)
            {
                Debug.LogError($"{name}: 바닥 물건 프리팹이 없어 던진 {stack.Item.name}이(가) 사라졌다.", this);
                return;
            }

            Vector2 position = at - projectile.Direction * backOff;
            if (Physics2D.OverlapCircle(position, 0.15f, blockers) != null) position = at - projectile.Direction * (backOff * 2f);

            WorldItem item = Instantiate(worldItemPrefab, position, Quaternion.identity);
            item.SetStack(stack);
            stack = ItemStack.Empty;
        }
    }
}
