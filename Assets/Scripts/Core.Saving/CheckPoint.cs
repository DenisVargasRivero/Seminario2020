﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.InventorySystem;

namespace Core.SaveSystem
{
    [Serializable]
    public struct PlayerData
    {
        //Posición del jugador.
        public Vector3 position;
        //Rotación del jugador
        public Quaternion rotacion;
        //Inventario / Equipamiento. Guardado como una lista de ItemID.
        public int maxItemsSlots;
        public ItemID EquipedItem;
        public bool itemIsActive;
        public Vector3 itemScale;
        public Quaternion itemRotation;
        public List<ItemID> inventory;
    }

    [Serializable]
    public enum EnemyType : int
    {
        baboso = 1,
        Grunt
    }

    [Serializable]
    public struct EnemyData
    {
        public Vector3 position;
        public Vector3 forward;
        public EnemyType enemyType;
        public int[] WaypointIDs;
    }

    [Serializable]
    public class CheckPoint
    {
        public int levelID;
        public int LevelBuildID;
        public PlayerData playerData;
        public List<EnemyData> Enemies;
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
    }
}
