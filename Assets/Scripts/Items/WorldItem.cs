using DustScrapper.Interaction;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 바닥에 놓인 물건 한 묶음. E를 누르고 있으면 줍는다.
    /// 장비는 맞는 칸으로 간다: 무기 칸에 맞으면 무기 칸(다 찼으면 든 무기와 바꿈), 보조 칸에만 맞으면 보조 칸(차 있으면 낀 것과 바꿈),
    /// 둘 다 맞으면(스태프) 무기 칸에 빈자리가 있을 때 무기 칸, 없으면 보조 칸. 바꿔서 빠진 장비는 이 자리에 내려놓는다. 나머지는 가방으로 간다.
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
            if (stack.Item is WeaponData data)
            {
                bool canWeapon = data.FitsWeaponSlot && interactor.Loadout != null;
                bool canSupport = data.FitsSupportSlot && interactor.SupportHand != null;
                return canWeapon || canSupport ? null : "장비를 낄 수 없다";
            }
            return interactor.Inventory != null && interactor.Inventory.HasRoomFor(stack.Item) ? null : "가방이 가득 찼다";
        }

        public override void Interact(PlayerInteractor interactor)
        {
            if (stack.Item is WeaponData data)
            {
                // 든 장비와 바꿨다면 빠진 장비가 이 자리에 남는다.
                SetStack(ToWeaponSlot(data, interactor) ? interactor.Loadout.PutWeapon(stack) : interactor.SupportHand.PutItem(stack));
                return;
            }

            int left = interactor.Inventory.Add(stack);
            SetStack(stack.WithCount(left));
        }

        /// <summary>장비를 무기 칸으로 보낼지(아니면 보조 칸). 둘 다 맞으면 무기 칸에 빈자리가 있을 때만 무기 칸.</summary>
        static bool ToWeaponSlot(WeaponData data, PlayerInteractor interactor)
        {
            bool canWeapon = data.FitsWeaponSlot && interactor.Loadout != null;
            bool canSupport = data.FitsSupportSlot && interactor.SupportHand != null;
            if (!canSupport) return true;
            if (!canWeapon) return false;
            return interactor.Loadout.HasEmptySlot;
        }

        void RefreshLook()
        {
            if (look != null && stack.Item != null) look.color = stack.Item.PlaceholderColor;
        }
    }
}
