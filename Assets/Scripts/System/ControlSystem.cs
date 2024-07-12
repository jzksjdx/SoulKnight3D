using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class ControlSystem : AbstractSystem
    {
        public bool IsMobile = false;

        protected override void OnInit()
        {
            // revert when not testing mobile
            IsMobile = Application.isMobilePlatform;
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
    }

}
