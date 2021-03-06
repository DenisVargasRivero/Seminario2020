﻿using UnityEngine;

namespace Core.DamageSystem
{
    public interface IDamageable<Input, output>
    {
        bool IsAlive { get;}

        //Componentes
        GameObject gameObject { get; }
        Transform transform { get; }
        T GetComponent<T>();

        output GetHit(Input damage);
        void FeedDamageResult(output result);
        Input GetDamageStats();

        //Stuns!
        void GetStun(Vector3 AgressorPosition, int PosibleKillingMethod);
    }
}
