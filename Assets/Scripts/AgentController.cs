using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class AgentController : MonoBehaviour
{
    public GameObject agentObject;
    private NavMeshAgent agent;
    public GameObject target;
    public bool canMove = false;
    private bool targetSelected = false;

    // Start is called before the first frame update
    void Start()
    {
        agent = agentObject.GetComponent<NavMeshAgent>();

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            //select target
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray.origin, ray.direction, out hit))
            {
                target.transform.position = hit.point;
            }
            targetSelected = true;
        }
        // Change target for selected agent
        if (targetSelected)
        {
            agent.SetDestination(target.transform.position);
        }
    }

}
