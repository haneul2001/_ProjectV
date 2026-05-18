using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    public GameObject shootEffectPref; 
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.visible = false; // 마우스 커서를 숨김

        Cursor.lockState = CursorLockMode.Locked; // 마우스 커서를 화면 중앙에 고정
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼이 눌렸을 때
        {
            Ray ray = Camera.main.ViewportPointToRay (new Vector2(0.5f, 0.5f));

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject shootEffect = Instantiate(shootEffectPref, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal)); // 발사 효과를 생성하고 위치와 회전을 설정

                shootEffect.transform.SetParent(hit.transform); // 발사 효과를 맞은 오브젝트의 자식으로 설정
            }

            
        }
    }
}
