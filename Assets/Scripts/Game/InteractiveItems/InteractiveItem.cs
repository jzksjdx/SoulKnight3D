using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class InteractiveItem : MonoBehaviour
    {
        public Collider InteractCollider;
        public InteractLabel Label;

        public virtual void Interact() { }

        
    }

}
