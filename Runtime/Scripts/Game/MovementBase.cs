namespace LOP
{
    /// <summary>
    /// 발밑 땅이 움직이는 속도(초당 m, 세계 기준). 도는 판 위면 그 자리의 판 속도, 아니면 0.
    /// <para>몸의 속도(<see cref="GameFramework.World.Velocity"/>)는 세계 기준이라, "땅에 대해 얼마나 움직이나"(달리기 애니 등)는
    /// 몸 속도에서 이 값을 빼서 본다. 언리얼 CharacterMovement의 movement base 속도, KCC의 AttachedRigidbodyVelocity에 해당한다.</para>
    /// 월드가 매 틱 끝에 다시 쓴다(저장·복원하지 않는다 — 판 자세가 틱의 식이라 다음 틱에 같은 값이 다시 나온다).
    /// </summary>
    public class MovementBase : GameFramework.World.Component
    {
        public System.Numerics.Vector3 Velocity;
    }
}
