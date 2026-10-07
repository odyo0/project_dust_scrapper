using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 죽으면 모습을 감추고 잠시 뒤 처음 자리에서 체력을 채워 되살린다.
    /// 죽어 있는 동안에는 조작·무기 같은 스크립트와 충돌체를 꺼서 아무것도 하지 않고 맞지도 않게 한다.
    /// 시험용 방의 플레이어·표적·포탑이 쓴다. 페이즈 4에서 "죽으면 잃음"이 생기면 플레이어 쪽은 다른 처리로 바뀐다.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class Respawner : MonoBehaviour
    {
        [SerializeField, Min(0f)] float respawnDelay = 2f;

        [Tooltip("죽어 있는 동안 끌 스크립트 (조작, 무기, AI 등)")]
        [SerializeField] Behaviour[] disableWhileDead;

        [Tooltip("되살아난 직후 부를 것 (카메라 바로 옮기기 등)")]
        [SerializeField] UnityEvent respawned;

        readonly List<Behaviour> disabledBehaviours = new();
        readonly List<Collider2D> disabledColliders = new();
        readonly List<Renderer> hiddenRenderers = new();

        Health health;
        Rigidbody2D body;
        Vector3 spawnPosition;
        float respawnTimer = -1f;

        void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            spawnPosition = transform.position;
        }

        void OnEnable() => health.Died += OnDied;
        void OnDisable() => health.Died -= OnDied;

        void Update()
        {
            if (respawnTimer < 0f) return;
            respawnTimer -= Time.deltaTime;
            if (respawnTimer < 0f) Respawn();
        }

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

            respawnTimer = respawnDelay;
        }

        void Respawn()
        {
            transform.position = spawnPosition;
            if (body != null)
            {
                body.position = spawnPosition;
                body.linearVelocity = Vector2.zero;
            }

            foreach (var renderer in hiddenRenderers) if (renderer != null) renderer.enabled = true;
            foreach (var collider in disabledColliders) if (collider != null) collider.enabled = true;
            foreach (var behaviour in disabledBehaviours) if (behaviour != null) behaviour.enabled = true;
            hiddenRenderers.Clear();
            disabledColliders.Clear();
            disabledBehaviours.Clear();

            health.RestoreFull();
            respawned?.Invoke();
        }
    }
}
