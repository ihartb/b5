using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoughEmitter : MonoBehaviour
{
    public GameObject virusPrefab;
    public float emissionInterval = 1;
    public int emissionCount = 10;
    public float coughSpeed = 100;
    public float scatter = 1;

    private GameObject virusPool;

    void Start()
    {
        virusPool = GameObject.Find("VirusPool");

        StartCoroutine(Run());
    }
    
    IEnumerator Run()
    {
        while (true)
        {
            for (int i = 0; i < emissionCount; i++)
            {
                yield return new WaitForEndOfFrame();
                var virus = Instantiate(virusPrefab, transform.position, Quaternion.identity);
                virus.transform.parent = virusPool.transform;
                var rb = virus.GetComponent<Rigidbody>();
                rb.AddForce(transform.forward * coughSpeed + transform.up * (Random.value - 0.5f) * scatter + transform.right * (Random.value - 0.5f) * scatter, ForceMode.Acceleration);
                var virusScript = virus.GetComponent<VirusScript>();
            }

            yield return new WaitForSeconds(emissionInterval);
        }
    }
}
