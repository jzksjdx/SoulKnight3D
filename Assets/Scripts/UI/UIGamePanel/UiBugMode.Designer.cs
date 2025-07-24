/****************************************************************************
 * 2025.7 Zach’s MacBook Pro
 ****************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public partial class UiBugMode
	{
		[SerializeField] public UnityEngine.UI.Image ChipIcon;
		[SerializeField] public RectTransform ChipTexts;
		[SerializeField] public UnityEngine.UI.Text Title;
		[SerializeField] public UnityEngine.UI.Text Description;

		public void Clear()
		{
			ChipIcon = null;
			ChipTexts = null;
			Title = null;
			Description = null;
		}

		public override string ComponentName
		{
			get { return "UiBugMode";}
		}
	}
}
