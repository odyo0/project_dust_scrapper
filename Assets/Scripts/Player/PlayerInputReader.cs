using DustScrapper.Controls;
using UnityEngine;

namespace DustScrapper.Player
{
    /// <summary>
    /// 키보드·마우스 입력을 읽어 플레이어의 "의도"(어디로 가려는지, 어디를 보는지)로 정리해 둔다.
    /// Input System을 직접 만지는 곳은 여기 하나뿐이고, 나머지는 이 값만 읽는다.
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
        }

        void OnDestroy()
        {
            controls.Dispose();
        }

        void Update()
        {
            var player = controls.Player;
            Move = Vector2.ClampMagnitude(player.Move.ReadValue<Vector2>(), 1f);
            AimScreenPosition = player.Aim.ReadValue<Vector2>();
            LookAheadHeld = player.LookAhead.IsPressed();
        }
    }
}
