using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SoulKnight3D
{
    [DisallowMultipleComponent]
    public sealed class UIPressStateRelay : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler
    {
        public readonly EasyEvent<bool> OnPressedChanged =
            new EasyEvent<bool>();

        private bool _isPressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                SetPressed(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                SetPressed(false);
            }
        }

        private void OnDisable()
        {
            SetPressed(false);
        }

        private void SetPressed(bool isPressed)
        {
            if (_isPressed == isPressed) { return; }

            _isPressed = isPressed;
            OnPressedChanged.Trigger(isPressed);
        }
    }
}
