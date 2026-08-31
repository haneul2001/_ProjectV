using UnityEngine;

public interface IDamageable
{
    // 데미지를 받는 기능을 정의 (누가 쐈는지, 데미지는 얼마인지 등 확장 가능)
    void TakeDamage(float damage);
}