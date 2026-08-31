using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
   public enum DisplayType{Current,Max} //현재, 최대
   [Header("무엇을 표시할 지")]
   public DisplayType displayType = DisplayType.Current;

   [Header("연결 (비워두면 자동으로 찾음)")]
   public Ammo ammo;
   private TMP_Text text;

    void Start()
    {
        text = GetComponent<TMP_Text>();
        ammo = FindAnyObjectByType<Ammo>();
    }

    // Update is called once per frame
    void Update()
    {
        if(ammo == null || text == null) return;
       if (displayType == DisplayType.Current)
            text.text = ammo.currentAmmo.ToString();
        else
            text.text = ammo.maxAmmo.ToString();
    }
}
