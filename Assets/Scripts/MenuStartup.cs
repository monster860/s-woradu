using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuStartup : MonoBehaviour
{
    AudioSource source;
    SpriteRenderer spriteRenderer;
    ParticleSystem particles;
    bool done = false;
    bool didParticles = false;
    public GameObject mehTarget;
    void Awake()
    {
        source = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        particles = GetComponent<ParticleSystem>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(done) return;
        if(source.time > 25) {
            done = true;
            mehTarget.SetActive(true);
        }
        spriteRenderer.enabled = (
            (source.time > 3.23 && source.time < 12)
            || (source.time > 14.02 && source.time < 20)
            || (source.time > 22.07)
        );
        if(source.time > 22.07 && !didParticles) {
            didParticles = true;
            particles.Play();
        }
        if(source.time < 13) transform.localRotation = Quaternion.Euler(0, 0, 75);
        else if(source.time < 21) transform.localRotation = Quaternion.Euler(0, 0, 167);
        else transform.localRotation = Quaternion.Euler(0, 0, 5);
    }
}
