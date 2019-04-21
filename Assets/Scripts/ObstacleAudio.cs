using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleAudio : MonoBehaviour
{
    public float minPulseFrequency = 1f / 5f;
    public float maxPulseFrequency = 8f;
    public AudioSource audioSource;
    public float cameraBoxSize = 2f;
    public float maxPitch = 1.0f;
    public float minPitch = 0.5f;

    private Camera _camera;

    private void Awake()
    {
    }

    // Use this for initialization
    void Start()
    {
        _camera = Camera.main;
        //AudioClip obstacleClip = AudioClip.Create("V_RIOT_synth_one_shot_music_box_02_E",1, 1, 1, true);    THIS WILL CRASH UNITY
        audioSource = GetComponentInParent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        //TODO: find the realtive height with cam
        //TODO: distance
        
        double dist = Vector3.Distance(transform.position, _camera.gameObject.transform.position);
        float newPitch = 0f;
        float heightDifference = transform.position.y - _camera.transform.position.y;
        //Debug.Log("Height difference for " + this.name + ": " + heightDifference);

        if (heightDifference >= cameraBoxSize * 0.25)
        {
            newPitch = maxPitch;
            // Debug.Log(beacon.name + "Maximum pitch reached");
        }

        else if (heightDifference <= cameraBoxSize * (-0.75))
        {
            newPitch = minPitch;
            // Debug.Log(beacon.name + "Minimum pitch reached");
        }

        else
        {
            newPitch = (minPitch + (maxPitch - minPitch) * (heightDifference + 0.75f * cameraBoxSize) / cameraBoxSize);
            // Debug.Log(beacon.name + " New pitch: " + newPitch);
        }

        audioSource.pitch = newPitch;

    }

}