using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Global : Architecture<Global>
    {
        [RuntimeInitializeOnLoadMethod]
        public static void AutoInit()
        {
            ResKit.Init();
            Application.targetFrameRate = -1;
        }

        protected override void Init()
        {
            RegisterSystem(new SaveSystem());
            RegisterSystem(new AudioSystem());
            RegisterSystem(new ControlSystem());
        }
    }

}
