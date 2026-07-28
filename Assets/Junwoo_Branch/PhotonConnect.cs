using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonConnect : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        PhotonNetwork.GameVersion = "1.0";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;

        PhotonNetwork.JoinOrCreateRoom(
            "TestRoom",
            roomOptions,
            TypedLobby.Default
        );
    }

    public override void OnJoinedRoom()
    {
        Vector3 spawnPos;

        if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
            spawnPos = new Vector3(-2, 1, 0);
        else
            spawnPos = new Vector3(2, 1, 0);

        PhotonNetwork.Instantiate(
            "NetworkPlayer",
            spawnPos,
            Quaternion.identity
        );
    }
}