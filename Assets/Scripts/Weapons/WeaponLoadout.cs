using System;
using DustScrapper.Actors;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 지금 바로 꺼내 쓸 수 있는 무기 슬롯. 슬롯마다 무기를 만들어 손에 붙여 두고, 하나만 꺼내 둔다.
    /// 아무 슬롯도 들지 않은 상태가 "빈손"이다. 빈손일 때 쓸 무기(주먹)를 따로 하나 두면 그것을 꺼내 든다.
    /// 주먹은 슬롯에 들어가지 않아서 가방 화면에 보이지 않고 떨어뜨릴 수도 없다.
    /// 판 도중에 주운 무기를 넣거나(PutWeapon) 뺄 수 있고, 가방 화면에서는 가방과 같은 칸 창구(IItemSlots)로 옮겨진다.
    /// 두뇌(플레이어 입력이나 AI)는 이곳에 방아쇠·재장전·슬롯 바꾸기만 요청하고, 손에 든 무기가 무엇인지는 몰라도 된다.
    /// 주운 물건을 담는 가방(Inventory)과는 따로이고, 총이 재장전할 탄만 가방에서 꺼낸다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponLoadout : MonoBehaviour, IItemSlots
    {
        [Tooltip("무기를 붙일 손 위치. 상체 아래에 두어 조준을 따라 돌게 한다.")]
        [SerializeField] Transform hand;
        [SerializeField] BodyAim aim;

        [Tooltip("시작할 때 슬롯마다 들 무기. 칸 수가 곧 슬롯 수다(숫자키는 1~3까지). 빈 칸은 비워 둔다. 시작 무기는 탄창이 가득 찬 채로 나온다.")]
        [SerializeField] WeaponData[] slots = new WeaponData[3];

        [Tooltip("빈손일 때 드는 무기(주먹). 슬롯에 들어가지 않는다. 비워 두면 빈손일 때 아무것도 들지 않는다(공격 못 함).")]
        [SerializeField] WeaponData unarmed;

        [Tooltip("총이 재장전할 때 탄을 꺼낼 곳(가방). 비워 두면 탄창에 든 탄만 쓴다.")]
        [SerializeField] Inventory ammoSource;

        Weapon[] weapons;
        Weapon unarmedWeapon;
        bool triggerHeld;

        public int SlotCount => slots.Length;

        /// <summary>손에 든 슬롯 번호. 빈손이면 -1.</summary>
        public int CurrentIndex { get; private set; } = -1;

        /// <summary>지금 꺼내 든 무기. 빈손이면 주먹(없으면 null).</summary>
        public Weapon Current { get; private set; }
        public Vector2 AimDirection => aim.AimDirection;
        public Inventory AmmoSource => ammoSource;

        /// <summary>손에 든 무기가 바뀌었을 때. 값은 새 무기(빈손이면 주먹, 주먹도 없으면 null).</summary>
        public event Action<Weapon> CurrentChanged;

        /// <summary>슬롯에 무기가 들어오거나 빠졌을 때.</summary>
        public event Action Changed;

        public Weapon GetWeapon(int index) => index >= 0 && index < weapons.Length ? weapons[index] : null;

        void Awake()
        {
            if (aim == null) aim = GetComponent<BodyAim>();

            weapons = new Weapon[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) weapons[i] = CreateWeapon(slots[i]);
            }
            if (unarmed != null) unarmedWeapon = CreateWeapon(unarmed);
            EquipFirstAvailable();
        }

        void OnEnable()
        {
            // 죽었다 살아나는 등 꺼졌다 켜졌을 때 들고 있던 무기를 다시 꺼낸다.
            if (Current != null && !Current.gameObject.activeSelf) Current.Draw(false);
        }

        void OnDisable()
        {
            triggerHeld = false;
            if (Current == null) return;
            Current.SetTrigger(false);
            Current.Holster();
        }

        /// <summary>
        /// 해당 슬롯을 든다. 빈 슬롯이면 들고 있던 무기를 집어넣고 빈손이 된다.
        /// 이미 든 슬롯이거나 슬롯 번호가 범위 밖이면 무시한다.
        /// </summary>
        public void Equip(int index)
        {
            if (!isActiveAndEnabled && Current != null) return;
            if (index < 0 || index >= weapons.Length) return;
            if (weapons[index] == null) index = -1;
            if (index == CurrentIndex) return;
            Hold(index);
        }

        /// <summary>슬롯(-1이면 빈손)으로 바꿔 든다. 하던 동작은 집어넣으면서 취소된다.</summary>
        void Hold(int index)
        {
            if (Current != null) Current.Holster();
            CurrentIndex = index;
            Current = index >= 0 ? weapons[index] : unarmedWeapon;
            if (Current != null) Current.Draw(triggerHeld);
            CurrentChanged?.Invoke(Current);
        }

        public void SetTrigger(bool held)
        {
            triggerHeld = held;
            if (Current != null) Current.SetTrigger(held);
        }

        public void Reload()
        {
            if (Current != null) Current.Reload();
        }

        /// <summary>
        /// 무기를 슬롯에 넣고 바로 꺼내 든다. 빈 슬롯이 있으면 거기에 넣는다.
        /// 슬롯이 다 찼으면 손에 든 무기와 바꾸고, 빠진 무기를 돌려준다(바닥에 떨어뜨리는 건 부른 쪽 몫이다).
        /// </summary>
        public ItemStack PutWeapon(ItemStack stack)
        {
            if (!(stack.Item is WeaponData data))
            {
                Debug.LogError($"{name}: 무기가 아닌 물건은 슬롯에 넣을 수 없다.", this);
                return stack;
            }

            int index = Array.IndexOf(weapons, null);
            ItemStack replaced = ItemStack.Empty;
            if (index < 0)
            {
                index = CurrentIndex;
                replaced = RemoveWeapon(index);
            }
            if (index < 0) return stack;

            if (!Install(index, data, stack.LoadedAmmo)) return stack;
            Equip(index);
            Changed?.Invoke();
            return replaced;
        }

        /// <summary>그 슬롯의 무기를 물건 묶음으로 본다(총은 지금 탄창 탄 수를 함께). 빈 슬롯이면 빈 묶음.</summary>
        public ItemStack GetSlot(int index)
        {
            Weapon weapon = GetWeapon(index);
            if (weapon == null) return ItemStack.Empty;
            return new ItemStack(weapon.Data, 1, weapon is Gun gun ? gun.AmmoInMagazine : 0);
        }

        /// <summary>무기 슬롯에는 무기만 놓인다.</summary>
        public bool CanPlace(int index, ItemStack stack) => stack.Item is WeaponData;

        /// <summary>슬롯의 무기를 빼서 물건 묶음으로 돌려준다. 손에 든 무기였으면 남은 무기 중 첫 번째를(없으면 빈손을) 든다.</summary>
        public ItemStack TakeSlot(int index)
        {
            ItemStack stack = RemoveWeapon(index);
            if (stack.IsEmpty) return stack;
            // 일부러 빈손(주먹)으로 있던 중이면 그대로 둔다. 손에 든 무기가 빠졌을 때만 새로 든다.
            if (Current == null) EquipFirstAvailable();
            Changed?.Invoke();
            return stack;
        }

        /// <summary>빈 슬롯에 무기를 넣는다. 빈손이었으면 바로 든다.</summary>
        public void PlaceInEmptySlot(int index, ItemStack stack)
        {
            if (!(stack.Item is WeaponData data) || GetWeapon(index) != null) return;
            if (!Install(index, data, stack.LoadedAmmo)) return;
            if (CurrentIndex < 0) Equip(index);
            Changed?.Invoke();
        }

        bool Install(int index, WeaponData data, int loadedAmmo)
        {
            Weapon weapon = CreateWeapon(data);
            if (weapon == null) return false;
            if (weapon is Gun gun) gun.SetMagazine(loadedAmmo);
            weapons[index] = weapon;
            return true;
        }

        /// <summary>슬롯을 비운다. 손에 든 무기였으면 주먹도 꺼내지 않은 상태가 된다(다음에 무엇을 들지는 부른 쪽이 정한다).</summary>
        ItemStack RemoveWeapon(int index)
        {
            Weapon weapon = GetWeapon(index);
            if (weapon == null) return ItemStack.Empty;

            int loaded = weapon is Gun gun ? gun.AmmoInMagazine : 0;
            var stack = new ItemStack(weapon.Data, 1, loaded);

            if (index == CurrentIndex)
            {
                weapon.SetTrigger(false);
                weapon.Holster();
                CurrentIndex = -1;
                Current = null;
            }
            weapons[index] = null;
            Destroy(weapon.gameObject);
            return stack;
        }

        /// <summary>첫 번째로 무기가 있는 슬롯을 든다. 하나도 없으면 빈손.</summary>
        void EquipFirstAvailable()
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null) continue;
                Hold(i);
                return;
            }
            Hold(-1);
        }

        Weapon CreateWeapon(WeaponData data)
        {
            if (data.Prefab == null)
            {
                Debug.LogError($"{name}: {data.name}에 무기 프리팹이 없다.", data);
                return null;
            }

            Weapon weapon = Instantiate(data.Prefab, hand);
            weapon.name = data.name;
            weapon.Initialize(data, this);
            weapon.gameObject.SetActive(false);
            return weapon;
        }
    }
}
