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
        public bool IsInteractable { get; private set; } = true;

        protected LanguageSystem _languageSystem;

        public virtual void Interact() { }

        public void SetInteractable(bool isInteractable)
        {
            IsInteractable = isInteractable;
            if (InteractCollider != null)
            {
                InteractCollider.enabled = isInteractable;
            }

            if (!isInteractable && Label != null)
            {
                Label.Hide();
            }
        }

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
