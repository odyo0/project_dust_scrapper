using UnityEngine;
using UnityEngine.Events;

namespace DustScrapper.Combat
{
    /// <summary>
    /// 쓰러지면(DisableOnDeath) 잠시 뒤 처음 자리에서 되살린다.
    /// 시험장(Game.unity)의 플레이어와 적만 쓴다. 한 판 맵에서는 붙이지 않는다(죽으면 판이 끝나고, 적은 되살아나지 않는다).
    /// </summary>
    [RequireComponent(typeof(DisableOnDeath))]
    [DisallowMultipleComponent]
    public sealed class Respawner : MonoBehaviour
    {
        [SerializeField, Min(0f)] float respawnDelay = 2f;

        [Tooltip("되살아난 직후 부를 것 (카메라 바로 옮기기 등)")]
        [SerializeField] UnityEvent respawned;

        DisableOnDeath downed;
        Rigidbody2D body;
        Vector3 spawnPosition;
        float respawnTimer = -1f;

        void Awake()
        {
            downed = GetComponent<DisableOnDeath>();
            body = GetComponent<Rigidbody2D>();
            spawnPosition = transform.position;
        }

        void OnEnable() => downed.ShutDown += OnShutDown;
        void OnDisable() => downed.ShutDown -= OnShutDown;

        void Update()
        {
            if (respawnTimer < 0f) return;
            respawnTimer -= Time.deltaTime;
            if (respawnTimer < 0f) Respawn();
        }

        void OnShutDown() => respawnTimer = respawnDelay;

        void Respawn()
        {
            transform.position = spawnPosition;
            if (body != null)
            {
                body.position = spawnPosition;
                body.linearVelocity = Vector2.zero;
            }

            downed.Revive();
            respawned?.Invoke();
        }
    }
}
