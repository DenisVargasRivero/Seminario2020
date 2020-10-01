﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using IA.PathFinding;
using Core.Interaction;
using Core.InventorySystem;

[Serializable]
public struct MouseContext
{
    public bool interactuableHitted;
    public IInteractable InteractionHandler;
    public bool validHit;
    public Vector3 hitPosition;
    public Node closerNode;
}

[RequireComponent(typeof(PathFindSolver))]
public class MouseContextTracker : MonoBehaviour
{
    Camera _viewCamera;
    PathFindSolver _solver;
    [SerializeField] LayerMask mouseDetectionMask = ~0;
    //float checkRate = 0.1f;
    [SerializeField] IInteractable lastFinded = null;

    [Header("Cursor Rendering")]
    public Texture2D defaultCursor;
    public Texture2D InteractiveCursor;
    public Texture2D AimCursor;

#if UNITY_EDITOR
    [SerializeField] List<Collider> hited = new List<Collider>();
#endif

    private void Awake()
    {
        _solver = GetComponent<PathFindSolver>();
        _viewCamera = Camera.main;

        if (defaultCursor != null)
        {
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
            Cursor.visible = true;
        }
        
    }
 
    public void ChangeCursorView(int index)
    {
        switch (index)
        {
            case 1:
                Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
                break;
            case 2:
                Cursor.SetCursor(InteractiveCursor, Vector2.zero, CursorMode.Auto);
                break;
            case 3:
                Cursor.SetCursor(AimCursor, new Vector2(AimCursor.width/2, AimCursor.height/2), CursorMode.Auto);
                break;
        }
    }

    
    public MouseContext GetCurrentMouseContext()
    {
        return m_MouseContextDetection();
    }

    MouseContext m_MouseContextDetection()
    {
        MouseContext _context = new MouseContext();
        int validHits = 0;

        //Calculo la posición del mouse en el espacio.
        RaycastHit[] hits;
        Ray mousePositionInWorld = _viewCamera.ScreenPointToRay(new Vector3(Input.mousePosition.x,
                                                          Input.mousePosition.y,
                                                          _viewCamera.transform.position.y));
        hits = Physics.RaycastAll(mousePositionInWorld, float.MaxValue, mouseDetectionMask, QueryTriggerInteraction.Collide);

        #region DEBUG
#if UNITY_EDITOR
        hited = new List<Collider>();
        for (int i = 0; i < hits.Length; i++) // Lista debug para el inspector.
        {
            hited.Add(hits[i].collider);
        }
#endif 
        #endregion

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];

            IInteractable interactableObject = hit.transform.GetComponentInParent<IInteractable>();
            if (interactableObject != null && interactableObject.InteractionsAmmount > 0)
            {
                _context.interactuableHitted = true;
                _context.InteractionHandler = interactableObject;
                validHits++;
            }
            ThrowMouseEvents(interactableObject);

            Collider collider = hit.collider;
            if (collider.transform.CompareTag("NavigationFloor"))
            {
                _context.hitPosition = hit.point;
                _context.closerNode = _solver.getCloserNode(hit.point);
                validHits++;
            }
            else continue;
        }

        _context.validHit = validHits > 0; //Validación del hit.

        return _context;
    }


    public void ThrowMouseEvents(IInteractable target)
    {
        if (target == null)
        {
            if (lastFinded != null)
            {
                lastFinded.OnInteractionMouseExit();
                lastFinded = null;
            }
        }
        else
        {
            if (lastFinded != null && lastFinded != target)
            {
                lastFinded.OnInteractionMouseExit();
                lastFinded = target;
                lastFinded.OnInteractionMouseOver();
            }
            else
            {
                lastFinded = target;
                lastFinded.OnInteractionMouseOver();
            }
        }
    }
}
