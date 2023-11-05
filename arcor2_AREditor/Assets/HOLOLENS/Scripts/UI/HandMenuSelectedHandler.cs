using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandMenuSelectedHandler : MonoBehaviour
{
    public Vector3 position = new Vector3(0f,-0.00209999993f,0.00949999969f);

    // Update is called once per frame
    void Update()
    {
        if(transform.hasChanged){
            transform.localPosition = position;
            transform.hasChanged = false;
        }
        
    }
}
