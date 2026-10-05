using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField]
    private Vector3 offset = new Vector3(0f, 4f, -6f);

    [SerializeField]
    private float followSpeed = 8f;

    [SerializeField]
    private float lookHeight = 1f;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition =
            target.position + offset;

        transform.position =
            Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );

        transform.LookAt(
            target.position + Vector3.up * lookHeight
        );
    }
}