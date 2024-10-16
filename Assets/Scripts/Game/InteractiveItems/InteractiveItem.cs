using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class InteractiveItem : MonoBehaviour, IController
    {
        public Collider InteractCollider;
        public InteractLabel Label;

        protected LanguageSystem _languageSystem;

        public virtual void Interact() { }

        protected virtual void Start()
        {
            _languageSystem = this.GetSystem<LanguageSystem>();
        }

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }

}
