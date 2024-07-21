using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using Crystal;

namespace SoulKnight3D
{
    public class UiResizer : MonoBehaviour
    {
        private int _screenWidth;
        private int _screenHeight;

        private void Start()
        {
            //_screenWidth = Screen.width;
            //_screenHeight = Screen.height;
             _screenWidth = 1920;
            _screenHeight = 1080;
            UIKit.Root.SetResolution(_screenWidth, _screenHeight, 1);

            // apply safe area
            UIRoot.Instance.Common.GetOrAddComponent<SafeArea>();
        }

        private void Update()
        {
            //if (Screen.width == _screenWidth && Screen.height == _screenHeight) { return; }
            //_screenWidth = Screen.width;
            //_screenHeight = Screen.height;
            //UIKit.Root.SetResolution(_screenWidth, _screenHeight, 1);
        }
    }

}
