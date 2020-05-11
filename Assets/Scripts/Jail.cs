﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.DamageSystem;

[RequireComponent(typeof(Collider))]
public class Jail : MonoBehaviour
{
    [SerializeField] Collider PhysicalCollider = null;
    [SerializeField] Collider _damageDealer;
    Rigidbody _rb;
    public bool IsKinematicObj;
    public bool Growndchecked =false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = IsKinematicObj;
    }
    private void Update()
    {
     
    }

    public void Drop()
    {
        _rb.isKinematic = IsKinematicObj;
        StartCoroutine(Deactivate());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.gameObject.layer == 0)
        {
            Growndchecked = true;
        }
        var myhitedObject = collision.collider.GetComponent<IDestructible>();

        if (myhitedObject != null)
        {
            myhitedObject.destroyMe();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var killable = other.GetComponentInParent<IDamageable<Damage>>();
        if (killable != null && !Growndchecked)
        {
            killable.Hit(new Damage() { instaKill = true });
        }
    }

    IEnumerator Deactivate()
    {
        yield return new WaitForSeconds(10f);
        PhysicalCollider.enabled = false;
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
}
