/****************************************************************************
 * 2025.7 Zach’s MacBook Pro
 ****************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public partial class PausePanel
	{
		[SerializeField] public UnityEngine.UI.Button BtnHome;
		[SerializeField] public UnityEngine.UI.Button BtnResume;
		[SerializeField] public UnityEngine.UI.Button BtnSettings;

		public void Clear()
		{
			BtnHome = null;
			BtnResume = null;
			BtnSettings = null;
		}

		public override string ComponentName
		{
			get { return "PausePanel";}
		}
	}
}
