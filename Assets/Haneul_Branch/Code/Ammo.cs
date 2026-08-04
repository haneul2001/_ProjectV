using UnityEngine;
using System.Collections;

public class Ammo : MonoBehaviour
{
    [Header("탄약")]
    public int maxAmmo = 30;
    public int currentAmmo;

    [Header("재장전")]
    public float reloadTime = 1.5f;
    public KeyCode reloadKey = KeyCode.R;
    private bool isReloading = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentAmmo = maxAmmo;     
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(reloadKey))
        {
            TryReload();
        }
    }

    public void TryReload(){
        if(!isReloading && currentAmmo < maxAmmo)
        {
            StartCoroutine(Reload());
        }
    }
    public bool HasAmmo(){
        return currentAmmo > 0 && !isReloading;
    }

    public bool Use(){
        if(!HasAmmo()) return false;
        currentAmmo--;
        return true;
    }
    IEnumerator Reload(){
        isReloading = true;
        Debug.Log("재장전...");
        //오디오 소스 추가 ...

        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("재장전 완료");
        //오디오 소스 추가 ...
    }
}
