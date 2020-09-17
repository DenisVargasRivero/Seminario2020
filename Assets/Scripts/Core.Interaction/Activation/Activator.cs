﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Core.Interaction;
using Core.InventorySystem;
using System;

[RequireComponent(typeof(InteractionHandler))]
public class Activator : MonoBehaviour, IInteractionComponent
{
    [SerializeField] UnityEvent OnActivate = new UnityEvent();

    public Transform ActivationPosition;
    public Vector3 LookToDirection { get => ActivationPosition.forward; }
    public bool isDynamic => false;

    public Vector3 requestSafeInteractionPosition(Vector3 requesterPosition)
    {
        return ActivationPosition.position;
    }

    public List<Tuple<OperationType, IInteractionComponent>> GetAllOperations(Inventory inventory)
    {
        return new List<Tuple<OperationType, IInteractionComponent>>()
        {
            new Tuple<OperationType, IInteractionComponent>(OperationType.Activate, this)
        };
    }
    public void InputConfirmed(OperationType operation, params object[] optionalParams)
    {
        throw new NotImplementedException();
    }
    public void ExecuteOperation(OperationType operation, params object[] optionalParams)
    {
        OnActivate.Invoke();
    }
    public void CancelOperation(OperationType operation, params object[] optionalParams)
    {
        
    }
}
