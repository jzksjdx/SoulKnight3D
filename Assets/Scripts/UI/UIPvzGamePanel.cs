using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System.Collections;
using System.Collections.Generic;

namespace SoulKnight3D
{
	public class UIPvzGamePanelData : UIPanelData
	{
	}
	public partial class UIPvzGamePanel : UIPanel
	{
		private List<RectTransform> _progressBarFlags = new List<RectTransform>();
		private float _levelDuration = 0f;
		private float _headRightPosX = 143.6f;
        private float _headLeftPosX = -193.1f;
		private float _headTotalDistance;
		private int _flagProgress = 0;

        protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIPvzGamePanelData ?? new UIPvzGamePanelData();
			// please add init code here
			ProgressBar.Hide();

			PvzGameManager.Instance.OnNewLevelStart.Register((zombieWavesSO) =>
			{
				UpdateProgressBarFlags(zombieWavesSO);
				StartCoroutine(GameStartTip());
            }).UnRegisterWhenGameObjectDestroyed(this);

            _headTotalDistance = _headRightPosX - _headLeftPosX;

            PvzGameManager.Instance.OnProgressUpdate.Register((progress) =>
			{
				ProgressBarImage.fillAmount = progress;
				//Head.anchoredPosition = new Vector2(Mathf.Lerp(_headRightPosX, _headLeftPosX, progress), 0f);
				Head.anchoredPosition = new Vector2(_headRightPosX - _headTotalDistance * progress, 0f);

            }).UnRegisterWhenGameObjectDestroyed(this);

            PvzGameManager.Instance.OnHugeWave.Register((isFinalWave) =>
			{
				_progressBarFlags[_flagProgress].GetComponent<Animator>().enabled = true;
				_flagProgress++;
				if (isFinalWave)
				{
					StartCoroutine(FinalWaveTip());
				} else
				{
					StartCoroutine(HugeWaveTip());
				}
            }).UnRegisterWhenGameObjectDestroyed(this);
        }

        private IEnumerator GameStartTip()
		{
			TipStart.Show();
			AudioKit.PlaySound("readysetplant");
			yield return new WaitForSeconds(1.8f);
			TipStart.Hide();
			ProgressBar.Show();
        }

		private IEnumerator HugeWaveTip() // wait for 3 seconds for zombie spawn
		{
			TipHugeWave.Show();
            yield return new WaitForSeconds(0.6f);
			AudioKit.PlaySound("hugewave");
            yield return new WaitForSeconds(2.4f);
            TipHugeWave.Hide();
            AudioKit.PlaySound("siren");
        }

		private IEnumerator FinalWaveTip()
		{
			yield return StartCoroutine(HugeWaveTip());
			TipFinalWave.Show();
            yield return new WaitForSeconds(0.6f);
            AudioKit.PlaySound("finalwave");
            yield return new WaitForSeconds(1.4f);
            TipFinalWave.Hide();
        }

		public void UpdateProgressBarFlags(ZombieWavesSO zombieWavesSO)
		{
            // calculate level total duration
            _levelDuration = 0f;
            var zombieWaves = zombieWavesSO.ZombieWaves;
            for (int i = 0; i < zombieWaves.Count - 1; i++) // ignore final wave duration
            {
                _levelDuration += zombieWaves[i].duration;
            }

			// instantiate flags
            float currWaveDuration = 0f;
            foreach (ZombieWave zombieWave in zombieWavesSO.ZombieWaves)
			{
				
                if (zombieWave.isHugeWave)
				{
                    FlagTemplate.InstantiateWithParent(FlagRoot)
                    .Self(newFlag =>
                    {
                        Vector3 fullPos = newFlag.anchoredPosition3D;
                        newFlag.anchoredPosition3D = fullPos * (currWaveDuration / _levelDuration);

						_progressBarFlags.Add(newFlag);
                    })
                    .Show();
                }
                currWaveDuration += zombieWave.duration;
            }
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}
	}
}
