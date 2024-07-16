using UnityEngine;
using QFramework;

namespace QFramework.Example
{
	public class Items
	{
		public static IItem item_iron => ItemKit.ItemByKey["item_iron"];
		public static string item_iron_key = "item_iron";
		public static IItem item_greensword => ItemKit.ItemByKey["item_greensword"];
		public static string item_greensword_key = "item_greensword";
		public static IItem item_paper => ItemKit.ItemByKey["item_paper"];
		public static string item_paper_key = "item_paper";
		public static IItem item_wood => ItemKit.ItemByKey["item_wood"];
		public static string item_wood_key = "item_wood";
	}
}
