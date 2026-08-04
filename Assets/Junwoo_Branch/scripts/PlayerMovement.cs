using Photon.Pun;
using UnityEngine;

public class PlayerMovement : MonoBehaviourPun
{
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float mouseSensitivity = 150f;

    [SerializeField]
    private Transform cameraHolder;

    private float cameraPitch;

    private void Start()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        Move();
        Look();
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction =
            transform.right * horizontal +
            transform.forward * vertical;

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        transform.position +=
            direction * moveSpeed * Time.deltaTime;
    }

    private void Look()
    {
        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity *
            Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity *
            Time.deltaTime;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        cameraHolder.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }
}