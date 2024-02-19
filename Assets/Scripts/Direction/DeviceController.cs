using System;
using RocketNet;
using UnityEngine;

public class DeviceController : MonoBehaviour
{
    [HideInInspector] public Device Device;
    [SerializeField] public bool RecordVideo = false;
    [SerializeField] public AudioSource _audioSource;
    [SerializeField] public bool InTrackRecordMode;
    private int rpb = 8;
    private int _BPM = 172;
    private float row_rate;

    public float RowRate()
    {
        return row_rate;
    }

    public AudioSource GetAudioSource()
    {
        return _audioSource;
    }

    public float GetRowTime()
    {
        float songTime = _audioSource.time;
        float rowRate = row_rate;
        return songTime * rowRate;
    }

    public float GetValue(Track track)
    {
        float rowTime = GetRowTime();
        float value = track.GetValue(rowTime + 0.001f);
        return value;
    }
    void OnApplicationQuit()
    {
        Device.SaveTracks();
        Device.Dispose();
    }
    void Awake()
    {
        row_rate = (float)((_BPM / 60.0) * rpb);
        Device = new Device("asm", InTrackRecordMode);
        Application.runInBackground = true;
        if (!Device.player)
        {
            bool connected = Device.Connect();
            Debug.Log("Trying to connect...");
            while (!connected)
            {
                Debug.Log("re-trying to connect...");
                connected = Device.Connect();
                Debug.Log($"Connection: {connected}");
            }
            Debug.Log($"Connection: {connected}");
        }

        Device.Pause += MusicPausers;
        Device.SetRow += SetRow;
        Device.IsPlaying += IsPlaying;
        if(InTrackRecordMode==true)
        _audioSource.Pause();
    }

    private bool IsPlaying()
    {
        return _audioSource.isPlaying;
    }

    private void SetRow(int row)
    {
        _audioSource.time = row / row_rate;
    }

    private void OnDestroy()
    {
        Device.Pause -= MusicPausers;
        Device.SetRow -= SetRow;
        Device.IsPlaying -= IsPlaying;
    }

    private void MusicPausers(bool pause)
    {
        if (pause)
        {
            _audioSource.Pause();
        }
        else
        {
            _audioSource.UnPause();
        }
    }

    private void Update()
    {
        if(Time.frameCount<10 && InTrackRecordMode == false)
        {
            _audioSource.Pause();
        } else if(InTrackRecordMode == true)
        {
            _audioSource.time += 1.0f / 25f;
            _audioSource.time = Mathf.Min( _audioSource.time, _audioSource.clip.length);
            _audioSource.Pause();
        }
        Device.Update((int)Mathf.Ceil(GetRowTime()) + 1);
        if (Input.GetKeyUp(KeyCode.F12))
        {
            bool connected = Device.Connect();
            while (!connected)
            {
                connected = Device.Connect();
            }
            Debug.Log($"Connection: {connected}");
        }
    }
}