using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirusScript : MonoBehaviour
{
    public float gravity = 0.1f;

    private Rigidbody rb;
    private bool settled = false;

    private CoughEmitter emitter;

    public void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void FixedUpdate()
    {
        if (!settled)
        {
            rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        }
        else
        {
            rb.velocity = Vector3.zero;
        }   
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (collider.transform.GetComponent<VirusScript>() == null)
        {
            if (collider.transform.GetComponent<CleanScript>() != null)
            {
                Destroy(gameObject);
                return;
            }
            transform.parent = collider.transform;
            settled = true;
        }
    }
}
