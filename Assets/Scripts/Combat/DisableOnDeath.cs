using System;
using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 죽으면 모습을 감추고 아무것도 하지 않게 한다: 지정한 스크립트(조작, 무기, AI 등)와 충돌체·그림을 끈다.
    /// 그래서 쓰러진 쪽은 움직이지도, 맞지도, 보이지도 않는다. Revive로 끈 것을 되돌린다.
    /// 되살릴지(Respawner), 판을 끝낼지(한 판 관리)는 이것을 쓰는 쪽이 정한다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class DisableOnDeath : MonoBehaviour
    {
        [Tooltip("죽어 있는 동안 끌 스크립트 (조작, 무기, AI 등)")]
        [SerializeField] Behaviour[] disableWhileDead;

        readonly List<Behaviour> disabledBehaviours = new();
        readonly List<Collider2D> disabledColliders = new();
        readonly List<Renderer> hiddenRenderers = new();

        Health health;
        Rigidbody2D body;

        /// <summary>죽어서 다 끈 직후. 되살리기나 판 끝내기는 이것을 듣는다.</summary>
        public event Action ShutDown;

        void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
        }

        void OnEnable() => health.Died += OnDied;
        void OnDisable() => health.Died -= OnDied;

        void OnDied(DamageInfo info)
        {
            foreach (var behaviour in disableWhileDead)
            {
                if (behaviour == null || !behaviour.enabled) continue;
                behaviour.enabled = false;
                disabledBehaviours.Add(behaviour);
            }
            foreach (var collider in GetComponentsInChildren<Collider2D>())
            {
                if (!collider.enabled) continue;
                collider.enabled = false;
                disabledColliders.Add(collider);
            }
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                renderer.enabled = false;
                hiddenRenderers.Add(renderer);
            }
            if (body != null) body.linearVelocity = Vector2.zero;

            ShutDown?.Invoke();
        }

        /// <summary>죽을 때 끈 것을 모두 되돌리고 체력을 채운다.</summary>
        public void Revive()
        {
            foreach (var renderer in hiddenRenderers) if (renderer != null) renderer.enabled = true;
            foreach (var collider in disabledColliders) if (collider != null) collider.enabled = true;
            foreach (var behaviour in disabledBehaviours) if (behaviour != null) behaviour.enabled = true;
            hiddenRenderers.Clear();
            disabledColliders.Clear();
            disabledBehaviours.Clear();

            health.RestoreFull();
        }
    }
}
