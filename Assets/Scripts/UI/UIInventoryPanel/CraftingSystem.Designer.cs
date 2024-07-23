/****************************************************************************
 * 2024.7 Zach’s MacBook Pro
 ****************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public partial class CraftingSystem
	{
		[SerializeField] public SoulKnight3D.CraftResultSlot CraftResultSlot;

		public void Clear()
		{
			CraftResultSlot = null;
		}

		public override string ComponentName
		{
			get { return "CraftingSystem";}
		}
	}
}
