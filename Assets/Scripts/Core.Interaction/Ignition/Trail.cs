﻿using UnityEngine;
using IA.PathFinding;
using Core.Interaction;

public class Trail : MonoBehaviour
{
    [Header("Igniteable Object Main Settings")]
    [SerializeField] GameObject _ignitionPoint_Prefab;
    [SerializeField] GameObject _slimePatch_Prefab;

    public bool Emit { get; set; } = false;

    //En vez de utilizar Update vamos a utilizar un evento que chequee si el nodo mas cercano actual
    //Tiene o no un componente igniteable, sino, le añado uno.
    /// <summary>
    /// Este callback se llama al actualizarse el valor del nodo más cercano. Chequea si dicho nodo contiene un componente igniteable.
    /// Si no existe uno, le añade un sub-Objeto igniteable.
    /// </summary>
    /// <param name="current">El nuevo nodo actual.</param>
    public void OnCloserNodeChanged(Node current)
    {
        if (current == null) return;

        if (Emit)
        {
            if (current.handler == null || !current.handler.HasStaticInteractionOfType(OperationType.Ignite))
            {
                var ignition = Instantiate(_ignitionPoint_Prefab);
                ignition.transform.SetParent(current.gameObject.transform); //Añadimos el prefab como un subobjeto.
                ignition.transform.localPosition = Vector3.zero;

                var ignitionInteractionHandler = ignition.GetComponent<IInteractable>();
                current.handler = ignitionInteractionHandler;
                //handler.markAsDirty(); //Función que utilizamos para decirle a un InteractionHandler que tiene que serializarse (Sistema de guardado).

                //Seteo los parches.
                foreach (var connection in current.Connections)
                {
                    if (connection.handler != null && connection.handler.HasStaticInteractionOfType(OperationType.Ignite))
                    {
                        Vector3 dir = (connection.transform.position - current.transform.position).normalized;
                        Vector3 center = Vector3.Lerp(current.transform.position, connection.transform.position, 0.5f);
                        var patch = Instantiate(_slimePatch_Prefab,center, Quaternion.identity);
                        patch.transform.forward = dir;
                    }
                }
            }
        }
    }
}