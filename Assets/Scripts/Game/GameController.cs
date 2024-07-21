using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
	public partial class GameController : ViewController, IController
	{
        public static GameController Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        void Start()
		{
            this.GetSystem<AudioSystem>().MusicVolume.RegisterWithInitValue((value) =>
            {
				MMSoundManager.SetVolumeMusic(value);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.GetSystem<AudioSystem>().SoundVolume.RegisterWithInitValue((value) =>
            {
                MMSoundManager.SetVolumeSfx(value);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            if (this.GetSystem<ControlSystem>().IsMobile)
            {
                UIKit.OpenPanel<UIMobileControlPanel>();
            }
            UIKit.OpenPanel<UIGamePanel>();

            PlayerController.Instance.PlayerAttack.SwitchWeapon(PlayerController.Instance.PlayerAttack.CurrentWeaponIndex);
            AudioKit.PlayMusic("bgm_1Low");
        }

        //public void SpawnDamageText(int value, Vector3 position)
        //{
        //    FloatingTextPos.position = position;
        //    MMF_FloatingText floatingText = DamageNumber?.GetFeedbackOfType<MMF_FloatingText>();
        //    floatingText.Value = value.ToString();
        //    DamageNumber?.PlayFeedbacks();
        //}

        //public void SpawnCritText(int value, Vector3 position)
        //{
        //    FloatingTextPos.position = position;
        //    MMF_FloatingText floatingText = CritNumber?.GetFeedbackOfType<MMF_FloatingText>();
        //    floatingText.Value = value.ToString();
        //    CritNumber?.PlayFeedbacks();
        //    CritText?.PlayFeedbacks();
        //}
    }
}
