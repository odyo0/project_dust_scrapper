using System;
using System.Collections.Generic;
using DustScrapper.Combat;
using DustScrapper.Interaction;
using DustScrapper.Progress;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DustScrapper.Raid
{
    /// <summary>
    /// 한 판의 시작과 끝을 맡는다. 씬이 열리면 판이 시작되고, 탈출 지점이 Extracted를 알리거나
    /// 플레이어가 쓰러지면(DisableOnDeath) 판이 끝난다. 끝나면 들고 있던 물건으로 결과(RaidResult)를 만들어
    /// Ended로 알리고 게임 시간을 멈춘다. 결과를 본 뒤 기지로 돌아간다(가져온 것을 저장에 넘기는 일은 RaidProfileLink가 한다).
    /// 플레이어는 Player 태그로, 탈출 지점은 씬에서 찾아서, 맵마다 연결할 것이 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaidSession : MonoBehaviour
    {
        [Tooltip("쓰러진 뒤 결과를 띄우기까지 기다리는 시간 (초). 쓰러지는 순간을 볼 수 있게.")]
        [SerializeField, Min(0f)] float deathResultDelay = 1.5f;

        [Tooltip("판이 끝나고 돌아갈 기지 씬 이름 (빌드 설정에 들어 있어야 한다)")]
        [SerializeField] string baseScene = "Base";

        ExtractionPoint[] extractionPoints;
        DisableOnDeath playerDowned;
        PlayerLoadout carried;
        PlayerInteractor interactor;
        float startTime;
        float pendingDeathTimer = -1f;

        public IReadOnlyList<ExtractionPoint> ExtractionPoints => extractionPoints;
        public bool IsOver => Result != null;
        public RaidResult Result { get; private set; }

        /// <summary>판이 끝났을 때(탈출 또는 사망).</summary>
        public event Action<RaidResult> Ended;

        void Awake()
        {
            extractionPoints = FindObjectsByType<ExtractionPoint>(FindObjectsSortMode.InstanceID);

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError($"{name}: Player 태그가 붙은 플레이어가 없어 판을 관리할 수 없다.", this);
                enabled = false;
                return;
            }
            playerDowned = player.GetComponent<DisableOnDeath>();
            carried = PlayerLoadout.Of(player);
            interactor = player.GetComponent<PlayerInteractor>();
        }

        void OnEnable()
        {
            Time.timeScale = 1f;
            startTime = Time.time;
            foreach (var point in extractionPoints) point.Extracted += OnExtracted;
            if (playerDowned != null) playerDowned.ShutDown += OnPlayerDown;
        }

        void OnDisable()
        {
            foreach (var point in extractionPoints) point.Extracted -= OnExtracted;
            if (playerDowned != null) playerDowned.ShutDown -= OnPlayerDown;
        }

        void Update()
        {
            if (pendingDeathTimer < 0f) return;
            pendingDeathTimer -= Time.deltaTime;
            if (pendingDeathTimer < 0f) End(false);
        }

        /// <summary>기지로 돌아간다.</summary>
        public void ReturnToBase()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(baseScene);
        }

        void OnExtracted()
        {
            if (!IsOver && pendingDeathTimer < 0f) End(true);
        }

        void OnPlayerDown()
        {
            if (!IsOver) pendingDeathTimer = deathResultDelay;
        }

        void End(bool extracted)
        {
            pendingDeathTimer = -1f;
            Result = new RaidResult(extracted, Time.time - startTime, carried.CollectItems());

            // 결과를 보는 동안 판은 멈추고, 플레이어는 더 줍거나 가방을 열 수 없다(가방 화면도 닫힌다).
            Time.timeScale = 0f;
            if (interactor != null) interactor.enabled = false;
            Ended?.Invoke(Result);
        }
    }
}
