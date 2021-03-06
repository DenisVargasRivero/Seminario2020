﻿using UnityEngine;

[ExecuteInEditMode]
public class SightTest : MonoBehaviour
{
    [SerializeField] LayerMask visibles = ~0;
    [SerializeField] float range        = 1f;
    [SerializeField] float angle        = 45f;
    [SerializeField] Transform Target   = null;

#if UNITY_EDITOR
    [Space, Header("Debug")]
    [SerializeField] bool Debug_LineOfSight = false;
    [SerializeField] Color rangeColor = Color.white;
    [SerializeField] Color angleColor = Color.white;
#endif

    /// <summary>
    /// El vector resultante de la resta de ambas posiciones: B - A.
    /// </summary>
    [Tooltip("El vector resultante de la resta de ambas posiciones: B - A."), HideInInspector]
    public Vector3 positionDiference = Vector3.zero;
    /// <summary>
    /// Dirección normalizada hacia el objetivo.
    /// </summary>
    [Tooltip("Dirección normalizada hacia el objetivo."), HideInInspector]
    public Vector3 dirToTarget = Vector3.zero;
    /// <summary>
    /// Último ángulo calculado entre la posición de origen y el objetivo.
    /// </summary>
    [Tooltip("Último ángulo calculado entre la posición de origen y el objetivo."), HideInInspector]
    public float angleToTarget = 0;
    /// <summary>
    /// Última distancia calculada entre la posición de origen y el objetivo.
    /// </summary>
    [Tooltip("Última distancia calculada entre la posición de origen y el objetivo."), HideInInspector]
    public float distanceToTarget = 0;

    private void Update()
    {
        IsInSight(Target);
    }

    public bool IsInSight(Transform target)
    {
        if (target == null)
        {
            Debug.Log("El target es inválido");
            return false;
        }

        positionDiference = (target.position - transform.position);
        distanceToTarget = positionDiference.magnitude;
        Vector3 BidimensionalProjection = positionDiference.YComponent(0);
        angleToTarget = Vector3.Angle(transform.forward, BidimensionalProjection);
        dirToTarget = positionDiference.normalized;

        if (distanceToTarget > range || angleToTarget > angle) return false;

        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position, dirToTarget, out hitInfo, range + 1, visibles))
            return hitInfo.transform == target;

        return false;
    }

    //Snippet for Debugg
#if (UNITY_EDITOR)
    void OnDrawGizmosSelected()
    {
        if (Debug_LineOfSight)
        {
            //Posición del objetivo.
            var currentPosition = transform.position;
            if (Target != null)
            {
                Gizmos.color = IsInSight(Target) ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, transform.position + dirToTarget * distanceToTarget);
            }

            //Rango
            Gizmos.color = rangeColor;
            Gizmos.matrix *= Matrix4x4.Scale(new Vector3(1, 0, 1));
            Gizmos.DrawWireSphere(transform.position, range);

            //Ángulo
            Gizmos.color = angleColor;
            Gizmos.DrawLine(currentPosition, currentPosition + Quaternion.Euler(0, angle + 1, 0) * transform.forward * range);
            Gizmos.DrawLine(currentPosition, currentPosition + Quaternion.Euler(0, -angle - 1, 0) * transform.forward * range);
        }

    }
#endif
}
