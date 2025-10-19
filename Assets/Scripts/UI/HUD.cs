using System;
using DG.Tweening;
using TheLongNight.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheLongNight.UI
{
    enum CurrentState
    {
        Visible,
        Hidden,
    }
    
    public class HUD : MonoBehaviour
    {
        [SerializeField] private Image _crosshairImage;
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private ObjectViewPanel _objectViewPanel;
        
        private PlayerInteraction _playerInteraction;
        private float _fadeDuration = 1f;
        private float _currentAlpha;
        private CurrentState _currentState;

		public void Init(Player player)
		{
			_playerInteraction = player.PlayerInteraction;
			Subscribe();
		}

		private void OnHoverEnter(PickableItem item)
        {
            _itemNameText.text = item.GetData().name;
            
            if (_currentState == CurrentState.Visible)
                return;
            
            UpdateItemUI(true);
        }

		private void OnHoverExit(PickableItem obj)
        {
            if (_currentState == CurrentState.Hidden)
                return;
            
            UpdateItemUI(false);
        }

		private void UpdateItemUI(bool isHovering)
        {
            _currentState = isHovering ? CurrentState.Visible : CurrentState.Hidden;
            _crosshairImage.DOFade(isHovering ? 1f : 0f, _fadeDuration);
            _itemNameText.DOFade(isHovering ? 1f : 0f, _fadeDuration);
        }

		private void OnCanceled()
        {
            _objectViewPanel.changeVisibility(false);
        }

		private void OnClick(PickableItem item)
        {
            //_objectViewPanel.changeVisibility(true);
            //var data = item.GetData();
        }

		private void OnDestroy()
		{
			Unsubscribe();
		}

		private void Subscribe()
		{
			_playerInteraction.OnHoverEnter += OnHoverEnter;
			_playerInteraction.OnHoverExit += OnHoverExit;
			_playerInteraction.OnClick += OnClick;
			_playerInteraction.OnCanceled += OnCanceled;
		}

		private void Unsubscribe()
		{
			if (_playerInteraction == null) 
				return;	
			
			_playerInteraction.OnHoverEnter -= OnHoverEnter;
			_playerInteraction.OnHoverExit -= OnHoverExit;
			_playerInteraction.OnClick -= OnClick;
			_playerInteraction.OnCanceled -= OnCanceled;
		}
	}
}