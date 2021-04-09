using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterThingy : MonoBehaviour
{
    public Transform arm;
    public float targetAngle = 0;
    public float angle = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        //
        angle = Mathf.LerpAngle(angle, targetAngle, 0.1f);
    }
    // Update is called once per frame
    void Update()
    {
        arm.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
