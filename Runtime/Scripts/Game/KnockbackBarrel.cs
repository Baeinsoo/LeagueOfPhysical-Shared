namespace LOP
{
    /// <summary>
    /// 굴러다니는 드럼통 하나. 위치·속도는 Transform/Velocity에 있고 여기엔 "마지막으로 민 사람"만 둔다.
    /// 드럼통이 몸을 칠 때 막타 크레딧을 그 사람에게 주려는 기억이다.
    /// </summary>
    public sealed class KnockbackBarrel : GameFramework.World.Component
    {
        /// <summary>클라가 드럼통을 알아보는 표. 드럼통은 동전과 같은 생성 메시지(visual_id)로 오며, 이 값이면 드럼통이다.</summary>
        public const string VisualId = "knockback-barrel";

        public string PusherId;
    }
}
