namespace LOP
{
    /// <summary>
    /// 지금 어떤 자세로 떨어지고 있나. 데이터만 — 바꾸는 것은 <see cref="SkydiveWorld"/>와
    /// <see cref="StaminaSystem"/>이다.
    /// </summary>
    public class Posture : GameFramework.World.Component
    {
        /// <summary>0이면 대자(팔다리 벌림), 1이면 완전한 다이브(머리부터). 사이는 연속이다.</summary>
        public float Axis;

        /// <summary>패러세일을 펼쳤나. 자세 축과 무관한 별개 도구라 bool이다.</summary>
        public bool Gliding;
    }
}
