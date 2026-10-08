using UnityEngine;

namespace DustScrapper.Vision
{
    /// <summary>
    /// 플레이어 시야 밖에 있으면 자기 그림(손에 든 무기 포함)을 모두 숨긴다. 적에 붙인다.
    /// 그림을 끄는 데 renderer.enabled가 아니라 forceRenderingOff를 써서, 죽었을 때 그림을 끄는 Respawner와 서로 덮어쓰지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HideOutsideVision : MonoBehaviour
    {
        Renderer[] renderers;
        bool hidden;

        void Start()
        {
            // 무기는 WeaponLoadout이 Awake에서 손에 만들어 두므로, Start에서 모으면 무기 그림까지 들어온다.
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        void OnDisable() => SetHidden(false);

        void LateUpdate()
        {
            PlayerVision vision = PlayerVision.Active;
            SetHidden(vision != null && !vision.CanSee(transform.position));
        }

        void SetHidden(bool value)
        {
            if (renderers == null || hidden == value) return;
            hidden = value;
            foreach (var renderer in renderers)
            {
                if (renderer != null) renderer.forceRenderingOff = value;
            }
        }
    }
}
