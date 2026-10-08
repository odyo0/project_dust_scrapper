using System;
using DustScrapper.Actors;
using DustScrapper.Items;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 지금 바로 꺼내 쓸 수 있는 무기 슬롯. 슬롯마다 무기를 만들어 손에 붙여 두고, 하나만 꺼내 둔다.
    /// 판 도중에 주운 무기를 넣거나(PutWeapon) 빼낼(TakeWeapon) 수 있다.
    /// 두뇌(플레이어 입력이나 AI)는 이곳에 방아쇠·재장전·슬롯 바꾸기만 요청하고, 손에 든 무기가 무엇인지는 몰라도 된다.
    /// 주운 물건을 담는 가방(Inventory)과는 따로이고, 총이 재장전할 탄만 가방에서 꺼낸다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponLoadout : MonoBehaviour
    {
        [Tooltip("무기를 붙일 손 위치. 상체 아래에 두어 조준을 따라 돌게 한다.")]
        [SerializeField] Transform hand;
        [SerializeField] BodyAim aim;

        [Tooltip("시작할 때 슬롯마다 들 무기. 칸 수가 곧 슬롯 수다(숫자키는 1~4까지). 빈 칸은 비워 둔다. 시작 무기는 탄창이 가득 찬 채로 나온다.")]
        [SerializeField] WeaponData[] slots = new WeaponData[3];

        [Tooltip("총이 재장전할 때 탄을 꺼낼 곳(가방). 비워 두면 탄창에 든 탄만 쓴다.")]
        [SerializeField] Inventory ammoSource;

        Weapon[] weapons;
        bool triggerHeld;

        public int SlotCount => slots.Length;
        public int CurrentIndex { get; private set; } = -1;
        public Weapon Current => CurrentIndex >= 0 ? weapons[CurrentIndex] : null;
        public Vector2 AimDirection => aim.AimDirection;
        public Inventory AmmoSource => ammoSource;

        /// <summary>손에 든 무기가 바뀌었을 때. 값은 새 무기(아무것도 안 들었으면 null).</summary>
        public event Action<Weapon> CurrentChanged;

        public Weapon GetWeapon(int index) => index >= 0 && index < weapons.Length ? weapons[index] : null;

        void Awake()
        {
            if (aim == null) aim = GetComponent<BodyAim>();

            weapons = new Weapon[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) weapons[i] = CreateWeapon(slots[i]);
            }
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

        /// <summary>해당 슬롯의 무기로 바꿔 든다. 비었거나 이미 든 슬롯이면 무시한다.</summary>
        public void Equip(int index)
        {
            if (!isActiveAndEnabled && CurrentIndex >= 0) return;
            if (GetWeapon(index) == null || index == CurrentIndex) return;

            if (Current != null) Current.Holster();
            CurrentIndex = index;
            Current.Draw(triggerHeld);
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

            Weapon weapon = CreateWeapon(data);
            if (weapon == null) return stack;
            if (weapon is Gun gun) gun.SetMagazine(stack.LoadedAmmo);
            weapons[index] = weapon;
            Equip(index);
            return replaced;
        }

        /// <summary>슬롯의 무기를 빼서 물건 묶음으로 돌려준다(총은 탄창 탄 수를 함께). 빈 슬롯이면 빈 묶음.</summary>
        public ItemStack TakeWeapon(int index)
        {
            ItemStack stack = RemoveWeapon(index);
            if (CurrentIndex < 0) EquipFirstAvailable();
            return stack;
        }

        /// <summary>슬롯을 비운다. 손에 든 무기였으면 아무것도 안 든 상태가 된다(다음에 무엇을 들지는 부른 쪽이 정한다).</summary>
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
            }
            weapons[index] = null;
            Destroy(weapon.gameObject);
            return stack;
        }

        void EquipFirstAvailable()
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null) continue;
                Equip(i);
                return;
            }
            CurrentChanged?.Invoke(null);
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
