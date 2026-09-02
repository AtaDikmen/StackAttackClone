using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Core;
using Data;
using Systems;

namespace UI
{
    public class PerkSelectionPanel : MonoBehaviour
    {
        [SerializeField] private PerkCardUI[] perkCards;
        [SerializeField] private Transform    cardsContainer;

        private          GameManager      _gameManager;
        private          PerkSystem       _perkSystem;
        private readonly List<PerkCardUI> _instantiatedCards = new List<PerkCardUI>();

        [Inject]
        public void Construct(GameManager gameManager, PerkSystem perkSystem)
        {
            _gameManager = gameManager;
            _perkSystem  = perkSystem;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            var options = _perkSystem?.GetRandomPerks(3) ?? new List<PerkDefinition>();

            if(perkCards != null && perkCards.Length >= options.Count)
            {
                for(int i = 0; i < perkCards.Length; i++)
                {
                    if(i < options.Count)
                    {
                        perkCards[i].gameObject.SetActive(true);
                        perkCards[i].Setup(options[i], OnPerkSelected);
                    }
                    else
                    {
                        perkCards[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ClearInstantiatedCards();
        }

        private void OnPerkSelected(PerkDefinition perk)
        {
            _perkSystem?.ApplyPerk(perk);
            Hide();
            _gameManager?.ResumeFromPerkSelection();
        }

        private void ClearInstantiatedCards()
        {
            foreach(var card in _instantiatedCards)
            {
                if(card != null) Destroy(card.gameObject);
            }
            _instantiatedCards.Clear();
        }
    }
}
