using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace QFramework
{
    [CreateAssetMenu(menuName = "@ItemKit/Create Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [DisplayLabel("命名空间:")] public string Namespace = "QFramework.Example";

        [HideInInspector] [DisplayLabel("生成路径:")]
        public string CodeGenPath = string.Empty;

        [HideInInspector]
        public List<ItemAttributeDefine> AttributesDefine = new List<ItemAttributeDefine>();

        public List<Item> Items;

        private void OnValidate()
        {
            if (AttributesDefine.Count > 0)
            {
                foreach (var attributeDefine in AttributesDefine)
                {
                    foreach (var item in Items)
                    {
                        var attribute = item.Attributes.FirstOrDefault(attribute => attribute.Name == attributeDefine.Name);

                        if (attribute == null)
                        {
                            attribute = new ItemAttribute()
                            {
                                Name = attributeDefine.Name,
                                Type = attributeDefine.Type,
                            };
                            item.Attributes.Add(attribute);
                        }
                        else
                        {
                            attribute.Type = attributeDefine.Type;
                        }

                        // 去除冗余
                        if (item.Attributes.Count != AttributesDefine.Count)
                        {
                            item.Attributes.RemoveAll(attribute =>
                                AttributesDefine.All(g => g.Name != attribute.Name));
                        }
                    }
                }
            }
            else
            {
                foreach (var item in Items)
                {
                    item.Attributes.Clear();
                }
            }
        }
    }


#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ItemDatabase))]
    public class ItemDatabaseEditor : UnityEditor.Editor
    {
        private SerializedProperty mItems;
        private SerializedProperty mCodeGenPath;
        private SerializedProperty mAttributesDefine;

        public class ItemEditorObj
        {
            public bool Foldout
            {
                get => EditorPrefs.GetBool(Item.GetName + "_foldout", true);
                set => EditorPrefs.SetBool(Item.GetName + "_foldout", value);
            }

            public Editor Editor = null;
            public Item Item = null;
        }

        private List<ItemEditorObj> mItemEditors = new List<ItemEditorObj>();

        private void OnEnable()
        {
            mItems = serializedObject.FindProperty("Items");
            mCodeGenPath = serializedObject.FindProperty("CodeGenPath");
            mAttributesDefine = serializedObject.FindProperty("AttributesDefine");
            RefreshItemEditors();
        }

        void RefreshItemEditors()
        {
            mItemEditors.Clear();

            for (int i = 0; i < mItems.arraySize; i++)
            {
                var item = mItems.GetArrayElementAtIndex(i);
                var editor = CreateEditor(item.objectReferenceValue);
                mItemEditors.Add(new ItemEditorObj()
                {
                    Editor = editor,
                    Item = item.objectReferenceValue as Item,
                });
            }
        }

        string mSearchKey = "";

        FluentGUIStyle mHeader = FluentGUIStyle.Label().FontBold();


        Queue<Action> mActionQueue = new Queue<Action>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.BeginVertical("box");
            serializedObject.DrawProperties(false, 0, "Items");

            if (mItems.arraySize != mItemEditors.Count)
            {
                RefreshItemEditors();
            }

            if (mCodeGenPath.stringValue.IsNotNullAndEmpty())
            {
                mCodeGenPath.DrawProperty();
            }

            if (GUILayout.Button("生成代码"))
            {
                var itemDb = target as ItemDatabase;
                var path = itemDb.CodeGenPath;

                if (path.IsNullOrEmpty())
                {
                    path = EditorUtility.SaveFilePanelInProject("items.cs", "Items", "cs", "");
                    mCodeGenPath.stringValue = path;
                }

                var filePath = path;

                var rootCode = new RootCode()
                    .Using("UnityEngine")
                    .Using("QFramework")
                    .EmptyLine()
                    .Namespace(itemDb.Namespace, ns =>
                    {
                        ns.Class("Items", String.Empty, false, false, c =>
                        {
                            foreach (var itemConfig in itemDb.Items)
                            {
                                c.Custom(
                                    $"public static IItem {itemConfig.Key} => ItemKit.ItemByKey[\"{itemConfig.Key}\"];");
                                c.Custom($"public static string {itemConfig.Key}_key = \"{itemConfig.Key}\";");
                            }
                        });
                    });

                using var fileWriter = File.CreateText(filePath);
                var codeWriter = new FileCodeWriter(fileWriter);
                rootCode.Gen(codeWriter);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            GUILayout.EndVertical();
            
            
            EditorGUILayout.PropertyField(mAttributesDefine,new GUIContent("属性定义:"));

            EditorGUILayout.Separator();
            GUILayout.Label("物品列表:", mHeader);
            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索:", GUILayout.Width(40));
            mSearchKey = EditorGUILayout.TextField(mSearchKey);
            GUILayout.EndHorizontal();

            for (var i = 0; i < mItemEditors.Count; i++)
            {
                var itemEditor = mItemEditors[i];

                if (!itemEditor.Item.Name.Contains(mSearchKey) && !itemEditor.Item.Key.Contains(mSearchKey))
                {
                    continue;
                }

                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                itemEditor.Foldout = EditorGUILayout.Foldout(itemEditor.Foldout, itemEditor.Item.GetName);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("-"))
                {
                    var index = i;
                    if (EditorUtility.DisplayDialog("删除物品", "确定要删除吗?", "删除", "取消"))
                    {
                        mActionQueue.Enqueue(() =>
                        {
                            var arrayElement = mItems.GetArrayElementAtIndex(index);
                            AssetDatabase.RemoveObjectFromAsset(arrayElement.objectReferenceValue);
                            mItems.DeleteArrayElementAtIndex(index);
                            serializedObject.ApplyModifiedPropertiesWithoutUndo();
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        });
                    }
                }

                GUILayout.EndHorizontal();

                if (itemEditor.Foldout)
                {
                    itemEditor.Editor.OnInspectorGUI();
                }

                GUILayout.EndVertical();
            }

            if (GUILayout.Button("创建物品"))
            {
                mActionQueue.Enqueue(() =>
                {
                    var item = CreateInstance<Item>();
                    item.name = nameof(Item);
                    item.Name = "新物品";
                    item.Key = "item_key";
                    AssetDatabase.AddObjectToAsset(item, target);
                    mItems.InsertArrayElementAtIndex(mItems.arraySize);
                    var arrayElement = mItems.GetArrayElementAtIndex(mItems.arraySize - 1);
                    arrayElement.objectReferenceValue = item;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                });
            }


            if (mActionQueue.Count > 0)
            {
                mActionQueue.Dequeue().Invoke();
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
#endif


    [Serializable]
    public class ItemAttributeDefine
    {
        public string Name;
        public ItemAttributeTypes Type;
    }

    public enum ItemAttributeTypes
    {
        Boolean,
        Int,
        Float,
        String,
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ItemAttributeDefine))]
    public class ItemAttributeDefineDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var name = property.FindPropertyRelative("Name");
            var type = property.FindPropertyRelative("Type");

            var nameRect = new Rect(position.x, position.y, 80, EditorGUIUtility.singleLineHeight);
            var typeRect = new Rect(position.x + 85, position.y, 80, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(position.x + 170, position.y, 200, EditorGUIUtility.singleLineHeight);

            name.stringValue = EditorGUI.TextField(nameRect, name.stringValue);
            type.intValue = (int)(ItemAttributeTypes)EditorGUI.EnumPopup(typeRect, (ItemAttributeTypes)type.intValue);
        }
    }
#endif
}