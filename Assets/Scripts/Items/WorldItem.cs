using DustScrapper.Equipment;
using DustScrapper.Interaction;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 바닥에 놓인 물건 한 묶음. E를 누르고 있으면 줍는다.
    /// 장비는 맞는 빈 칸이 있으면 바로 끼고(무기는 무기 칸에 넣고 바로 든다), 맞는 칸이 다 차 있으면 가방으로 간다.
    /// 들고 있거나 낀 장비와 맞바꾸지 않는다(기획). 스태프처럼 두 칸에 다 맞으면 무기 칸 빈자리 → 보조 칸 빈자리 → 가방 순서.
    /// 몸 장비도 같다(그 부위 칸이 비어 있으면 착용, 차 있으면 가방). 나머지 물건은 가방으로 간다.
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
            if (FindEmptyHandSlot(interactor) != HandTarget.None) return null;
            if (stack.Item is BodyGearData && interactor.Body != null && interactor.Body.HasEmptySlotFor(stack)) return null;
            return interactor.Inventory != null && interactor.Inventory.HasRoomFor(stack.Item) ? null : "가방이 가득 찼다";
        }

        public override void Interact(PlayerInteractor interactor)
        {
            switch (FindEmptyHandSlot(interactor))
            {
                case HandTarget.WeaponSlot:
                    SetStack(interactor.Loadout.PutWeapon(stack));
                    return;
                case HandTarget.SupportSlot:
                    SetStack(interactor.SupportHand.PutItem(stack));
                    return;
            }

            if (stack.Item is BodyGearData && interactor.Body != null && interactor.Body.TryEquip(stack))
            {
                SetStack(ItemStack.Empty);
                return;
            }

            int left = interactor.Inventory.Add(stack);
            SetStack(stack.WithCount(left));
        }

        enum HandTarget { None, WeaponSlot, SupportSlot }

        /// <summary>손 장비를 넣을 빈 칸. 무기 칸 빈자리 → 보조 칸 빈자리 순서. 손 장비가 아니거나 맞는 빈 칸이 없으면 None(가방으로).</summary>
        HandTarget FindEmptyHandSlot(PlayerInteractor interactor)
        {
            if (!(stack.Item is WeaponData data)) return HandTarget.None;
            if (data.FitsWeaponSlot && interactor.Loadout != null && interactor.Loadout.HasEmptySlot) return HandTarget.WeaponSlot;
            if (data.FitsSupportSlot && interactor.SupportHand != null && interactor.SupportHand.EquippedData == null) return HandTarget.SupportSlot;
            return HandTarget.None;
        }

        void RefreshLook()
        {
            if (look != null && stack.Item != null) look.color = stack.Item.PlaceholderColor;
        }
    }
}
