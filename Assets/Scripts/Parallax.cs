using UnityEngine;
using Unity.Cinemachine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private Transform background;
    [SerializeField] private float parallaxStrength = 0.5f;

    private Vector3 lastCameraPosition;

    private void Awake()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(UpdateParallax);
    }

    private void OnDestroy()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateParallax);
    }

    private void Start()
    {
        lastCameraPosition = transform.position;
    }

    private void UpdateParallax(CinemachineBrain brain)
    {
        Vector3 cameraDelta = transform.position - lastCameraPosition;
        background.position += new Vector3(
            cameraDelta.x * parallaxStrength,
            cameraDelta.y * parallaxStrength,
            0f
        );

        lastCameraPosition = transform.position;
    }
}