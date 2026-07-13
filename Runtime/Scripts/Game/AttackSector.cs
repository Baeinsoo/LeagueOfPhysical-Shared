namespace LOP
{
    /// <summary>시전자 정면 부채꼴(전체 각 <paramref name="angle"/>도) 안이고 <paramref name="range"/> 이내인지 판정.
    /// World.Transform(진실원본, System.Numerics) 기준 — 엔진 좌표 무관, 순수·EditMode 테스트. Damage/Knockback 공유.</summary>
    public static class AttackSector
    {
        public static bool Contains(GameFramework.World.Transform caster,
                                    System.Numerics.Vector3 targetPosition, float range, float angle)
        {
            System.Numerics.Vector3 toTarget = targetPosition - caster.Position;
            if (toTarget.Length() > range)
            {
                return false;
            }

            System.Numerics.Vector3 forward =
                System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, caster.Rotation);
            float dot = System.Numerics.Vector3.Dot(
                System.Numerics.Vector3.Normalize(forward),
                System.Numerics.Vector3.Normalize(toTarget));
            float targetAngle = (float)System.Math.Acos(System.Math.Clamp(dot, -1.0, 1.0)) * (180f / (float)System.Math.PI);
            return targetAngle <= (angle * 0.5f);
        }
    }
}
