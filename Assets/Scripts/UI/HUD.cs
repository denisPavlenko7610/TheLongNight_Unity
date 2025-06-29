using DG.Tweening;
using TheLongNight.Items;
using TheLongNight.Player;
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
        [SerializeField] private PlayerInteraction _playerInteraction;
        [SerializeField] private TextMeshProUGUI _itemNameText;
        
        private float _fadeDuration = 1f;
        private float _currentAlpha;
        private CurrentState _currentState;
        
        private void OnEnable()
        {
            _playerInteraction.OnHoverEnter += OnHoverEnter;
            _playerInteraction.OnHoverExit += OnHoverExit;
        }

        private void OnDisable()
        {
            _playerInteraction.OnHoverEnter -= OnHoverEnter;
            _playerInteraction.OnHoverExit -= OnHoverExit;
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
    }
}