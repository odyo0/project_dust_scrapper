using DustScrapper.Interaction;
using DustScrapper.Player;
using UnityEngine;

namespace DustScrapper.Items
{
    /// <summary>
    /// 바닥에 놓인 물건 한 묶음. F를 누르고 있으면 줍는다.
    /// 장비는 맞는 빈 칸이 있으면 바로 끼고(무기는 무기 칸에 넣고 바로 든다), 맞는 칸이 다 차 있으면 가방으로 간다.
    /// 들고 있거나 낀 장비와 맞바꾸지 않는다(기획). 어느 칸에 먼저 끼는지는 PlayerGear.EquipOrder가 정한다
    /// (무기 칸 → 보조 칸 → 몸 장비). 끼울 칸이 없거나 장비가 아닌 물건은 가방으로 간다.
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
            PlayerGear gear = interactor.Gear;
            if (gear == null) return "가방이 가득 찼다";
            if (gear.FindEquipSlot(stack) != null) return null;
            return gear.Bag != null && gear.Bag.HasRoomFor(stack.Item) ? null : "가방이 가득 찼다";
        }

        public override void Interact(PlayerInteractor interactor)
        {
            PlayerGear gear = interactor.Gear;
            if (gear == null) return;

            IEquipSlots slot = gear.FindEquipSlot(stack);
            if (slot != null)
            {
                SetStack(slot.AutoEquip(stack));
                return;
            }

            int left = gear.Bag != null ? gear.Bag.Add(stack) : stack.Count;
            SetStack(stack.WithCount(left));
        }

        void RefreshLook()
        {
            if (look != null && stack.Item != null) look.color = stack.Item.PlaceholderColor;
        }
    }
}
