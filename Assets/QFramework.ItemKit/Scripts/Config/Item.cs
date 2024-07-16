#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QFramework
{
    [CreateAssetMenu(menuName = "@ItemKit/Create item")]
    public class Item : ScriptableObject, IItem
    {
        
        [DisplayLabel("名称")] public string Name = string.Empty;

        [DisplayLabel("描述")] 
        [TextArea(minLines:1,maxLines:3)]
        public string Description = string.Empty;

        [DisplayLabel("关键字")] public string Key = string.Empty;
        public Sprite Icon;

        [DisplayLabel("可堆叠")] public bool Stackable = true;

        [DisplayIf(nameof(Stackable), false, true)] [DisplayLabel("    有最大堆叠数量")]
        public bool HasMaxStackableCount = false;

        [DisplayIf(new[] { nameof(Stackable), nameof(HasMaxStackableCount) }, new[] { false, false }, true)]
        [DisplayLabel("        最大堆叠数量")]
        public int MaxStackableCount;
        
        [HideInInspector]
        public List<ItemAttribute> Attributes = new List<ItemAttribute>();

        public string GetName => ItemKit.CurrentLanguage == ItemKit.DefaultLanguage ? Name : LocaleItem.Name;

        public string GetDescription => ItemKit.CurrentLanguage == ItemKit.DefaultLanguage ? Description : LocaleItem.Description;
        public string GetKey => Key;
        public ItemLanguagePackage.LocaleItem LocaleItem { get; set; }
        
        public Sprite GetIcon => Icon;

        public bool GetBoolean(string attributeName)
        {
            var attribute = Attributes.FirstOrDefault(attribute => attribute.Name == attributeName);
            
            if (bool.TryParse(attribute.Value, out var result))
            {
                return result;
            }

            return false;
        }

        public int GetInt(string attributeName)
        {
            var attribute = Attributes.FirstOrDefault(attribute => attribute.Name == attributeName);
            
            if (int.TryParse(attribute.Value, out var result))
            {
                return result;
            }

            return 0;
        }

        public float GetFloat(string attributeName)
        {
            var attribute = Attributes.FirstOrDefault(attribute => attribute.Name == attributeName);
            
            if (float.TryParse(attribute.Value, out var result))
            {
                return result;
            }

            return 0;
        }

        public string GetString(string attributeName)
        {
            var attribute = Attributes.FirstOrDefault(attribute => attribute.Name == attributeName);
            return attribute.Value;
        }

        public bool IsStackable => Stackable;
        public bool GetHasMaxStackableCount => HasMaxStackableCount;
        public int GetMaxStackableCount => MaxStackableCount;
    }
    



#if UNITY_EDITOR
    [CustomEditor(typeof(Item))]
    public class ItemEditor : Editor
    {
        private SerializedProperty mIcon;
        private SerializedProperty mKey;
        private SerializedProperty mAttributes;

        private void OnEnable()
        {
            mIcon = serializedObject.FindProperty("Icon");
            mKey = serializedObject.FindProperty("Key");
            mAttributes = serializedObject.FindProperty("Attributes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("图标");
            mIcon.objectReferenceValue = EditorGUILayout.ObjectField(mIcon.objectReferenceValue,
                typeof(Sprite), true, GUILayout.Height(48),
                GUILayout.Width(48));

            GUILayout.EndHorizontal();
            
            serializedObject.DrawProperties(false, 0, "Icon");

         

            if (mKey.stringValue != target.name)
            {
                target.name = mKey.stringValue;
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.PropertyField(mAttributes, new GUIContent("属性:"));

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
#endif
    
    [Serializable]
    public class ItemAttribute
    {
        public string Name;
        [HideInInspector]
        public ItemAttributeTypes Type;
        public string Value;
    }
    
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ItemAttribute))]
    public class ItemAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var name = property.FindPropertyRelative("Name");
            var type = property.FindPropertyRelative("Type");
            var value = property.FindPropertyRelative("Value");

            var nameRect = new Rect(position.x, position.y, 80, EditorGUIUtility.singleLineHeight);
            // var typeRect = new Rect(position.x + 85, position.y, 80, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(position.x + 85, position.y, 200, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(nameRect, name.stringValue);
            // EditorGUI.EnumPopup(typeRect, (ItemAttributeTypes)type.intValue);

            if (type.intValue == (int)ItemAttributeTypes.Boolean)
            {
                if (bool.TryParse(value.stringValue, out var boolValue))
                {
                    value.stringValue = EditorGUI.Toggle(valueRect, boolValue).ToString();
                }
                else
                {
                    value.stringValue = false.ToString();
                }
                
            }
            else if (type.intValue == (int)ItemAttributeTypes.Int)
            {
                if (int.TryParse(value.stringValue, out var intValue))
                {
                    value.stringValue = EditorGUI.IntField(valueRect, intValue).ToString();
                }
                else
                {
                    value.stringValue = 0.ToString();
                }
            }
            else if (type.intValue == (int)ItemAttributeTypes.Float)
            {
                if (float.TryParse(value.stringValue, out var floatValue))
                {
                    value.stringValue = EditorGUI.FloatField(valueRect, floatValue).ToString();
                }
                else
                {
                    value.stringValue = 0.ToString();
                }
            }
            else if (type.intValue == (int)ItemAttributeTypes.String)
            {
                value.stringValue = EditorGUI.TextField(valueRect, value.stringValue);
            }
        }
    }
#endif
}