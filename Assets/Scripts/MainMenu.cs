using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public int index = 0;
    public SpriteRenderer[] items;

    public FadeOut ree;
    public AudioSource source;
    public AudioClip moveclip;
    public AudioClip boomclip;

    // Start is called before the first frame update
    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) {
            index--;
            if(index < 0) index += items.Length;
            source.PlayOneShot(moveclip);
        } else if(Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) {
            index++;
            if(index >= items.Length) index -= items.Length;
            source.PlayOneShot(moveclip);
        }

        for(int i = 0; i < items.Length; i++) {
            if(index == i) items[i].color = new Color(1,1,1,1);
            else items[i].color = new Color(0.5f,0.5f,0.5f,0.5f);
        }
        
        if(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) {
            if(index == 0) {
                ree.fadeTask = "Scenes/Tutorial";
                ree.target = 1;
                enabled = false;
            } else if(index == 1) {
                ree.fadeTask = "Scenes/Level1";
                ree.target = 1;
                enabled = false;
            }
            source.PlayOneShot(boomclip);
            enabled = false;
        }
    }
}
