using DustScrapper.Interaction;
using DustScrapper.Items;
using DustScrapper.Player;
using DustScrapper.Weapons;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 가방 화면을 열고 닫는다. Tab으로 가방(무기 슬롯 + 가방)을 열고, 상자를 열면 상자 칸까지 함께 띄운다.
    /// Tab·Esc로 닫고, 상자에서 멀어지거나 플레이어가 쓰러지면 저절로 닫힌다. 게임은 멈추지 않는다.
    /// 열려 있는 동안에는 입력 층에 "창이 마우스를 쓴다"고 알려 조준·발사를 막고 이동만 받게 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryScreen : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] Inventory bag;
        [SerializeField] WeaponLoadout weapons;

        [Tooltip("열고 닫을 화면 묶음. 이 스크립트가 붙은 물체 자신이면 안 된다(꺼지면 다시 켤 수 없다).")]
        [SerializeField] GameObject root;
        [SerializeField] ItemGridView weaponGrid;
        [SerializeField] ItemGridView bagGrid;
        [SerializeField] ItemGridView lootGrid;
        [Tooltip("상자를 열었을 때만 보이는 상자 쪽 묶음")]
        [SerializeField] GameObject lootPanel;

        [Tooltip("연 상자에서 이 거리(칸)보다 멀어지면 화면을 닫는다")]
        [SerializeField, Min(0f)] float lootCloseDistance = 1.8f;

        Inventory loot;

        public bool IsOpen => root.activeSelf;
        public IItemSlots Bag => bag;
        public IItemSlots Weapons => weapons;

        /// <summary>지금 열어 둔 상자의 칸. 없으면 null.</summary>
        public IItemSlots Loot => loot;

        void Awake()
        {
            if (root == gameObject) Debug.LogError($"{name}: root에 자기 자신을 넣으면 한 번 닫은 뒤 다시 열 수 없다. 자식 묶음을 넣는다.", this);
        }

        void Start()
        {
            weaponGrid.Bind(weapons);
            bagGrid.Bind(bag);
            Close();
        }

        void OnEnable() => interactor.LootOpened += OpenWithLoot;
        void OnDisable() => interactor.LootOpened -= OpenWithLoot;

        void Update()
        {
            if (!interactor.isActiveAndEnabled)
            {
                if (IsOpen) Close();
                return;
            }

            if (input.InventoryPressed)
            {
                if (IsOpen) Close();
                else Open(null);
            }
            else if (IsOpen && input.CancelPressed)
            {
                Close();
            }

            if (loot != null && Vector2.Distance(loot.transform.position, interactor.transform.position) > lootCloseDistance)
                Close();
        }

        void OpenWithLoot(Inventory container) => Open(container);

        void Open(Inventory container)
        {
            loot = container;
            lootPanel.SetActive(container != null);
            lootGrid.Bind(container);
            root.SetActive(true);
            input.SetMenuOpen(true);
        }

        void Close()
        {
            root.SetActive(false);
            loot = null;
            lootGrid.Bind(null);
            input.SetMenuOpen(false);
        }
    }
}
