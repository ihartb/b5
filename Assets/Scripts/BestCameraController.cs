using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BestCameraController : MonoBehaviour
{
    [Range(.1f, 10f)]
    public float speed;
    [Range(.1f, 5f)]
    public float rotation;

    private Vector2 currentRotation = new Vector2(0f,50f);

    public float GetRotation()
    {
        return Mathf.PI * currentRotation.x / 180.0f;
    }

    void Start()
    {
        // Cursor.SetCursor(null, new Vector2(0, 0), CursorMode.Auto);
        transform.rotation = Quaternion.Euler(60, 0, 00);
    }

    // Update is called once per frame
    void Update()
    {
        var verticalSpeed = speed;
        float angle = Mathf.PI * currentRotation.x / 180.0f;
        // print(currentRotation.x.ToString() + " "+currentRotation.y.ToString());
        float forward_component = Input.GetAxis("Horizontal") * Mathf.Cos(-angle)
                                + Input.GetAxis("Vertical")   * Mathf.Sin(angle);
        float horizontal_component =  Input.GetAxis("Horizontal") * Mathf.Sin(-angle)
                                    + Input.GetAxis("Vertical")   * Mathf.Cos(angle);
        var moveVector =
            new Vector3(forward_component, 0, horizontal_component) * speed
            + Vector3.up * (Input.GetKey("space") ? verticalSpeed : 0)
            - Vector3.up * (Input.GetKey("left shift") ? verticalSpeed : 0);
        transform.position += moveVector * Time.deltaTime;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
        currentRotation.x += Input.GetAxis("Mouse X") * rotation;
        currentRotation.y -= Input.GetAxis("Mouse Y") * rotation;
        transform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);

        // if (Input.GetKeyDown("escape"))
        // {
        //     Cursor.lockState = CursorLockMode.None;
        // }
    }
}
