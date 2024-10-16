/****************************************************************************
 * 2024.7 Zach’s MacBook Pro
 ****************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
	public partial class CraftingSystem : UIElement
    {
        public List<RecipeSO> Recipes;
        public List<CraftingSlot> CraftingSlots;

        private Dictionary<WeaponData, int> _availableIngredients = new Dictionary<WeaponData, int>();
        public List<ItemPlant> _cachedItem = new List<ItemPlant> { null, null, null, null };

        private void Awake()
		{
            InitCraftingSlots();

            foreach(CraftingSlot craftingSlot in CraftingSlots)
            {
                craftingSlot.Data.Changed.Register(() =>
                {
                    ItemPlant newItem = craftingSlot.Data.Item;
                    if (newItem != _cachedItem[craftingSlot.Index])
                    {
                        if (_cachedItem[craftingSlot.Index] != null)
                        { // remove from _availableIngredients
                            RemoveFromDictionary(_cachedItem[craftingSlot.Index].GetIngredientRequirements());
                        }
                        _cachedItem[craftingSlot.Index] = craftingSlot.Data.Item;
                        if (newItem != null)
                        { // add new one _availableIngredients
                            Debug.Log("Add new item ingredient");
                            AddToDictionary(newItem.GetIngredientRequirements());
                        }

                        ShowCraftResult();
                    } 
                }).UnRegisterWhenGameObjectDestroyed(this);
            }

            CraftResultSlot.OnItemCrafted.Register(() =>
            {
                _cachedItem = new List<ItemPlant> { null, null, null, null };
                _availableIngredients = new Dictionary<WeaponData, int>();
                foreach (CraftingSlot craftingSlot in CraftingSlots)
                {
                    if (craftingSlot.Data.Item)
                    {
                        craftingSlot.Data.Item.DestroyGameObjGracefully();
                        craftingSlot.Data.Item = null;
                        craftingSlot.Data.Count = 0;
                        craftingSlot.Data.Changed.Trigger();
                    }
                }
            }).UnRegisterWhenGameObjectDestroyed(this);
        }

        public void InitCraftingSlots()
        {
            ItemKit.CreateSlotGroup("Crafting").CreateSlotsByCount(CraftingSlots.Count);
            for (int i = 0; i < CraftingSlots.Count; i++)
            {
                CraftingSlots[i].InitWithData(ItemKit.GetSlotGroupByKey("Crafting").Slots[i]);
            }

            ItemKit.CreateSlotGroup("CraftResult").CreateSlotsByCount(1);
            CraftResultSlot.InitWithData(ItemKit.GetSlotGroupByKey("CraftResult").Slots[0]);
        }

        public void RemoveFromDictionary(List<IngredientRequirement> requiredIngredients)
        {
            foreach(IngredientRequirement requiredIngredient in requiredIngredients)
            {
                if(!_availableIngredients.ContainsKey(requiredIngredient.Ingredient))
                {
                    continue;
                }
                if(_availableIngredients[requiredIngredient.Ingredient] <= requiredIngredient.Quantity)
                {
                    _availableIngredients.Remove(requiredIngredient.Ingredient);
                    continue;
                }
                _availableIngredients[requiredIngredient.Ingredient] -= requiredIngredient.Quantity;

            }
        }

        public void AddToDictionary(List<IngredientRequirement> requiredIngredients)
        {
            foreach (IngredientRequirement requiredIngredient in requiredIngredients)
            {
                if (_availableIngredients.ContainsKey(requiredIngredient.Ingredient))
                {
                    _availableIngredients[requiredIngredient.Ingredient] += requiredIngredient.Quantity;
                    continue;
                }
                _availableIngredients[requiredIngredient.Ingredient] = requiredIngredient.Quantity;
            }
        }

        public void ShowCraftResult()
        {
            bool matchRecipe = false;
            foreach(var recipe in Recipes)
            {
                if(CanCraft(recipe))
                {
                    Debug.Log("Match!");
                    CraftResultSlot.ResultPrefab = recipe.Result;
                    CraftResultSlot.Data.Item = recipe.Result.GetComponent<ItemPlant>();
                    CraftResultSlot.Data.Item.State = ItemPlant.StorageState.Recipe;
                    CraftResultSlot.Data.Count = 1;
                    CraftResultSlot.Data.Changed.Trigger();

                    matchRecipe = true;
                    break;
                }
            }

            if (!matchRecipe)
            {
                Debug.Log("No match!");
                CraftResultSlot.ResultPrefab = null;
                CraftResultSlot.Data.Item = null;
                CraftResultSlot.Data.Count = 0;
                CraftResultSlot.Data.Changed.Trigger();
            }
        }

        private bool CanCraft(RecipeSO recipe)
        {
            foreach (var requirement in recipe.RequiredIngredients)
            {
                bool containsKey = false;
                WeaponData matchKey = null;
                foreach(var availableIngredient in _availableIngredients)
                {
                    if (availableIngredient.Key.Name == requirement.Ingredient.name)
                    {
                        containsKey = true;
                        matchKey = availableIngredient.Key;
                        break;
                    }
                }
                if (!containsKey)
                {
                    return false;
                }
                if (_availableIngredients[matchKey] < requirement.Quantity)
                {
                    return false;
                }
                //if (!_availableIngredients.ContainsKey(requirement.Ingredient) ||
                //    _availableIngredients[requirement.Ingredient] < requirement.Quantity)
                //{
                //    return false;
                //}
            }
            
            Debug.Log("Can craft!");
            return true;
        }

        protected override void OnBeforeDestroy()
		{
		}
	}
}