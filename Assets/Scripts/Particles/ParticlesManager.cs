using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using BitStreams;
using RocketNet;
using UnityEngine;
using Random = UnityEngine.Random;

public class ParticlesManager : MonoBehaviour
{
    public class TrunkPosition
    {
        public Vector3b Position;
        public Vector3b Bits;
        public TrunkPosition( Vector3b position, Vector3b bits)
        {
            Position = position;
            Bits = bits;
        }
    }
    public class BakedParticles {
        public int TotalFrames;
        public int ParticlesPerFrame;
        public float TotalTime;
        public TrunkPosition[] StartingPositions = new TrunkPosition[1200];
        public byte[] Bytestream = new byte[1024 * 1024 * 2];
        public int MaxBitstream;
    }
    private Track _orbitX;
    private Track _orbitY;
    private Track _orbitZ;
    private Track _repulsion;
    private Track _repulsionPower;
    private Track _gravity;
    private Track _gravityPower;
    private Track _fakeBokeh;
    private Track _friction;
    private Track[] _effect;
    private Track _windX;
    private Track _windY;
    private Track _windZ;
    private BakedParticles _bakedParticles;
    private Vector3[] _startingPositions;
    private Dictionary<int, List<Vector3>> _deltaChange;
    [SerializeField] private DeviceController _deviceController;
    [SerializeField] private ParticleSystem particleSystem;
    public List<byte> DeltaMovement;
    public struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    }

    public ComputeShader computeShader;

    ComputeBuffer particles;

    const int WARP_SIZE = 1024;

    int size = 1024000;
    int stride;

    int warpCount;

    int kernelIndex;

    Particle[] initBuffer;
    private float bakeTime = 0f;
    string filePath;
    void SaveObject(BakedParticles obj)
    {
        using (FileStream file = File.Create(filePath))
        {
            using (BinaryWriter writer = new BinaryWriter(file))
            {
                writer.Write(obj.TotalFrames);
                writer.Write(obj.TotalTime);
                writer.Write(obj.ParticlesPerFrame);
                for(int i = 0; i < 1200; i++) 
                {
                    var startPosition = _bakedParticles.StartingPositions[i];
                    writer.Write(startPosition.Bits.X);
                    writer.Write(startPosition.Bits.Y);
                    writer.Write(startPosition.Bits.Z);
                    writer.Write(startPosition.Position.X);
                    writer.Write(startPosition.Position.Y);
                    writer.Write(startPosition.Position.Z);
                }
                for(int i = 0; i < obj.MaxBitstream; i++)
                {
                    var strbyte = obj.Bytestream[i];
                    writer.Write(strbyte);
                }
                writer.Close();
            }
            file.Close();
        }
    }

    // Use this for initialization

    void Start ()
    {
        if (_deviceController.InTrackRecordMode)
        {
            Application.targetFrameRate = 25;
        }
        warpCount = Mathf.CeilToInt((float)size / WARP_SIZE);

        stride = Marshal.SizeOf(typeof(Particle));
        particles = new ComputeBuffer(size, stride);

        initBuffer = new Particle[particleSystem.main.maxParticles];
        _startingPositions = new Vector3[particleSystem.main.maxParticles];
        for (int i = 0; i < particleSystem.main.maxParticles; i++)
        {
            initBuffer[i] = new Particle();
            _startingPositions[i] = initBuffer[i].position = new Vector3(Random.Range(-10.0f,10.0f), Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f));
            initBuffer[i].velocity = Vector3.zero;
        }

        particles.SetData(initBuffer);

        kernelIndex = computeShader.FindKernel("Update");
        
        computeShader.SetBuffer(kernelIndex, "Particles", particles);

        _orbitX = _deviceController.Device.GetTrack("Orbit X");
        _orbitY = _deviceController.Device.GetTrack("Orbit Y");
        _orbitZ = _deviceController.Device.GetTrack("Orbit Z");
        _repulsion = _deviceController.Device.GetTrack("Repulsion");
        _repulsionPower = _deviceController.Device.GetTrack("RepulsionPower");
        _gravity = _deviceController.Device.GetTrack("Gravity");
        _gravityPower = _deviceController.Device.GetTrack("GravityPower");
        _fakeBokeh = _deviceController.Device.GetTrack("FakeBokeh");
        _friction = _deviceController.Device.GetTrack("Friction");
        _effect = new Track[6];
        _effect[0] = _deviceController.Device.GetTrack("EffectA");
        _effect[1] = _deviceController.Device.GetTrack("EffectB");
        _effect[2] = _deviceController.Device.GetTrack("EffectC");
        _effect[3] = _deviceController.Device.GetTrack("EffectD");
        _effect[4] = _deviceController.Device.GetTrack("EffectE");
        _effect[5] = _deviceController.Device.GetTrack("EffectF");
        _windX = _deviceController.Device.GetTrack("Wind X");
        _windY = _deviceController.Device.GetTrack("Wind Y");
        _windZ = _deviceController.Device.GetTrack("Wind Z");

        _bakedParticles = new BakedParticles();
        _bakedParticles.TotalFrames = Mathf.CeilToInt(_deviceController.GetAudioSource().clip.length * 25);
        _bakedParticles.TotalTime = _deviceController.GetAudioSource().clip.length;
        _bakedParticles.ParticlesPerFrame = particleSystem.main.maxParticles;
        filePath = Application.dataPath + "/objectData.bin";
        Debug.Log(filePath);
        DeltaMovement = new List<byte>();
        _deltaChange = new Dictionary<int, List<Vector3>>();
    }
    public static int CountBits(int value)
    {
        // Special case for zero
        if (value == 0)
            return 1;

        // Calculate the number of bits required
        return (int)Math.Floor(Math.Log(Math.Abs(value), 2)) + 1;
    }
    void ParticleCompression()
    {
        var bitstream = new BitStreams.BitStream(_bakedParticles.Bytestream);
        int[] scale = new int[particleSystem.main.maxParticles * 3 + 3];
        foreach (var deltaDict in _deltaChange)
        {
            var deltaArray = deltaDict.Value;
            for (int i = 0; i < deltaArray.Count; i++)
            {
                byte x = (byte)((byte)(Mathf.FloorToInt(Mathf.Abs(deltaArray[i].x * 32f)) << 1) + (byte)((Mathf.Sign(deltaArray[i].x) > 0) ? 1 : 0));
                byte y = (byte)((byte)(Mathf.FloorToInt(Mathf.Abs(deltaArray[i].y * 32f)) << 1) + (byte)((Mathf.Sign(deltaArray[i].y) > 0) ? 1 : 0));
                byte z = (byte)((byte)(Mathf.FloorToInt(Mathf.Abs(deltaArray[i].z * 32f)) << 1) + (byte)((Mathf.Sign(deltaArray[i].z) > 0) ? 1 : 0));
                DeltaMovement.Add(x);
                DeltaMovement.Add(y);
                DeltaMovement.Add(z);
                scale[i * 3 + 0] = Math.Max(CountBits(x), scale[i * 3 + 0]);
                scale[i * 3 + 1] = Math.Max(CountBits(y), scale[i * 3 + 1]);
                scale[i * 3 + 2] = Math.Max(CountBits(z), scale[i * 3 + 2]);
                Debug.Log("Scale:" + scale[i * 3 + 0]);
            }
        }
        for (int i = 0; i < _startingPositions.Length; i++)
        {
            byte x = (byte)((byte)(Mathf.FloorToInt(Mathf.Abs(_startingPositions[i].x * 8f)) << 1) + (byte)((Mathf.Sign(_startingPositions[i].x) > 0) ? 1 : 0));
            byte y = (byte)((byte)(Mathf.FloorToInt(Mathf.Abs(_startingPositions[i].y * 8f)) << 1) + (byte)((Mathf.Sign(_startingPositions[i].y) > 0) ? 1 : 0));
            byte z = (byte)((byte)(Mathf.FloorToInt(Mathf.Abs(_startingPositions[i].z * 8f)) << 1) + (byte)((Mathf.Sign(_startingPositions[i].z) > 0) ? 1 : 0));
            _bakedParticles.StartingPositions[i] = (new TrunkPosition(new Vector3b(x, y, z), new Vector3b((byte)scale[i * 3 + 0], (byte)scale[i * 3 + 1], (byte)scale[i * 3 + 2])));
        }
        for (int i = 0; i< DeltaMovement.Count; i++)
        {
            var howManyBits = scale[i % (particleSystem.main.maxParticles * 3)];
            bitstream.WriteByte(DeltaMovement[i], howManyBits);
            if(i%(particleSystem.main.maxParticles * 3) == 0)
            {
                Debug.Log("BitOffset:" + bitstream.BitOffsetPosition / 8);
            }
        }
        Debug.Log(bitstream.BitOffsetPosition);
        _bakedParticles.MaxBitstream = (int)bitstream.BitOffsetPosition ;
        SaveObject(_bakedParticles);
    }
	// Update is called once per frame
	void Update () {

        if (_deviceController.InTrackRecordMode)
        {
            computeShader.SetFloat("dt", 1.0f / 25f);
        }
        else
        {
            computeShader.SetFloat("dt", Time.deltaTime);
        }
        computeShader.SetFloat("OrbitX", _deviceController.GetValue(_orbitX));
        computeShader.SetFloat("OrbitY", _deviceController.GetValue(_orbitY));
        computeShader.SetFloat("OrbitZ", _deviceController.GetValue(_orbitZ));
        computeShader.SetFloat("Repulsion", _deviceController.GetValue(_repulsion));
        computeShader.SetFloat("RepulsionPower", _deviceController.GetValue(_repulsionPower));
        computeShader.SetFloat("Gravity", _deviceController.GetValue(_gravity));
        computeShader.SetFloat("GravityPower", _deviceController.GetValue(_gravityPower));
        computeShader.SetFloat("Friction", _deviceController.GetValue(_friction));
        computeShader.SetFloat("EffectA", _deviceController.GetValue(_effect[0]));
        computeShader.SetFloat("EffectB", _deviceController.GetValue(_effect[1]));
        computeShader.SetFloat("EffectC", _deviceController.GetValue(_effect[2]));
        computeShader.SetFloat("EffectD", _deviceController.GetValue(_effect[3]));
        computeShader.SetFloat("EffectE", _deviceController.GetValue(_effect[4]));
        computeShader.SetFloat("EffectF", _deviceController.GetValue(_effect[5]));
        computeShader.SetFloat("WindX", _deviceController.GetValue(_windX));
        computeShader.SetFloat("WindY", _deviceController.GetValue(_windY));
        computeShader.SetFloat("WindZ", _deviceController.GetValue(_windZ));
        computeShader.Dispatch(kernelIndex, warpCount, 1, 1);
        particles.GetData(initBuffer);
            List<Vector3> deltaArray = new List<Vector3>();
            var particlesArray = new ParticleSystem.Particle[particleSystem.main.maxParticles];
            var currentAmount = particleSystem.GetParticles(particlesArray);
            // Change only the particles that are alive
            for (int i = 0; i < currentAmount; i++)
            {
                deltaArray.Add(initBuffer[i].position - particlesArray[i].position);
                particlesArray[i].position = initBuffer[i].position;
                particlesArray[i].size = initBuffer[i].position.magnitude * _deviceController.GetValue(_fakeBokeh) * 0.1f + 0.2f;
                bakeTime = _deviceController.GetAudioSource().time;
            }

            // Apply the particle changes to the Particle System
            particleSystem.SetParticles(particlesArray, currentAmount);
        if (_deviceController.InTrackRecordMode)
        {
            int key = Mathf.FloorToInt(bakeTime * 50.0f);
            if (!_deltaChange.ContainsKey(key) && key > 2)
                _deltaChange.Add(key, deltaArray);
        }

    }

    void OnDestroy()
    {
        if (particles != null)
            particles.Release();
        if (_deviceController.InTrackRecordMode)
        {
            ParticleCompression();
        }
    }
}
