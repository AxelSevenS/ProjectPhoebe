using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

using SevenGame.UI;

namespace SeleneGame.Core.UI {

    public sealed class WeaponCostumeCase : CustomButton {
        
        private WeaponCostume _weaponCostume;

        [SerializeField] private Image weaponCostumePortrait;
        [SerializeField] private TextMeshProUGUI weaponCostumeName;



        [SerializeField] private Sprite nullPortrait;

        public WeaponCostume weaponCostume => _weaponCostume;

        public Sprite portraitSprite {
            get => weaponCostumePortrait.sprite;
            set => weaponCostumePortrait.sprite = value;
        }

        public string nameText {
            get => weaponCostumeName.text;
            set => weaponCostumeName.text = value;
        }


        public void SetDisplayWeaponCostume(WeaponCostume weaponCostume) {
            _weaponCostume = weaponCostume;
            portraitSprite = weaponCostume?.portrait ?? nullPortrait;
            nameText = weaponCostume?.displayName.GetLocalizedString();
        }



        public override void OnSubmit(BaseEventData eventData) {
            if (!interactable) return;
            
            base.OnSubmit(eventData);
            Debug.Log($"Weapon case {nameText} clicked");
            WeaponCostumeSelectionMenuController.current.onWeaponCostumeSelected(_weaponCostume);
        }
    }
    
}
