using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    [DisallowMultipleComponent]
    public sealed class MountRider : MonoBehaviour
    {
        private PlayerController _player;

        public MountBase CurrentMount { get; private set; }
        public bool IsMounted => CurrentMount != null;
        public PlayerController Player => _player;
        public EasyEvent<MountBase> OnMountChanged = new EasyEvent<MountBase>();

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
        }

        private void LateUpdate()
        {
            if (!IsMounted || !CurrentMount.gameObject.activeInHierarchy)
            {
                return;
            }

            _player.SyncToMount(CurrentMount.transform);
        }

        private void OnDestroy()
        {
            if (CurrentMount != null)
            {
                Destroy(CurrentMount.gameObject);
                CurrentMount = null;
            }
        }

        public bool TryMount(MountBase mount)
        {
            if (mount == null || IsMounted || IsBattleActive())
            {
                return false;
            }

            CurrentMount = mount;
            if (!mount.BeginRide(this))
            {
                CurrentMount = null;
                return false;
            }

            OnMountChanged.Trigger(CurrentMount);
            return true;
        }

        public bool TryHandleSkillAction()
        {
            if (!IsMounted)
            {
                return false;
            }

            Dismount();
            return true;
        }

        public void Dismount()
        {
            if (!IsMounted) { return; }

            MountBase mount = CurrentMount;
            CurrentMount = null;
            Vector3 dismountPosition = mount.EndRide(this, false);
            _player.ExitMountControl(dismountPosition);
            OnMountChanged.Trigger(null);
        }

        internal void EnterMountControl(MountBase mount, bool replacesRider)
        {
            if (CurrentMount != mount) { return; }
            _player.EnterMountControl(replacesRider);
        }

        internal void HandleMountDestroyed(MountBase mount)
        {
            if (CurrentMount != mount) { return; }

            CurrentMount = null;
            Vector3 dismountPosition = mount.EndRide(this, true);
            _player.ExitMountControl(dismountPosition);
            OnMountChanged.Trigger(null);
        }

        internal void PrepareForLevelTransition()
        {
            CurrentMount?.PrepareForLevelTransition();
        }

        internal void RestoreAfterLevelTransition(Vector3 spawnPosition)
        {
            if (!IsMounted) { return; }

            CurrentMount.RestoreAfterLevelTransition(spawnPosition);
            _player.SyncToMount(CurrentMount.transform);
        }

        private static bool IsBattleActive()
        {
            return GameController.Instance != null &&
                GameController.Instance.IsRoomBattleActive;
        }
    }
}
