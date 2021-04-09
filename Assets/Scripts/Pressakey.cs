using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pressakey : MonoBehaviour
{
    public AudioSource toStart;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.anyKeyDown) {
            toStart.Play();
            enabled = false;
            gameObject.SetActive(false);
        }
    }
}
