using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
    public float speed;

    float timer;

    Vector3 origin;

    // Start is called before the first frame update
    void Start()
    {
        origin = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = origin + new Vector3(Mathf.Sin(timer += speed * Time.deltaTime), Mathf.Sin(timer += speed / 5.0f * Time.deltaTime) * 0.5f, 0.0f);
    }
}
