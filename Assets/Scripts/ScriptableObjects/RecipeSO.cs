using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "RecipeSO", menuName = "ScriptableObject/RecipeSO")]
    public class RecipeSO : ScriptableObject
    {
        public List<IngredientRequirement> RequiredIngredients;
        public GameObject Result;
    }

    [Serializable]
    public class IngredientRequirement
    {
        public WeaponData Ingredient;
        public int Quantity;

        public IngredientRequirement(WeaponData ingredient, int quantity)
        {
            Ingredient = ingredient;
            Quantity = quantity;
        }
    }

}
