using DustScrapper.Equipment;
using DustScrapper.Interaction;
using DustScrapper.Items;
using DustScrapper.Player;
using DustScrapper.Trade;
using DustScrapper.Weapons;
using TMPro;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 가방 화면을 열고 닫는다. Tab으로 가방(무기 슬롯 + 보조 칸 + 몸 장비 + 가방)을 열고, 상자를 열면 상자 칸까지 함께 띄운다.
    /// 상인과 거래하면 상인 창(TradePanel)을 띄우고, 상인에 보관함이 연결돼 있으면 보관함 칸도 함께 띄운다.
    /// Tab·Esc로 닫고, 연 상자·상인에서 멀어지거나 플레이어가 쓰러지면 저절로 닫힌다. 게임은 멈추지 않는다.
    /// 열려 있는 동안에는 입력 층에 "창이 마우스를 쓴다"고 알려 조준·발사를 막고 이동만 받게 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryScreen : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] Inventory bag;
        [SerializeField] WeaponLoadout weapons;
        [SerializeField] SupportHand support;
        [SerializeField] BodyEquipment body;

        [Tooltip("열고 닫을 화면 묶음. 이 스크립트가 붙은 물체 자신이면 안 된다(꺼지면 다시 켤 수 없다).")]
        [SerializeField] GameObject root;
        [SerializeField] ItemGridView weaponGrid;
        [SerializeField] ItemGridView supportGrid;
        [SerializeField] ItemGridView bodyGrid;
        [SerializeField] ItemGridView bagGrid;
        [SerializeField] ItemGridView lootGrid;
        [Tooltip("상자를 열었을 때만 보이는 상자 쪽 묶음")]
        [SerializeField] GameObject lootPanel;
        [Tooltip("상자 쪽 제목. 연 상자의 이름(보관함 등)을 쓴다.")]
        [SerializeField] TMP_Text lootTitle;
        [Tooltip("상인과 거래할 때만 보이는 상인 창")]
        [SerializeField] TradePanel tradePanel;

        [Tooltip("연 상자·상인에서 이 거리(칸)보다 멀어지면 화면을 닫는다")]
        [SerializeField, Min(0f)] float lootCloseDistance = 1.8f;

        Inventory loot;
        Trader trader;
        Transform anchor;

        public bool IsOpen => root.activeSelf;
        public IItemSlots Bag => bag;
        public IItemSlots Weapons => weapons;
        public IItemSlots Support => support;
        public IItemSlots Body => body;

        /// <summary>지금 열어 둔 상자의 칸. 없으면 null.</summary>
        public IItemSlots Loot => loot;

        /// <summary>지금 거래 중인 상인. 없으면 null.</summary>
        public Trader Trader => trader;

        void Awake()
        {
            if (root == gameObject) Debug.LogError($"{name}: root에 자기 자신을 넣으면 한 번 닫은 뒤 다시 열 수 없다. 자식 묶음을 넣는다.", this);
        }

        void Start()
        {
            weaponGrid.Bind(weapons);
            if (supportGrid != null) supportGrid.Bind(support);
            if (bodyGrid != null) bodyGrid.Bind(body);
            bagGrid.Bind(bag);
            Close();
        }

        void OnEnable()
        {
            interactor.LootOpened += OpenWithLoot;
            interactor.TradeOpened += OpenWithTrader;
        }

        void OnDisable()
        {
            interactor.LootOpened -= OpenWithLoot;
            interactor.TradeOpened -= OpenWithTrader;
        }

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
                else Open(null, null, null);
            }
            else if (IsOpen && input.CancelPressed)
            {
                Close();
            }

            if (anchor != null && Vector2.Distance(anchor.position, interactor.transform.position) > lootCloseDistance)
                Close();
        }

        void OpenWithLoot(Inventory container) => Open(container, null, container.transform);

        void OpenWithTrader(Trader t) => Open(t.Storage, t, t.transform);

        void Open(Inventory container, Trader withTrader, Transform openedFrom)
        {
            loot = container;
            trader = withTrader;
            anchor = openedFrom;
            lootPanel.SetActive(container != null);
            lootGrid.Bind(container);
            if (lootTitle != null && container != null)
            {
                var named = container.GetComponent<LootContainer>();
                lootTitle.text = named != null ? named.DisplayName : "상자";
            }
            if (tradePanel != null)
            {
                tradePanel.Bind(withTrader, bag);
                tradePanel.gameObject.SetActive(withTrader != null);
            }
            root.SetActive(true);
            input.SetMenuOpen(this, true);
        }

        void Close()
        {
            root.SetActive(false);
            loot = null;
            trader = null;
            anchor = null;
            lootGrid.Bind(null);
            if (tradePanel != null) tradePanel.Bind(null, null);
            input.SetMenuOpen(this, false);
        }
    }
}
