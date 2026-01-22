using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifespan = 5f;
    public Rigidbody rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifespan);
        rb = GetComponent<Rigidbody>();
        StartCoroutine(Predict());
    }

    protected void FixedUpdate()
    {
        StartCoroutine(Predict());
    }
    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log("Bullet hit: " + collider.gameObject.name);
        Destroy(gameObject);
    }

    protected IEnumerator Predict()
    {
        Vector3 prediction = transform.position + rb.linearVelocity * Time.fixedDeltaTime;

        RaycastHit hit2;
        int layerMask = ~LayerMask.GetMask("Bullet");
        if (Physics.Linecast(transform.position, prediction, out hit2, layerMask))
        {
            transform.position = hit2.point;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            //rb.isKinematic = true;
            yield return 0;
            OnTriggerEnterFixed(hit2.collider);
        }
    }
    protected virtual void OnTriggerEnterFixed(Collider other)
    {
        Debug.Log("Bullet hit: " + other.gameObject.name);
        Destroy(gameObject);
    }
}
