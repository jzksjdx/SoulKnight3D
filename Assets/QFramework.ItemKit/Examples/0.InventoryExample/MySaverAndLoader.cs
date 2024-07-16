using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QFramework.Example
{
  public class MySaverAndLoader : IItemKitSaverAndLoader
        {
            [Serializable]
            public class SaveData
            {
                public List<GroupData> Groups;
            }
            
            [Serializable]
            public class GroupData
            {
                public string Key;
                public List<SlotData> Slots;
            }
            
            [Serializable]

            public class SlotData
            {
                public string ItemKey;
                public int Count;
            }
            
            public void Save(Dictionary<string, SlotGroup> slotGroups)
            {
                PlayerPrefs.SetString("my_item_kit", JsonUtility.ToJson(new SaveData()
                {
                    Groups = slotGroups.Values.Select(group => new GroupData()
                    {
                        Key = group.Key,
                        Slots = group.Slots.Select(slot => new SlotData()
                        {
                            ItemKey = slot.Item != null ? slot.Item.GetKey : null,
                            Count = slot.Count
                        }).ToList()
                    }).ToList()
                }));
            }

            public void Load(Dictionary<string, SlotGroup> slotGroups)
            {
                var json = PlayerPrefs.GetString("my_item_kit", string.Empty);
                if (json.IsNotNullAndEmpty())
                {
                    var saveData = JsonUtility.FromJson<SaveData>(json);
                    
                    foreach (var group in saveData.Groups)
                    {
                        SlotGroup slotGroup = null;
                        
                        if (slotGroups.ContainsKey(group.Key))
                        {
                            slotGroup = slotGroups[group.Key];
                        }
                        else
                        {
                            slotGroup = ItemKit.CreateSlotGroup(group.Key);
                        }

                        for (int i = 0; i < group.Slots.Count; i++)
                        {
                            var slotSaveData = group.Slots[i];
                            var item = slotSaveData.ItemKey.IsNullOrEmpty()
                                ? null
                                : ItemKit.ItemByKey[slotSaveData.ItemKey];

                            if (i < slotGroup.Slots.Count)
                            {
                                slotGroup.Slots[i].Item = item;
                                slotGroup.Slots[i].Count = slotSaveData.Count;
                                slotGroup.Slots[i].Changed.Trigger();
                            }
                            else
                            {
                                slotGroup.CreateSlot(item, slotSaveData.Count);
                            }
                        }   
                    }
                }
            }

            public void Clear()
            {
                PlayerPrefs.DeleteKey("my_item_kit");
            }
        }
}