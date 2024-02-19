using System.Collections;
using System.Collections.Generic;
using RocketNet;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    private Track _cameraPositionX;
    private Track _cameraPositionY;
    private Track _cameraPositionZ;
    private Track _cameraRotationX;
    private Track _cameraRotationY;
    private Track _cameraRotationZ;
    private Track _cameraFieldOfView;
    private Camera _camera;
    [SerializeField] private DeviceController _deviceController;
    void Start()
    {
        _cameraPositionX = _deviceController.Device.GetTrack("Camera X");
        _cameraPositionY = _deviceController.Device.GetTrack("Camera Y");
        _cameraPositionZ = _deviceController.Device.GetTrack("Camera Z");
        _cameraRotationX = _deviceController.Device.GetTrack("Camera Yaw");
        _cameraRotationY = _deviceController.Device.GetTrack("Camera Pitch");
        _cameraRotationZ = _deviceController.Device.GetTrack("Camera Roll");
        _cameraFieldOfView = _deviceController.Device.GetTrack("CamFOV");
        _camera = GetComponent<Camera>();
    }

    void Update()
    {
        Vector3 cameraPosition = new Vector3(
            _deviceController.GetValue(_cameraPositionX),
            _deviceController.GetValue(_cameraPositionY),
            _deviceController.GetValue(_cameraPositionZ));
        Vector3 cameraRotation = new Vector3(
            _deviceController.GetValue(_cameraRotationX),
            _deviceController.GetValue(_cameraRotationY),
            _deviceController.GetValue(_cameraRotationZ));
        float cameraFOV = _deviceController.GetValue(_cameraFieldOfView);
        transform.position = cameraPosition;
        transform.rotation = Quaternion.Euler(cameraRotation);
        _camera.fieldOfView = cameraFOV;
    }
}
