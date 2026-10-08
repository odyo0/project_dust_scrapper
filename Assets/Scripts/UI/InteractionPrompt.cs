using DustScrapper.Interaction;
using DustScrapper.Player;
using TMPro;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 손이 닿는 대상 위에 "무엇을 / E 무엇하기"를 띄우고, E를 누르고 있는 진행도를 막대로 보여 준다.
    /// 지금 할 수 없으면(가방이 가득 참 등) 키 안내 대신 그 이유를 경고색으로 띄운다.
    /// 가방 화면처럼 마우스를 쓰는 창이 열려 있으면(E가 막혀 있으므로) 띄우지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] PlayerInputReader input;
        [Tooltip("대상이 없을 때 숨길 묶음. 이 스크립트가 붙은 물체 자신이면 안 된다(꺼지면 다시 켤 수 없다).")]
        [SerializeField] RectTransform root;
        [SerializeField] TMP_Text targetLabel;
        [SerializeField] TMP_Text actionLabel;
        [Tooltip("누르고 있는 진행 막대 (anchorMax.x로 길이 조절)")]
        [SerializeField] RectTransform progressFill;
        [Tooltip("비워 두면 Main Camera를 쓴다.")]
        [SerializeField] Camera viewCamera;

        [Tooltip("대상 위치에서 위로 띄울 거리 (칸)")]
        [SerializeField] float heightAbove = 0.6f;
        [SerializeField] Color actionColor = Color.white;
        [SerializeField] Color blockedColor = new(1f, 0.35f, 0.3f);

        Canvas canvas;
        Interactable shownTarget;
        string shownName;
        string shownReason;

        void Awake()
        {
            if (root == transform) Debug.LogError($"{name}: root에 자기 자신을 넣으면 한 번 숨긴 뒤 다시 보이지 않는다. 자식 묶음을 넣는다.", this);
            if (viewCamera == null) viewCamera = Camera.main;
            canvas = GetComponentInParent<Canvas>();
        }

        void LateUpdate()
        {
            bool menuOpen = input != null && input.MenuOpen;
            Interactable target = interactor != null && interactor.isActiveAndEnabled && !menuOpen ? interactor.Target : null;
            if (root.gameObject.activeSelf != (target != null)) root.gameObject.SetActive(target != null);
            if (target == null)
            {
                shownTarget = null;
                return;
            }

            PlaceOver(target.Position + Vector2.up * heightAbove);

            string reason = interactor.BlockedReason;
            string targetName = target.TargetName;
            if (target != shownTarget || targetName != shownName || reason != shownReason)
            {
                // 글자는 내용이 바뀔 때만 새로 만든다.
                targetLabel.text = targetName;
                actionLabel.text = reason ?? $"E  {target.ActionName}";
                actionLabel.color = reason != null ? blockedColor : actionColor;
                shownTarget = target;
                shownName = targetName;
                shownReason = reason;
            }

            if (progressFill != null)
            {
                float progress = interactor.Progress;
                progressFill.gameObject.SetActive(progress > 0f);
                Vector2 max = progressFill.anchorMax;
                max.x = progress;
                progressFill.anchorMax = max;
            }
        }

        void PlaceOver(Vector2 worldPoint)
        {
            // 캔버스가 화면 오버레이든 카메라 방식이든 같은 자리에 오도록 부모 기준 좌표로 바꾼다.
            Vector2 screenPoint = viewCamera.WorldToScreenPoint(worldPoint);
            var parent = (RectTransform)root.parent;
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, uiCamera, out Vector2 local))
                root.localPosition = local;
        }
    }
}
