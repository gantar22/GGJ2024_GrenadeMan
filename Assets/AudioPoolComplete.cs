using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class AudioPoolComplete : MonoBehaviour
{

    public AudioSource source;
    public IObjectPool<AudioSource> myPool;

    void Update()
    {
        if(!source.isPlaying)
        {
            myPool.Release(source);
        }
    }
}
