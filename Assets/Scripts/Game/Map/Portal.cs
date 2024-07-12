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

					UIKit.ClosePanel<UIGamePanel>();
                    UIKit.ClosePanel<UIMobileControlPanel>();

                    SceneManager.LoadScene(0);
				}
			});
		}
	}
}
