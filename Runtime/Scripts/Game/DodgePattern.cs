namespace LOP
{
    public enum DodgePatternKind
    {
        BulletRain = 1,
        BulletWall = 2,
        BulletAimed = 3,
        Bomb = 4,
        Laser = 5,
        Rock = 6,
        Tiles = 7,
    }

    /// <summary>
    /// 위험 하나. 값이 뜻하는 바는 <see cref="DodgeHazards"/> 머리의 표가 계약이다.
    /// 이 값과 틱만 있으면 어디서든 같은 도형이 나온다 — 네트워크로 오가는 건 이것뿐이다.
    /// </summary>
    public readonly struct DodgePattern
    {
        public readonly int Id;
        public readonly DodgePatternKind Kind;
        public readonly long StartTick;
        public readonly ulong Seed;
        public readonly float P0, P1, P2, P3;

        public DodgePattern(int id, DodgePatternKind kind, long startTick, ulong seed, float p0, float p1, float p2, float p3)
        {
            Id = id; Kind = kind; StartTick = startTick; Seed = seed; P0 = p0; P1 = p1; P2 = p2; P3 = p3;
        }
    }

    public enum DodgeShapeType { Circle, Segment, Rect }

    /// <summary>
    /// 어느 한 틱에 보이는 도형. 원은 (X0,Z0)=지금·(X1,Z1)=한 틱 전, 선분은 두 끝과 반폭(Radius),
    /// 칸은 최소·최대 모서리다. Active=false면 예고라 판정이 없고 Progress(0~1)가 차오른 정도다.
    /// </summary>
    public struct DodgeShape
    {
        public DodgeShapeType Type;
        public bool Active;
        public float Progress;
        public float X0, Z0, X1, Z1;
        public float Radius;
    }
}
