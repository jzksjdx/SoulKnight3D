using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
	public partial class StartMenuManager : ViewController
	{
		void Start()
		{
			ResKit.Init();
            UIKit.OpenPanel<UIStartMenuPanel>();
			AudioKit.PlayMusic("bgm_room");
		}
	}
}
