using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonConnect : MonoBehaviourPunCallbacks
{
    // Unity Inspector에서 스폰 위치를 직접 연결
    [Header("플레이어 스폰 포인트")]
    public Transform spawnPoint1;   // 첫 번째 플레이어 생성 위치
    public Transform spawnPoint2;   // 두 번째 플레이어 생성 위치

    // [추가]
    // 매칭 대기 중 사용할 카메라
    [Header("매칭 대기 카메라")]
    public Camera waitingCamera;

    // 같은 플레이어가 두 번 생성되는 것을 방지
    private bool playerSpawned = false;


    private void Start()
    {
        // =============================================
        // [추가]
        // 게임 시작 시 웨이팅 카메라를 켜둔다.
        // 플레이어가 아직 생성되지 않았기 때문에
        // 이 카메라로 맵의 특정 장소를 보여준다.
        // =============================================
        if (waitingCamera != null)
        {
            waitingCamera.gameObject.SetActive(true);
        }

        Debug.Log("Photon 서버 접속 시도...");

        // Photon 서버 접속 시작
        PhotonNetwork.ConnectUsingSettings();
    }


    // ==================================================
    // Photon 서버 연결 성공
    // ==================================================
    public override void OnConnectedToMaster()
    {
        Debug.Log("서버 연결 성공");

        // 기존 TestRoom 사용
        // 방이 있으면 입장하고 없으면 생성
        PhotonNetwork.JoinOrCreateRoom(
            "TestRoom",
            new RoomOptions
            {
                MaxPlayers = 2
            },
            TypedLobby.Default
        );
    }


    // ==================================================
    // 방 입장 성공
    // ==================================================
    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공");

        // 바로 플레이어를 생성하지 않고
        // 2명이 모였는지 먼저 확인
        CheckPlayerCount();
    }


    // ==================================================
    // 다른 플레이어가 방에 들어왔을 때 실행
    // ==================================================
    public override void OnPlayerEnteredRoom(
        Photon.Realtime.Player newPlayer)
    {
        Debug.Log(
            "다른 플레이어가 방에 들어왔습니다."
        );

        // 새로운 플레이어가 들어왔으므로
        // 현재 방 인원을 다시 확인
        CheckPlayerCount();
    }


    // ==================================================
    // 현재 방에 2명이 모였는지 확인
    // ==================================================
    private void CheckPlayerCount()
    {
        // 혹시 방에 아직 들어가지 않은 상태라면 실행하지 않음
        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        int playerCount =
            PhotonNetwork.CurrentRoom.PlayerCount;

        Debug.Log(
            "현재 인원 : " +
            playerCount +
            " / 2"
        );


        // ---------------------------------------------
        // 아직 1명이라면 게임 시작하지 않고 기다림
        // ---------------------------------------------
        if (playerCount < 2)
        {
            Debug.Log(
                "상대방을 기다리는 중... 1 / 2"
            );

            // 웨이팅 카메라는 계속 켜둠
            if (waitingCamera != null)
            {
                waitingCamera.gameObject.SetActive(true);
            }

            return;
        }


        // ---------------------------------------------
        // 2명이 모이면 매칭 완료
        // ---------------------------------------------
        Debug.Log("매칭 완료! 게임 시작!");


        // 방장만 방을 닫음
        // 새로운 플레이어가 들어오는 것을 방지
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }


        // =============================================
        // [추가]
        // 매칭 완료되면 웨이팅 카메라 끄기
        // 이후 생성되는 player_clone의 카메라를 사용
        // =============================================
        if (waitingCamera != null)
        {
            waitingCamera.gameObject.SetActive(false);
        }


        // 자기 플레이어 생성
        SpawnPlayer();
    }


    // ==================================================
    // 자기 player_clone 생성
    // ==================================================
    private void SpawnPlayer()
    {
        // ---------------------------------------------
        // 중복 생성 방지
        // ---------------------------------------------
        if (playerSpawned)
        {
            return;
        }


        // ---------------------------------------------
        // SpawnPoint 연결 확인
        // ---------------------------------------------
        if (spawnPoint1 == null ||
            spawnPoint2 == null)
        {
            Debug.LogError(
                "SpawnPoint1 또는 SpawnPoint2가 연결되지 않았습니다."
            );

            return;
        }


        // 플레이어가 생성될 위치
        Vector3 spawnPos;

        // 플레이어가 처음 바라볼 방향
        Quaternion spawnRot;


        // ---------------------------------------------
        // 첫 번째 플레이어
        // ---------------------------------------------
        if (PhotonNetwork.LocalPlayer.ActorNumber == 1)
        {
            spawnPos = spawnPoint1.position;
            spawnRot = spawnPoint1.rotation;

            Debug.Log(
                "플레이어 1 → SpawnPoint1에서 생성"
            );
        }


        // ---------------------------------------------
        // 두 번째 플레이어
        // ---------------------------------------------
        else
        {
            spawnPos = spawnPoint2.position;
            spawnRot = spawnPoint2.rotation;

            Debug.Log(
                "플레이어 2 → SpawnPoint2에서 생성"
            );
        }


        // ---------------------------------------------
        // player_clone을 Photon 네트워크로 생성
        // ---------------------------------------------
        PhotonNetwork.Instantiate(
            "player_clone",
            spawnPos,
            spawnRot
        );


        // 생성 완료 기록
        playerSpawned = true;

        Debug.Log("player_clone 생성 완료");
    }


    // ==================================================
    // Photon 연결이 끊어졌을 때
    // ==================================================
    public override void OnDisconnected(
        DisconnectCause cause)
    {
        Debug.LogError(
            "연결 종료 : " + cause
        );
    }
}