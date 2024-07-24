using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class TempMenuController : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            ResKit.Init();
            UIKit.OpenPanel<UITempStartPanel>();
            AudioKit.PlayMusic("bgm_Faster");
        }
    }

}
