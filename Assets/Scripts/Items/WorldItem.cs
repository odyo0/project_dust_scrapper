using DustScrapper.Interaction;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 바닥에 놓인 물건 한 묶음. E를 누르고 있으면 줍는다.
    /// 무기는 무기 슬롯으로 가고(슬롯이 다 찼으면 든 무기와 바꿔 이 자리에 내려놓는다), 나머지는 가방으로 간다.
    /// 가방에 다 안 들어가면 남은 만큼 바닥에 남는다.
    /// </summary>
    public sealed class WorldItem : Interactable
    {
        [SerializeField] ItemStack stack;

        [Tooltip("물건 모습. 진짜 그림이 생기기 전까지 물건의 임시 색으로 칠한다.")]
        [SerializeField] SpriteRenderer look;

        public ItemStack Stack => stack;

        public override string ActionName => "줍기";

        public override string TargetName =>
            stack.IsEmpty ? string.Empty :
            stack.Count > 1 ? $"{stack.Item.DisplayName} ×{stack.Count}" : stack.Item.DisplayName;

        void Start()
        {
            if (stack.IsEmpty)
            {
                Debug.LogWarning($"{name}: 빈 바닥 물건이라 없앤다.", this);
                Destroy(gameObject);
                return;
            }
            RefreshLook();
        }

        void OnValidate() => RefreshLook();

        /// <summary>담긴 물건을 바꾼다. 빈 묶음이면 바닥 물건이 사라진다.</summary>
        public void SetStack(ItemStack value)
        {
            stack = value;
            if (stack.IsEmpty)
            {
                Destroy(gameObject);
                return;
            }
            RefreshLook();
        }

        public override string BlockedReason(PlayerInteractor interactor)
        {
            if (stack.Item is WeaponData) return interactor.Loadout != null ? null : "무기를 들 수 없다";
            return interactor.Inventory != null && interactor.Inventory.HasRoomFor(stack.Item) ? null : "가방이 가득 찼다";
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (stack.Item is WeaponData)
            {
                // 든 무기와 바꿨다면 빠진 무기가 이 자리에 남는다.
                SetStack(interactor.Loadout.PutWeapon(stack));
                return;
            }

            int left = interactor.Inventory.Add(stack);
            SetStack(stack.WithCount(left));
        }

        void RefreshLook()
        {
            if (look != null && stack.Item != null) look.color = stack.Item.PlaceholderColor;
        }
    }
}
