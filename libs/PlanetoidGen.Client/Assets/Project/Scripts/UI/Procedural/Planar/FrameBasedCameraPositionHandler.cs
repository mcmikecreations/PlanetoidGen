using UnityEngine;

public class FrameBasedCameraPositionHandler : MonoBehaviour
{
    public int targetUpdateFrameRate = 60;

    private int _currentFrame = 0;

    void Update()
    {
        ++_currentFrame;

        if (_currentFrame >= targetUpdateFrameRate)
        {
            var cameraPosition = transform.position;

            Debug.Log("Frame-based camera position: " + cameraPosition);

            _currentFrame = 0;
        }
    }
}
