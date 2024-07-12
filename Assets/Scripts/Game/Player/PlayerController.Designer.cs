// Generate Id:da2410ab-88dd-488f-b26b-884fc89a070f
using UnityEngine;

namespace SoulKnight3D
{
	public partial class PlayerController : QFramework.IController
	{

		public SoulKnight3D.Skill Skill;

		public PlayerStats PlayerStats;

		public UnityEngine.Rigidbody SelfRigidbody;

		public UnityEngine.GameObject CameraTarget;

		public SoulKnight3D.PlayerAnimation PlayerAnimation;

		public SoulKnight3D.PlayerAttack PlayerAttack;

		public UnityEngine.Transform ModelRoot;

		QFramework.IArchitecture QFramework.IBelongToArchitecture.GetArchitecture()=>SoulKnight3D.Global.Interface;
	}
}
