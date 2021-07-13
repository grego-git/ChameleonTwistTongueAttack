using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimBlock : MonoBehaviour
{
    public bool inverse;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation *= Quaternion.Euler(new Vector3(0.0f, 0.0f, inverse ? -0.25f : 0.25f));
    }
}
