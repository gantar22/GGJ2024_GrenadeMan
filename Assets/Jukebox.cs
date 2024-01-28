using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;

public class Jukebox : MonoBehaviour
{
    public static Jukebox Instance;

    [SerializeField] AudioClip[] clips;
    [SerializeField] AudioSource[] tracks;
    [SerializeField] AudioMixer mixer;

    private void Awake()
    {
        Instance = this;
        _previousClip = -1;
        Random.InitState(System.DateTime.Now.GetHashCode());
    }

    public bool StartMuffled;
    private void Start()
    {
        if(StartMuffled)
        {
            mixer.SetFloat("LowpassFreq", 900f);
            mixer.SetFloat("ReverbRoom", -2000f);
            _muffled = true;
        }
        ShuffleSong();
    }

    int _previousClip;
    AudioClip GetRandomClip()
    {
        int targetSong = Random.Range(0, _previousClip == -1 ? clips.Length : clips.Length - 1);
        if(targetSong == _previousClip)
        {
            targetSong = (targetSong + 1) % clips.Length;
        }
        _previousClip = targetSong;

        return clips[targetSong];
    }


    int _currentTrack;
    bool _shuffling;
    readonly float fadeSpeed = 5f;
    [ContextMenu("Shuffle Song")]
    public async void ShuffleSong()
    {
        if (_shuffling)
            return;
        _shuffling = true;

        int _previousTrack = _currentTrack;
        _currentTrack = (_currentTrack + 1) % tracks.Length;

        tracks[_currentTrack].clip = GetRandomClip();
        tracks[_currentTrack].Play();
        tracks[_previousTrack].DOFade(0f, fadeSpeed);
        await System.Threading.Tasks.Task.Delay(1000);
        await tracks[_currentTrack].DOFade(1f, fadeSpeed).AsyncWaitForCompletion();

        tracks[_previousTrack].Stop();

        _shuffling = false;
    }

    private void Update()
    {
        if(!_shuffling && tracks[_currentTrack].clip != null && (tracks[_currentTrack].time / tracks[_currentTrack].clip.length) > 0.95f)
        {
            ShuffleSong();
        }
    }


    bool _muffled;
    readonly float muffleSpeed = 3f;

    [ContextMenu("Toggle Muffle")]
    public void ToggleMuffle(bool inMuffle)
    {
        StopAllCoroutines();
        StartCoroutine(MuffleLerp(inMuffle));
    }

    IEnumerator MuffleLerp(bool inMuffled)
    {
        _muffled = inMuffled;

        mixer.GetFloat("LowpassFreq", out float lowpassFreq);
        float targetFreq = _muffled ? 900f : 22000f;

        float lerpSpeed = muffleSpeed * (22000f - 900f) / Mathf.Abs(lowpassFreq - targetFreq);

        float targetReverb = _muffled ? -2000f : -10000f;

        mixer.DOSetFloat("LowpassFreq", targetFreq, lerpSpeed);

        yield return mixer.DOSetFloat("ReverbRoom", targetReverb, lerpSpeed).WaitForCompletion();
    }
}
