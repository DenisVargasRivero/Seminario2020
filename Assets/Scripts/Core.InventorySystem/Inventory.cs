﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.InventorySystem;


namespace Core.InventorySystem
{
    public class Inventory
    {
        public int maxItemsSlots = 3;
        public List<Item> slots = new List<Item>(); //Esto es por si en un futuro queremos llevar varias cosas.
        public Item equiped = null;

        //Utility functions.
        //Notas:
        //-->Añadir un objeto a los slots, ocupa un espacio.
        //-->Equipar un objeto desocupa un espacio.

        public Inventory()
        {
            maxItemsSlots = 3;
            slots = new List<Item>();
            equiped = null;
        }

        public void EquipItem(Item item)
        {
            //Si hay un objeto ya equipado reemplazo por el item intercambio los 2 items.
            if (item != null)
                equiped = item; //Reasigno el objeto.
        }

        public Item UnEquipItem()
        {
            var releasedItem = equiped;
            equiped = null;
            return releasedItem;
        }
    }
}

