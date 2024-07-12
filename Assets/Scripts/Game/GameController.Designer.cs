// Generate Id:26b2b856-6648-4c3d-8c1a-8798cad4e807
using UnityEngine;

namespace SoulKnight3D
{
	public partial class GameController : QFramework.IController
	{

		public MoreMountains.Tools.MMSoundManager MMSoundManager;

		public MoreMountains.Feedbacks.MMF_Player DamageNumber;

		public UnityEngine.Transform FloatingTextPos;

		public MoreMountains.Feedbacks.MMF_Player CritNumber;

		public MoreMountains.Feedbacks.MMF_Player CritText;

		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>SoulKnight3D.Global.Interface;
	}
}
