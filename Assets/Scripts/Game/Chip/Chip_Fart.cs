using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
    public class Chip_Fart : Chip
    {
        [Header("Fart Config")]
        [SerializeField] private float _fartForce = 5f;
        [SerializeField] private MMF_Player _fartFeedbacks;
        [SerializeField] private MMF_Player _poisonFartFeedbacks;
        [SerializeField] private GameObject _poisonZonePrefab;

        protected override void Start()
        {
            base.Start();
            PlayerController player = PlayerController.Instance;

            player.PlayerAttack.OnPlayerAttaked.Register(() =>
            {
                // generate fart
                Rigidbody rb = player.SelfRigidbody;

                float verticalFactor = player.transform.position.y > 2 ? 0f : 0.7f;
                Vector3 direction = (player.transform.forward + Vector3.up * verticalFactor).normalized;
                rb.AddForce(direction * _fartForce, ForceMode.Impulse);

                if (_isUpgraded)
                {
                    _poisonFartFeedbacks?.PlayFeedbacks();
                    GameObjectsManager.Instance.SpawnStatusZone(_poisonZonePrefab, transform.position);
                }
                else
                {
                    _fartFeedbacks?.PlayFeedbacks();
                }
            }).UnRegisterWhenGameObjectDestroyed(this);

            transform.parent = player.transform;
            transform.localPosition = Vector3.up * 0.25f;
        }

        public override void ApplyUpgradeEffect()
        {
            base.ApplyUpgradeEffect();


        }
    }

}
