using QFramework;

namespace SoulKnight3D
{
    public sealed class ArmorMount : MountBase
    {
        public override bool ReplacesRider => true;

        protected override void OnRideStarted()
        {
            AudioKit.PlaySound("get_in_mecha");
        }
    }
}
