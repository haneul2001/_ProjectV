using Photon.Pun;
using UnityEngine;

public class GrenadeProjectile_01 : MonoBehaviourPun
{
    [Header("폭발 오브젝트 연결")]
    public GameObject explosionPrefab; // 폭발 범위 및 데미지를 처리할 프리팹

    private bool hasExploded = false;

    // 오브젝트가 다른 콜라이더와 물리적으로 충돌했을 때 자동으로 호출되는 함수
    void OnCollisionEnter(Collision collision)
    {
        // 이 수류탄을 만든 사람만 폭발을 처리
        if (!photonView.IsMine)
        {
            return;
        }

        // [추가]
        // 수류탄이 처음 무엇과 충돌했는지 Console에서 확인
        Debug.Log(
            "수류탄 첫 충돌 대상 : " +
            collision.gameObject.name
        );

        // 이미 폭발했다면 중복 실행 방지
        if (hasExploded)
        {
            return;
        }

        Explode();
    }

    private void Explode()
    {
        hasExploded = true;

        // 1. 충돌한 위치에 폭발 프리팹을 네트워크로 생성
        if (explosionPrefab != null)
        {
            PhotonNetwork.Instantiate(
                explosionPrefab.name,
                transform.position,
                transform.rotation
            );
        }

        // 2. Photon으로 생성된 수류탄이므로 Photon으로 파괴
        PhotonNetwork.Destroy(gameObject);
    }
}