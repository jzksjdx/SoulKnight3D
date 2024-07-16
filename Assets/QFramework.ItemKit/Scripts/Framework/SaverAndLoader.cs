using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QFramework
{
    public interface IItemKitSaverAndLoader
    {
        void Save(Dictionary<string, SlotGroup> slotGroups);

        void Load(Dictionary<string, SlotGroup> slotGroups);

        void Clear();
    }

    public class DefaultItemKitSaverAndLoader : IItemKitSaverAndLoader
    {
        [Serializable]
        public class SaveData
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
            var keyString = string.Join("@@", slotGroups.Keys.ToList());
            PlayerPrefs.SetString("slot_group_keys", keyString);
            
            foreach (var slotGroup in slotGroups.Values)
            {
                var slotGroupSaveData = new SaveData()
                {
                    Key = slotGroup.Key,
                    Slots = slotGroup.Slots.Select(slot => new SlotData()
                    {
                        ItemKey = slot.Item != null ? slot.Item.GetKey : null,
                        Count = slot.Count
                    }).ToList()
                };

                var json = JsonUtility.ToJson(slotGroupSaveData);
                PlayerPrefs.SetString("slot_group_"+ slotGroup.Key, json);
            }
        }
        
        public void Load(Dictionary<string, SlotGroup> slotGroups)
        {
            var keyString = PlayerPrefs.GetString("slot_group_keys", string.Empty);
            var keys = keyString.Split("@@").ToList();
            
            foreach (var key in keys)
            {
                SlotGroup slotGroup = null;
                
                if (slotGroups.ContainsKey(key))
                {
                    slotGroup = slotGroups[key];
                }
                else
                {
                    slotGroup = ItemKit.CreateSlotGroup(key);
                }
                
                var json = PlayerPrefs.GetString("slot_group_" + slotGroup.Key, string.Empty);
                if (json.IsNullOrEmpty())
                {
                    
                }
                else
                {
                    var saveData = JsonUtility.FromJson<SaveData>(json);

                    for (int i = 0; i < saveData.Slots.Count; i++)
                    {
                        var slotSaveData = saveData.Slots[i];
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
            var keyString = PlayerPrefs.GetString("slot_group_keys", string.Empty);
            var keys = keyString.Split("@@").ToList();
            foreach (var key in keys)
            {
                PlayerPrefs.DeleteKey("slot_group_" + key);
            }

            PlayerPrefs.DeleteKey("slot_group_keys");
        }
    }
}