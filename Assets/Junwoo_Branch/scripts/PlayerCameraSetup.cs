using Photon.Pun;
using UnityEngine;

public class PlayerCameraSetup : MonoBehaviourPun
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    private void Start()
    {
        bool isMyPlayer = photonView.IsMine;

        if (playerCamera != null)
        {
            playerCamera.enabled = isMyPlayer;
        }

        if (audioListener != null)
        {
            audioListener.enabled = isMyPlayer;
        }

        Debug.Log(
            $"카메라 설정 / ViewID={photonView.ViewID} / " +
            $"IsMine={isMyPlayer} / " +
            $"CameraEnabled={(playerCamera != null && playerCamera.enabled)}"
        );
    }
}