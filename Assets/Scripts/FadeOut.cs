using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeOut : MonoBehaviour
{
    public float target = 0;
    public float curr = 0;

    public string fadeTask;
    public SpriteRenderer asdf;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(target > curr) {
            curr = Mathf.Min(curr + Time.deltaTime * 0.3f, target);
        }
        else if(target < curr) {
            curr = Mathf.Max(curr - Time.deltaTime * 0.3f, target);
        }
        asdf.color = new Color(0,0,0,curr);
        if(fadeTask != null && curr >= 1.0f) SceneManager.LoadScene(fadeTask);
    }
}
