using UnityEngine;
using QFramework;
using System.Collections;

namespace SoulKnight3D
{
	public partial class Portal : ViewController
	{
		void Start()
		{
			SelfCollider.OnTriggerEnterEvent((other) =>
			{
				if (other.tag == "Player")
				{
					// win game;
					AudioKit.PlaySound("fx_transform");
                    AudioKit.StopMusic();
                    if (GameController.Instance.IsFinalLevel) // win
					{
                        GameController.Instance.ToggleGameFreeze(true);
                        StartCoroutine(ShowEndPanel(true));
                    } else
					{
                        UIKit.ClosePanel<UIGamePanel>();
                        UIKit.HidePanel<UIMobileControlPanel>();
                        GameController.Instance.EnterNextLevel();
                    }
				}
			});
		}

        private IEnumerator ShowEndPanel(bool playerWon)
        {
            yield return UIKit.OpenPanelAsync<UIEndPanel>();
            UIKit.GetPanel<UIEndPanel>()?.UpdateEndTitle(playerWon);
        }
	}
}
