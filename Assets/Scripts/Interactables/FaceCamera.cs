using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [SerializeField] private Vector3 offsetFromParent = new Vector3(0f, 1.5f, 0f);

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (transform.parent == null) return;


        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;   // no camera this frame, skip quietly
        }

        transform.position = transform.parent.position + offsetFromParent;

        Vector3 directionToCamera = _mainCamera.transform.position - transform.position;
        directionToCamera.y = 0;

        if (directionToCamera == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
        transform.rotation = targetRotation;
    }
}
