using DustScrapper.Actors;
using UnityEngine;

namespace DustScrapper.Items.Use
{
    /// <summary>조준한 자리(최대 거리까지, 벽 앞에서 멈춤)에 물체를 놓는다. 연막 등.</summary>
    [CreateAssetMenu(menuName = "Dust Scrapper/Items/Use/Throw", fileName = "Throw")]
    public sealed class ThrowEffect : UseEffect
    {
        [SerializeField] GameObject prefab;
        [SerializeField, Min(0f)] float maxDistance = 6f;
        [Tooltip("이 레이어(벽)에 닿으면 그 앞에 떨어진다")]
        [SerializeField] LayerMask blockers;

        public override bool Apply(GameObject user)
        {
            if (prefab == null) return false;
            Vector2 from = user.transform.position;
            Vector2 target = from + Vector2.right;
            if (user.TryGetComponent(out BodyAim aim)) target = aim.AimPoint != Vector2.zero ? aim.AimPoint : from + aim.AimDirection;

            Vector2 offset = Vector2.ClampMagnitude(target - from, maxDistance);
            RaycastHit2D hit = Physics2D.Raycast(from, offset.normalized, offset.magnitude, blockers);
            Vector2 landing = hit.collider != null ? hit.point - offset.normalized * 0.3f : from + offset;
            Instantiate(prefab, landing, Quaternion.identity);
            return true;
        }
    }
}
