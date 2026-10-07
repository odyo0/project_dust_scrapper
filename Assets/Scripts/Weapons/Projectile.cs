using System.Collections.Generic;
using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 날아가는 탄. 매 프레임 이번에 갈 거리만큼 앞을 광선으로 검사해서, 빠른 탄이 얇은 벽을 뚫고 지나가지 않게 한다.
    /// 무언가에 맞으면 체력이 있으면 피해를 주고, 맞은 자리에 효과를 남기고 사라진다.
    /// 쏜 사람 자신에게는 맞지 않는다.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        static readonly List<RaycastHit2D> Hits = new();
        static readonly ContactFilter2D SolidOnly = new() { useTriggers = false };

        [Tooltip("맞은 자리에 남길 효과 (비워 두면 없음)")]
        [SerializeField] GameObject impactEffect;

        Vector2 direction;
        float speed;
        float remainingRange;
        float damage;
        float knockback;
        GameObject source;
        Transform sourceRoot;

        /// <param name="start">출발점(쏜 캐릭터 중심)</param>
        /// <param name="instantTravel">출발하자마자 한 번에 이동할 거리(총구까지)</param>
        public void Launch(Vector2 start, Vector2 direction, float instantTravel, float speed, float range,
            float damage, float knockback, GameObject source)
        {
            this.direction = direction.normalized;
            this.speed = speed;
            this.damage = damage;
            this.knockback = knockback;
            this.source = source;
            sourceRoot = source != null ? source.transform : null;
            remainingRange = range;

            transform.SetPositionAndRotation(start, Quaternion.FromToRotation(Vector3.right, this.direction));
            Advance(instantTravel);
        }

        void Update()
        {
            Advance(speed * Time.deltaTime);
        }

        void Advance(float distance)
        {
            distance = Mathf.Min(distance, remainingRange);
            Vector2 position = transform.position;

            if (TryFindHit(position, distance, out RaycastHit2D hit))
            {
                transform.position = hit.point;
                OnHit(hit);
                return;
            }

            transform.position = position + direction * distance;
            remainingRange -= distance;
            if (remainingRange <= 0f) Destroy(gameObject);
        }

        bool TryFindHit(Vector2 position, float distance, out RaycastHit2D closest)
        {
            closest = default;
            if (distance <= 0f) return false;

            int count = Physics2D.Raycast(position, direction, SolidOnly, Hits, distance);
            float closestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = Hits[i];
                if (sourceRoot != null && hit.collider.transform.IsChildOf(sourceRoot)) continue;
                if (hit.distance >= closestDistance) continue;
                closest = hit;
                closestDistance = hit.distance;
            }
            return closestDistance < float.MaxValue;
        }

        void OnHit(RaycastHit2D hit)
        {
            Health health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(new DamageInfo(damage, hit.point, direction, knockback, source));
            }

            if (impactEffect != null)
            {
                // 효과는 탄이 날아온 쪽(벽 바깥)을 향하게 둔다.
                Instantiate(impactEffect, hit.point, Quaternion.FromToRotation(Vector3.right, hit.normal));
            }
            Destroy(gameObject);
        }
    }
}
