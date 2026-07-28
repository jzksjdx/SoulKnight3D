using UnityEngine;

namespace SoulKnight3D
{
    [DisallowMultipleComponent]
    public sealed class MountInteraction : InteractiveItem
    {
        [SerializeField] private MountBase _mount;
        [SerializeField] private InteractLabel _labelPrefab;
        [SerializeField] private string _displayName = "Blue Mech";
        [SerializeField] private Vector3 _labelLocalPosition =
            new Vector3(0f, 2.1f, 0f);

        private void Awake()
        {
            if (_mount == null) { _mount = GetComponent<MountBase>(); }
            CreateLabel();
        }

        protected override void Start()
        {
            base.Start();
            RefreshAvailability();
        }

        private void Update()
        {
            RefreshAvailability();
        }

        public override void Interact()
        {
            PlayerController player = PlayerController.Instance;
            if (player == null || _mount == null)
            {
                RefreshAvailability();
                return;
            }

            if (player.MountRider.TryMount(_mount))
            {
                SetInteractable(false);
            }
            else
            {
                RefreshAvailability();
            }
        }

        public void RefreshAvailability()
        {
            bool battleActive = GameController.Instance != null &&
                GameController.Instance.IsRoomBattleActive;
            bool canMount = _mount != null && !_mount.IsMounted &&
                !_mount.IsDead && !battleActive;

            if (IsInteractable != canMount)
            {
                SetInteractable(canMount);
            }
        }

        private void CreateLabel()
        {
            if (Label != null || _labelPrefab == null)
            {
                return;
            }

            Label = Instantiate(_labelPrefab, transform);
            Label.name = "MountInteractLabel";
            Label.transform.localPosition = _labelLocalPosition;
            Label.transform.localRotation = Quaternion.identity;
            Label.SetLabelText(_displayName, WeaponData.WeaponRarity.White);
            Label.gameObject.SetActive(false);
        }
    }
}
