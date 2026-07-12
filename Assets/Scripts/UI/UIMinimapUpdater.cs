using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
    public class UIMinimapUpdater : MonoBehaviour
    {
        public static UIMinimapUpdater Instance;
        [SerializeField] private Texture MinimapTexture;

        void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void UpdateMap()
        {
            UIKit.GetPanel<UIGamePanel>().MinimapRawImage.texture = MinimapTexture;
        }
    }

}
