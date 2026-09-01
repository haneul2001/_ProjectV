using UnityEngine;
using UnityEngine.Events;

// 플레이어 체력.
// 총알은 IDamageable 을 찾아서 때리므로 콜라이더가 붙어 있는 오브젝트에 함께 두어야 한다.
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("체력")]
    public float maxHp = 100f;

    [SerializeField]
    [Tooltip("시작할 때 maxHp 로 채워진다")]
    private float currentHp = 100f;

    [Header("죽었을 때")]
    public UnityEvent onDied;

    public float CurrentHp { get { return currentHp; } }
    public bool IsDead { get { return currentHp <= 0f; } }

    // 체력 바에 쓰는 0~1 비율
    public float Normalized
    {
        get { return maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f; }
    }

    void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHp = Mathf.Max(0f, currentHp - damage);
        Debug.Log($"[PlayerHealth] 피격 {damage} → 남은 HP {currentHp}");

        if (IsDead)
        {
            Debug.Log("[PlayerHealth] 사망");
            onDied.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHp = Mathf.Min(maxHp, currentHp + amount);
    }

    public void ResetHealth()
    {
        currentHp = maxHp;
    }
}
