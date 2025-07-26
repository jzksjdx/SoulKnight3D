using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Chip : MonoBehaviour
    {
        [SerializeField] private int _maxProgress = 5;

        private int _progress = 0;
        protected bool _isUpgraded = false;
        private float _showProgressTime = 3f;

        [Header("Chip Asset")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private string _title;
        [SerializeField] private string _description;
        [SerializeField] private string _upgradedDesc;

        GameController gameController;

        protected virtual void Start()
        {
            ActionKit.DelayFrame(1, () =>
            {
                UIKit.GetPanel<UIGamePanel>().ToggleBugMode();
                UpdateBugModeUi();
            }).Start(this);

            gameController = GameController.Instance;
            GameController.Instance.OnRoomClear.Register(() =>
            {
                AddProgress();
            }).UnRegisterWhenGameObjectDestroyed(GameController.Instance);

            GameController.Instance.OnReturnMenu.Register(() =>
            {
                Destroy(gameObject);
            }).UnRegisterWhenGameObjectDestroyed(GameController.Instance);

            DontDestroyOnLoad(gameObject);
        }

        private void FixedUpdate()
        {
            if (gameController == null)
            {
                gameController = FindObjectOfType<GameController>();
                if (gameController)
                {
                    gameController.OnRoomClear.Register(() =>
                    {
                        AddProgress();
                    }).UnRegisterWhenGameObjectDestroyed(gameController);

                    ActionKit.DelayFrame(1, () =>
                    {
                        UIKit.GetPanel<UIGamePanel>().ToggleBugMode();
                        UpdateBugModeUi();
                    }).Start(this);
                }
            }
        }

        public virtual void AddProgress()
        {
            if (_isUpgraded) { return; }
            _progress++;
            if (_progress >= _maxProgress)
            {
                _isUpgraded = true;
                ApplyUpgradeEffect();
            }

            UpdateBugModeUi();
            UiBugMode bugModeUi = UIKit.GetPanel<UIGamePanel>().UiBugMode;
            // briefly show description texts
            bugModeUi.ToggleTexts(true);
            ActionKit.Delay(_showProgressTime, () =>
            {
                if (UIKit.GetPanel<UIGamePanel>().PausePanel.gameObject.activeSelf == false)
                {
                    bugModeUi.ToggleTexts(false);
                }
            }).Start(this);
        }

        public virtual void ApplyUpgradeEffect()
        {
           
        }

        private void UpdateBugModeUi()
        {
            string description = _isUpgraded ? _upgradedDesc : _description;
            UIKit.GetPanel<UIGamePanel>().UiBugMode.UpdateUi(_icon, _title, description);
            UIKit.GetPanel<UIGamePanel>().UiBugMode.UpdateProgress(_maxProgress, _progress);
        }
    }

}
