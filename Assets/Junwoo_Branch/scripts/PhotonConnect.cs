using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonConnect : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        Debug.Log("Photon 서버 접속 시도...");
        PhotonNetwork.ConnectUsingSettings();
    }

    // Photon 서버 연결 성공
    public override void OnConnectedToMaster()
    {
        Debug.Log("서버 연결 성공");

        PhotonNetwork.JoinOrCreateRoom(
            "TestRoom",
            new RoomOptions { MaxPlayers = 4 },
            TypedLobby.Default
        );
    }

    // 방 입장 성공
    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공");

        Vector3 spawnPos;

        // 첫 번째 플레이어와 두 번째 플레이어를 다른 위치에 생성
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
        {
            spawnPos = new Vector3(-2f, 1f, 0f);
        }
        else
        {
            spawnPos = new Vector3(2f, 1f, 0f);
        }

        // ★ player_clone 프리팹 생성
        PhotonNetwork.Instantiate(
            "player_clone",
            spawnPos,
            Quaternion.identity
        );
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("연결 종료 : " + cause);
    }
}