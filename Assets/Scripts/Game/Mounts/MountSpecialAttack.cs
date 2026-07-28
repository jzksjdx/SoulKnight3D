using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public abstract class MountSpecialAttack : MonoBehaviour
    {
        public readonly EasyEvent<float> OnChargeChanged =
            new EasyEvent<float>();

        public abstract float ChargeNormalized { get; }
        public abstract bool TryActivate();

        public virtual void HandleRideEnded()
        {
        }
    }
}
