using UnityEngine;
using QFramework;
using UnityEngine.SceneManagement;

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

					int level = GameController.Instance.Level;
					if (level > 2) // win
					{
                        GameController.Instance.ToggleGameFreeze(true);
                        UIEndPanel endPanel = UIKit.OpenPanel<UIEndPanel>();
                        endPanel.UpdateEndTitle(true);
                    } else
					{
                        UIKit.ClosePanel<UIGamePanel>();
                        UIKit.HidePanel<UIMobileControlPanel>();
                        GameController.Instance.EnterNextLevel();
                    }
				}
			});
		}
	}
}
