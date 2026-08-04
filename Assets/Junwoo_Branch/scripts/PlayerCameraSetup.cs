using Photon.Pun;
using UnityEngine;

public class PlayerCameraSetup : MonoBehaviourPun
{
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    private AudioListener audioListener;

    private void Start()
    {
        bool isMyPlayer = photonView.IsMine;

        playerCamera.enabled = isMyPlayer;
        audioListener.enabled = isMyPlayer;
    }
}