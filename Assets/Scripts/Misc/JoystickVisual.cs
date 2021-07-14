using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickVisual : MonoBehaviour
{
    private Vector3 initEuler;

    // Start is called before the first frame update
    void Start()
    {
        initEuler = transform.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        transform.eulerAngles = initEuler + new Vector3(30.0f * -Input.GetAxis("Vertical"), 30.0f * -Input.GetAxis("Horizontal"));
    }
}
