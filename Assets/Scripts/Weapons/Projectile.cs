using System;
using System.Collections.Generic;
using DustScrapper.Combat;
using UnityEngine;

namespace DustScrapper.Weapons
{
    /// <summary>
    /// 날아가는 탄. 매 프레임 이번에 갈 거리만큼 앞을 광선으로 검사해서, 빠른 탄이 얇은 벽을 뚫고 지나가지 않게 한다.
    /// 무언가에 맞으면 체력이 있으면 피해를 주고, 맞은 자리에 효과를 남기고 사라진다.
    /// 쏜 사람 자신에게는 맞지 않는다. 멈추면(맞았거나 사거리 끝) Stopped로 멈춘 자리를 알린다(던진 칼을 바닥에 떨어뜨릴 때 쓴다).
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
        bool stopped;

        /// <summary>날아가는 방향 (길이 1).</summary>
        public Vector2 Direction => direction;

        /// <summary>멈췄을 때(맞았거나 사거리 끝). 값은 멈춘 자리. 이 직후 탄은 사라진다.</summary>
        public event Action<Vector2> Stopped;

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
            // 출발하자마자 멈춘 탄은 같은 프레임에 Update가 한 번 더 돌 수 있어서(사라지는 건 프레임 끝) 막는다.
            if (stopped) return;
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
            if (remainingRange <= 0f) Stop(transform.position);
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

        void Stop(Vector2 at)
        {
            stopped = true;
            Stopped?.Invoke(at);
            Destroy(gameObject);
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
            Stop(hit.point);
        }
    }
}
