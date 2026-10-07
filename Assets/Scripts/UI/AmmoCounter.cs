using DustScrapper.Weapons;
using TMPro;
using UnityEngine;

namespace DustScrapper.UI
{
    /// <summary>
    /// 손에 든 총의 탄창 표시 ("탄창 안 / 탄창 크기")와 재장전 진행 막대.
    /// 근접 무기를 들면 통째로 숨긴다. 탄이 바닥나면 숫자를 경고색으로 바꾼다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmmoCounter : MonoBehaviour
    {
        [SerializeField] WeaponLoadout loadout;
        [Tooltip("총을 들지 않았을 때 숨길 묶음. 이 스크립트가 붙은 물체 자신이면 안 된다(꺼지면 다시 켤 수 없다).")]
        [SerializeField] GameObject root;
        [SerializeField] TMP_Text label;
        [Tooltip("재장전 진행 막대 (anchorMax.x로 길이 조절)")]
        [SerializeField] RectTransform reloadFill;

        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color emptyColor = new(1f, 0.35f, 0.3f);
        [SerializeField] Color reloadingColor = new(1f, 1f, 1f, 0.4f);

        int shownAmmo = -1;
        int shownSize = -1;

        void Awake()
        {
            if (root == gameObject) Debug.LogError($"{name}: root에 자기 자신을 넣으면 한 번 숨긴 뒤 다시 보이지 않는다. 자식 묶음을 넣는다.", this);
        }

        void Update()
        {
            Gun gun = loadout != null ? loadout.Current as Gun : null;
            if (root != null && root.activeSelf != (gun != null)) root.SetActive(gun != null);
            if (gun == null || gun.GunData == null) return;

            int ammo = gun.AmmoInMagazine;
            int size = gun.GunData.MagazineSize;
            if (ammo != shownAmmo || size != shownSize)
            {
                // 숫자가 바뀔 때만 글자를 새로 만든다(매 프레임 문자열을 만들지 않게).
                label.SetText("{0} / {1}", ammo, size);
                shownAmmo = ammo;
                shownSize = size;
            }

            label.color = gun.IsReloading ? reloadingColor : ammo == 0 ? emptyColor : normalColor;

            if (reloadFill != null)
            {
                reloadFill.gameObject.SetActive(gun.IsReloading);
                Vector2 max = reloadFill.anchorMax;
                max.x = gun.ReloadProgress;
                reloadFill.anchorMax = max;
            }
        }
    }
}
