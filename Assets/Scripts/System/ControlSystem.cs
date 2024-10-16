using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class ControlSystem : AbstractSystem
    {
        public bool IsMobile = false;
        public BindableProperty<float> Sensitivity = new BindableProperty<float>(1);

        private SaveSystem _saveSystem;

        protected override void OnInit()
        {
            // revert when not testing mobile
            IsMobile = Application.isMobilePlatform;

            // configure sensitivity
            _saveSystem = this.GetSystem<SaveSystem>();
            Sensitivity.Value = _saveSystem.LoadFloat("Sensitivity", 1f);
        }

        public void ToggleCursor(bool isCursorShown)
        {
            if (IsMobile) { return; }
            if (isCursorShown)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            } else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void ChangeSensitivity(float value)
        {
            Sensitivity.Value = value;
            _saveSystem.SaveFloat("Sensitivity", Sensitivity.Value);
        }
    }

}
