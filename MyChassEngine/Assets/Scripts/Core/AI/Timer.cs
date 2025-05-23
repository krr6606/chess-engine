using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public float GameTime = 0;
    void Awake()
    {
    }

    // Update is called once per frame
    void Update()
    {
        GameTime = Time.time;

    }
}
