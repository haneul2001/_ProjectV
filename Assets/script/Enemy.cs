using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    public float hp = 100f;

    public void TakeDamage(float damage)
    {
        hp -= damage;
        Debug.Log($"{gameObject.name} 맞음! 현재 HP: {hp}");

        if (hp <= 0)
        {
            Debug.Log("적 사망!");
            Destroy(gameObject);
        }
    }
}