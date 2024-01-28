using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class SoundMachine : MonoBehaviour
{
    [SerializeField] GameObject SoundPoolMember;
    public enum PoolType
    {
        Stack,
        LinkedList
    }

    public PoolType poolType;
    public bool collectionChecks = true;
    public int maxPoolSize = 40;

    IObjectPool<AudioSource> _soundPool;

    

    AudioSource CreatePooledItem()
    {
        GameObject newMember = Instantiate(SoundPoolMember, transform);
        AudioSource source = newMember.GetComponent<AudioSource>();
        AudioPoolComplete completeRoutine = newMember.GetComponent<AudioPoolComplete>();
        completeRoutine.source = source;
        completeRoutine.myPool = _soundPool;
        return source;
    }

    void OnReturnedToPool(AudioSource source)
    {
        source.gameObject.SetActive(false);
    }

    void OnTakeFromPool(AudioSource source)
    {
        source.gameObject.SetActive(true);
    }

    void OnDestroyPoolObject(AudioSource source)
    {
        Destroy(source.gameObject);
    }

    readonly float randomStrength = 0.1f;
    public void PlaySound(string soundName, bool randomPitch = true)
    {
        AudioSource newSource = _soundPool.Get();

        if(randomPitch)
        {
            float pitch = Random.Range(1f - randomStrength, 1f + randomStrength);
            newSource.pitch = pitch;
        }

        AudioClip clip = Resources.Load<AudioClip>("Sounds/" + soundName);
        newSource.clip = clip;
        newSource.Play();
    }

    private void Awake()
    {
        _soundPool = new ObjectPool<AudioSource>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, collectionChecks, 20,maxPoolSize);
    }

    [ContextMenu("Test")]
    void TestAudio()
    {
        PlaySound("Throw");
    }
}
