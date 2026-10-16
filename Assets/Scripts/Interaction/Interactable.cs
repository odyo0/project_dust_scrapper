using System.Collections.Generic;
using UnityEngine;

namespace DustScrapper.Interaction
{
    /// <summary>
    /// F를 누르고 있으면 무언가가 일어나는 것들의 공통 틀(바닥 물건 줍기, 상자 뒤지기, 나중에 탈출구·문 등).
    /// 누르고 있는 시간을 재고 대상을 고르는 일은 PlayerInteractor가 하고, 여기서는 "무엇을 하는지"만 정한다.
    /// 켜져 있는 것들을 목록으로 모아 두어서, 충돌체나 전용 레이어 없이도 가까운 것을 찾을 수 있다.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        static readonly List<Interactable> active = new();

        [Tooltip("F를 누르고 있어야 하는 시간 (초)")]
        [SerializeField, Min(0f)] float holdTime = 0.3f;

        /// <summary>지금 켜져 있는 모든 상호작용 대상.</summary>
        public static IReadOnlyList<Interactable> Active => active;

        /// <summary>F를 누르고 있어야 하는 시간. 처음 뒤질 때만 오래 걸리는 상자처럼 상황에 따라 바뀔 수 있다.</summary>
        public virtual float HoldTime => holdTime;
        public Vector2 Position => transform.position;

        /// <summary>지금 상호작용 대상이 될 수 있는가. 이미 켠 신호기처럼 다 쓴 것은 false로 둬서 고르지 않게 한다.</summary>
        public virtual bool IsAvailable => true;

        /// <summary>화면에 띄울 동작 이름 (예: 줍기).</summary>
        public abstract string ActionName { get; }

        /// <summary>화면에 띄울 대상 이름 (예: 권총탄 ×24).</summary>
        public abstract string TargetName { get; }

        protected virtual void OnEnable() => active.Add(this);
        protected virtual void OnDisable() => active.Remove(this);

        /// <summary>지금 할 수 없으면 그 이유(예: 가방이 가득 찼다), 할 수 있으면 null.</summary>
        public abstract string BlockedReason(PlayerInteractor interactor);

        /// <summary>다 누르고 있었을 때 한 번 불린다.</summary>
        public abstract void Interact(PlayerInteractor interactor);
    }
}
