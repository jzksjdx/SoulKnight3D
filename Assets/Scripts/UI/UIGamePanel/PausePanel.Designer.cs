/****************************************************************************
 * 2024.7 Zach’s MacBook Pro
 ****************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public partial class PausePanel
	{
		[SerializeField] public UnityEngine.UI.Button BtnHome;
		[SerializeField] public UnityEngine.UI.Button BtnRestart;
		[SerializeField] public UnityEngine.UI.Button BtnResume;
		[SerializeField] public UnityEngine.UI.Slider SliderMusic;
		[SerializeField] public UnityEngine.UI.Slider SliderSound;

		public void Clear()
		{
			BtnHome = null;
			BtnRestart = null;
			BtnResume = null;
			SliderMusic = null;
			SliderSound = null;
		}

		public override string ComponentName
		{
			get { return "PausePanel";}
		}
	}
}
