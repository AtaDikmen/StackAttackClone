using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Data;

namespace UI
{
    public class PerkCardUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image           iconImage;
        [SerializeField] private Button          selectButton;

        private PerkDefinition         _perkData;
        private Action<PerkDefinition> _onSelectCallback;

        private void Start()
        {
            if(selectButton != null)
            {
                selectButton.onClick.AddListener(OnCardClicked);
            }
        }

        public void Setup(PerkDefinition perk, Action<PerkDefinition> onSelectCallback)
        {
            _perkData         = perk;
            _onSelectCallback = onSelectCallback;

            if(titleText != null)
                titleText.text = perk != null ? perk.perkName : "Upgrade";

            if(descriptionText != null)
                descriptionText.text = perk != null ? perk.description : "Boost your stats!";

            if(iconImage != null)
            {
                if(perk != null && perk.icon != null)
                {
                    iconImage.sprite = perk.icon;
                    iconImage.gameObject.SetActive(true);
                }
                else
                    iconImage.gameObject.SetActive(false);
            }
        }

        private void OnCardClicked()
        {
            _onSelectCallback?.Invoke(_perkData);
        }

        private void OnDestroy()
        {
            if(selectButton != null)
                selectButton.onClick.RemoveAllListeners();
        }
    }
}
