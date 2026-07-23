using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public class UIBossFightData : UIPanelData
	{
		public BossEncounterDataSO BossEncounter { get; }
		public Action OnIntroFinished { get; }

		public UIBossFightData()
		{
		}

		public UIBossFightData(BossEncounterDataSO bossEncounter, Action onIntroFinished)
		{
			BossEncounter = bossEncounter;
			OnIntroFinished = onIntroFinished;
		}
	}

	public partial class UIBossFight : UIPanel
	{
		private static readonly int IntroState = Animator.StringToHash("UIBossFight");

		[SerializeField] private Image _background;
		[SerializeField] private Image _bossSprite;
		[SerializeField] private Text _bossText;
		[SerializeField] private Animator _animator;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UIBossFightData ?? new UIBossFightData();
			CacheReferences();
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as UIBossFightData ?? mData;
			CacheReferences();
			ApplyBossPresentation();
			StartCoroutine(PlayIntro());
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

		private IEnumerator PlayIntro()
		{
			if (_animator == null || _animator.runtimeAnimatorController == null)
			{
				yield return new WaitForSecondsRealtime(3.05f);
			}
			else
			{
				_animator.Rebind();
				_animator.Update(0f);
				ApplyBossPresentation();
				_animator.Play(IntroState, 0, 0f);
				yield return null;

				AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
				int stateHash = state.fullPathHash;
				float timeout = Mathf.Max(0.1f, state.length * 2f);
				float elapsed = 0f;
				while (_animator != null &&
				       _animator.GetCurrentAnimatorStateInfo(0).fullPathHash == stateHash &&
				       _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f &&
				       elapsed < timeout)
				{
					elapsed += Time.unscaledDeltaTime;
					yield return null;
				}
			}

			Action onIntroFinished = mData?.OnIntroFinished;
			CloseSelf();
			onIntroFinished?.Invoke();
		}

		private void ApplyBossPresentation()
		{
			BossEncounterDataSO boss = mData?.BossEncounter;
			if (boss == null) { return; }

			if (_background != null) { _background.color = boss.IntroBackgroundColor; }
			if (_bossSprite != null) { _bossSprite.sprite = boss.BossSprite; }
			if (_bossText != null) { _bossText.text = boss.DisplayName; }
		}

		private void CacheReferences()
		{
			if (_animator == null) { _animator = GetComponent<Animator>(); }
			if (_background == null)
			{
				Transform background = transform.Find("Bg");
				if (background != null) { _background = background.GetComponent<Image>(); }
			}
			if (_bossSprite == null)
			{
				Transform sprite = transform.Find("BossSprite");
				if (sprite != null) { _bossSprite = sprite.GetComponent<Image>(); }
			}
			if (_bossText == null)
			{
				Transform text = transform.Find("BossText");
				if (text != null) { _bossText = text.GetComponent<Text>(); }
			}
		}
	}
}
