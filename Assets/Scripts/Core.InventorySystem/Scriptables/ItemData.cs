﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.InventorySystem
{
    [Serializable, CreateAssetMenu(fileName = "new Item", menuName = "Item DataBase/new Item", order = 2)]
    public class ItemData : ScriptableObject
    {
        public ItemID ID = 0;
        public string Name = "";
        [TextArea]
        public string Description = "";
        public Texture2D Icon = null;

        public GameObject[] inGamePrefabs = new GameObject[1];

        public bool isCombinable = false;
        public bool isDropeable = false;
        public bool isTrowable = false;
        public bool isConsumable = false;

        public GameObject GetRandomInGamePrefab()
        {
            if (inGamePrefabs.Length == 1)
                return inGamePrefabs[0];

            if (inGamePrefabs.Length > 1)
            {
                int resultIndex = UnityEngine.Random.Range(0, inGamePrefabs.Length);
                return inGamePrefabs[resultIndex];
            }

            return null;
        }
    }
}
