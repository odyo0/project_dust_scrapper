using System.Collections.Generic;
using DustScrapper.Controls;
using UnityEngine;

namespace DustScrapper.Player
{
    /// <summary>
    /// 키보드·마우스 입력을 읽어 플레이어의 "의도"(어디로 가려는지, 어디를 보는지)로 정리해 둔다.
    /// Input System을 직접 만지는 곳은 여기 하나뿐이고, 나머지는 이 값만 읽는다.
    /// 가방 화면처럼 마우스를 쓰는 창이 열려 있으면(MenuOpen) 이동(달리기·살금살금 포함)만 받고, 조준·발사·마법·재장전·상호작용·슬롯은 막는다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        GameControls controls;

        /// <summary>이동 의도. 길이는 0~1.</summary>
        public Vector2 Move { get; private set; }

        /// <summary>마우스 커서의 화면 좌표(픽셀).</summary>
        public Vector2 AimScreenPosition { get; private set; }

        /// <summary>멀리 보기 키를 누르고 있는가.</summary>
        public bool LookAheadHeld { get; private set; }

        /// <summary>달리기 키를 누르고 있는가.</summary>
        public bool SprintHeld { get; private set; }

        /// <summary>살금살금 키를 누르고 있는가.</summary>
        public bool SneakHeld { get; private set; }

        /// <summary>사용/발사 키를 누르고 있는가. 한 프레임 안에 눌렀다 뗀 짧은 클릭도 누른 것으로 본다.</summary>
        public bool FireHeld { get; private set; }

        /// <summary>무기 손 특수 스킬 키(Q)를 누르고 있는가. 짧게 톡 눌러도 놓치지 않는다.</summary>
        public bool SpecialHeld { get; private set; }

        /// <summary>보조 손 기본 스킬 키(우클릭)를 누르고 있는가. 한 프레임 안에 눌렀다 뗀 짧은 클릭도 누른 것으로 본다.</summary>
        public bool SupportPrimaryHeld { get; private set; }

        /// <summary>보조 손 특수 스킬 키(E)를 누르고 있는가. 짧게 톡 눌러도 놓치지 않는다.</summary>
        public bool SupportSpecialHeld { get; private set; }

        /// <summary>이번 프레임에 재장전 키를 눌렀는가.</summary>
        public bool ReloadPressed { get; private set; }

        /// <summary>상호작용 키(줍기·뒤지기)를 누르고 있는가.</summary>
        public bool InteractHeld { get; private set; }

        /// <summary>이번 프레임에 가방 키(Tab)를 눌렀는가. 창이 열려 있어도 읽는다.</summary>
        public bool InventoryPressed { get; private set; }

        /// <summary>이번 프레임에 취소 키(Esc)를 눌렀는가. 창이 열려 있어도 읽는다.</summary>
        public bool CancelPressed { get; private set; }

        /// <summary>이번 프레임에 제작 키(C)를 눌렀는가. 창이 열려 있어도 읽는다.</summary>
        public bool CraftPressed { get; private set; }

        /// <summary>이번 프레임에 누른 소모품 단축 칸 번호(0부터, 키 4·5·6). 누르지 않았으면 -1.</summary>
        public int QuickPressed { get; private set; } = -1;

        readonly HashSet<object> openMenus = new();

        /// <summary>마우스를 쓰는 창이 하나라도 열려 있는가. 열려 있는 동안 조준점은 마지막 자리에 멈춰 있다.</summary>
        public bool MenuOpen => openMenus.Count > 0;

        /// <summary>
        /// 마우스를 쓰는 창이 열리고 닫힐 때 그 창이 자기 자신을 넘겨 부른다.
        /// 창마다 따로 기억해서, 한 창이 닫혀도 다른 창(결과 화면 등)이 열려 있으면 계속 막는다.
        /// </summary>
        public void SetMenuOpen(object menu, bool open)
        {
            if (open) openMenus.Add(menu);
            else openMenus.Remove(menu);
        }

        /// <summary>이번 프레임에 누른 무기 슬롯 번호(0부터). 누르지 않았으면 -1.</summary>
        public int SlotPressed { get; private set; } = -1;

        void Awake()
        {
            controls = new GameControls();
        }

        void OnEnable()
        {
            controls.Player.Enable();
        }

        void OnDisable()
        {
            controls.Player.Disable();
            Move = Vector2.zero;
            LookAheadHeld = false;
            SprintHeld = false;
            SneakHeld = false;
            FireHeld = false;
            SpecialHeld = false;
            SupportPrimaryHeld = false;
            SupportSpecialHeld = false;
            ReloadPressed = false;
            InteractHeld = false;
            InventoryPressed = false;
            CancelPressed = false;
            CraftPressed = false;
            SlotPressed = -1;
            QuickPressed = -1;
        }

        void OnDestroy()
        {
            controls.Dispose();
        }

        void Update()
        {
            var player = controls.Player;
            Move = Vector2.ClampMagnitude(player.Move.ReadValue<Vector2>(), 1f);
            SprintHeld = player.Sprint.IsPressed();
            SneakHeld = player.Sneak.IsPressed();
            InventoryPressed = player.Inventory.WasPressedThisFrame();
            CancelPressed = player.Cancel.WasPressedThisFrame();
            CraftPressed = player.Craft.WasPressedThisFrame();

            if (MenuOpen)
            {
                LookAheadHeld = false;
                FireHeld = false;
                SpecialHeld = false;
                SupportPrimaryHeld = false;
                SupportSpecialHeld = false;
                ReloadPressed = false;
                InteractHeld = false;
                SlotPressed = -1;
                QuickPressed = -1;
                return;
            }

            AimScreenPosition = player.Aim.ReadValue<Vector2>();
            LookAheadHeld = player.LookAhead.IsPressed();
            FireHeld = player.Fire.IsPressed() || player.Fire.WasPressedThisFrame();
            SpecialHeld = player.Special.IsPressed() || player.Special.WasPressedThisFrame();
            SupportPrimaryHeld = player.SupportPrimary.IsPressed() || player.SupportPrimary.WasPressedThisFrame();
            SupportSpecialHeld = player.SupportSpecial.IsPressed() || player.SupportSpecial.WasPressedThisFrame();
            ReloadPressed = player.Reload.WasPressedThisFrame();
            InteractHeld = player.Interact.IsPressed();
            SlotPressed = ReadSlotPressed(player);
            QuickPressed = player.Quick1.WasPressedThisFrame() ? 0 : player.Quick2.WasPressedThisFrame() ? 1 : player.Quick3.WasPressedThisFrame() ? 2 : -1;
        }

        static int ReadSlotPressed(GameControls.PlayerActions player)
        {
            if (player.Slot1.WasPressedThisFrame()) return 0;
            if (player.Slot2.WasPressedThisFrame()) return 1;
            if (player.Slot3.WasPressedThisFrame()) return 2;
            return -1;
        }
    }
}
