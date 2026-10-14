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
    /// 기지 시설(상인·충전대 등)을 열면 그 시설을 보여 줄 수 있는 창(StationPanel)을 띄우고, 시설에 보관함이 연결돼 있으면 보관함 칸도 함께 띄운다.
    /// Tab·Esc로 닫고, 연 상자·시설에서 멀어지거나 플레이어가 쓰러지면 저절로 닫힌다. 게임은 멈추지 않는다.
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
        [SerializeField] QuickSlots quick;
        [SerializeField] ItemUser itemUser;
        [Tooltip("C로 여는 제작(몸에 지닌 Workbench)")]
        [SerializeField] Station craftStation;

        [Tooltip("열고 닫을 화면 묶음. 이 스크립트가 붙은 물체 자신이면 안 된다(꺼지면 다시 켤 수 없다).")]
        [SerializeField] GameObject root;
        [SerializeField] ItemGridView weaponGrid;
        [SerializeField] ItemGridView supportGrid;
        [SerializeField] ItemGridView bodyGrid;
        [SerializeField] ItemGridView quickGrid;
        [SerializeField] ItemGridView bagGrid;
        [SerializeField] ItemGridView lootGrid;
        [Tooltip("상자를 열었을 때만 보이는 상자 쪽 묶음")]
        [SerializeField] GameObject lootPanel;
        [Tooltip("상자 쪽 제목. 연 상자의 이름(보관함 등)을 쓴다.")]
        [SerializeField] TMP_Text lootTitle;
        [Tooltip("시설 창들(상인 창, 충전 창 …). 연 시설을 보여 줄 수 있는 창 하나만 켠다.")]
        [SerializeField] StationPanel[] stationPanels;

        [Tooltip("연 상자·시설에서 이 거리(칸)보다 멀어지면 화면을 닫는다")]
        [SerializeField, Min(0f)] float lootCloseDistance = 1.8f;

        Inventory loot;
        Station station;
        Transform anchor;

        public bool IsOpen => root.activeSelf;
        public IItemSlots Bag => bag;
        public IItemSlots Weapons => weapons;
        public IItemSlots Support => support;
        public IItemSlots Body => body;
        public IItemSlots Quick => quick;
        public ItemUser ItemUser => itemUser;

        /// <summary>지금 열어 둔 상자의 칸. 없으면 null.</summary>
        public IItemSlots Loot => loot;

        /// <summary>지금 열어 둔 시설. 없으면 null.</summary>
        public Station Station => station;

        /// <summary>지금 거래 중인 상인. 상인이 아니면 null.</summary>
        public Trader Trader => station as Trader;

        void Awake()
        {
            if (root == gameObject) Debug.LogError($"{name}: root에 자기 자신을 넣으면 한 번 닫은 뒤 다시 열 수 없다. 자식 묶음을 넣는다.", this);
        }

        void Start()
        {
            weaponGrid.Bind(weapons);
            if (supportGrid != null) supportGrid.Bind(support);
            if (bodyGrid != null) bodyGrid.Bind(body);
            if (quickGrid != null) quickGrid.Bind(quick);
            bagGrid.Bind(bag);
            Close();
        }

        void OnEnable()
        {
            interactor.LootOpened += OpenWithLoot;
            interactor.StationOpened += OpenWithStation;
        }

        void OnDisable()
        {
            interactor.LootOpened -= OpenWithLoot;
            interactor.StationOpened -= OpenWithStation;
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
            else if (input.CraftPressed && craftStation != null)
            {
                if (IsOpen && station == craftStation) Close();
                else OpenWithStation(craftStation);
            }
            else if (IsOpen && input.CancelPressed)
            {
                Close();
            }

            if (anchor != null && Vector2.Distance(anchor.position, interactor.transform.position) > lootCloseDistance)
                Close();
        }

        void OpenWithLoot(Inventory container) => Open(container, null, container.transform);

        void OpenWithStation(Station s) => Open(s.Storage, s, s.transform);

        void Open(Inventory container, Station withStation, Transform openedFrom)
        {
            loot = container;
            station = withStation;
            anchor = openedFrom;
            lootPanel.SetActive(container != null);
            lootGrid.Bind(container);
            if (lootTitle != null && container != null)
            {
                var named = container.GetComponent<LootContainer>();
                lootTitle.text = named != null ? named.DisplayName : "상자";
            }
            BindStationPanels(withStation);
            root.SetActive(true);
            input.SetMenuOpen(this, true);
        }

        void Close()
        {
            root.SetActive(false);
            loot = null;
            station = null;
            anchor = null;
            lootGrid.Bind(null);
            BindStationPanels(null);
            input.SetMenuOpen(this, false);
        }

        void BindStationPanels(Station shown)
        {
            if (stationPanels == null) return;
            foreach (var panel in stationPanels)
            {
                bool show = shown != null && panel.Shows(shown);
                panel.Bind(show ? shown : null, interactor);
                panel.gameObject.SetActive(show);
            }
        }
    }
}
