using UnityEngine;

namespace QFramework
{

    public interface IItem
    {
       string GetKey { get; }
       string GetName { get; }
       string GetDescription { get; }
       
       ItemLanguagePackage.LocaleItem LocaleItem { get; set; }

       Sprite GetIcon { get; }

       bool GetBoolean(string attributeName);
       int GetInt(string attributeName);
       float GetFloat(string attributeName);
       string GetString(string attributeName);
       
       bool IsStackable { get; }
       bool GetHasMaxStackableCount { get; }
       int GetMaxStackableCount { get; }
    }
    
}