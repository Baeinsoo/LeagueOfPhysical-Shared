namespace LOP
{
    /// <summary>
    /// 고른 콜라이더를 빼고 밀어내는 밀어내기. <see cref="GameFramework.World.IMotionBridge.Depenetrate"/>는 마스크
    /// 층 전부에서 밀어내는데, 움직이는 장애물은 맵과 같은 층이라 층으로는 못 뺀다 — 그래서 콜라이더를 보고 거른다.
    /// 기절·무적인 Flappy 새가 치는 장애물(진자·풍차·낙석)에 끌려가지 않으면서 맵·문에서는 지금처럼 빠져나오게
    /// 하려고 있다(<see cref="FlappyWorld"/>). GameFramework 포트를 안 바꾸려고 공용 패키지에 따로 둔다.
    /// </summary>
    public interface IFilteredDepenetration
    {
        /// <summary>
        /// <see cref="GameFramework.World.IMotionBridge.Depenetrate"/>와 같되 <paramref name="skip"/>이 참인
        /// 콜라이더는 밀어내기에서 뺀다. 실제로 민 벡터를 돌려준다(안 밀었으면 0).
        /// </summary>
        System.Numerics.Vector3 DepenetrateExcept(GameFramework.World.Entity entity,
            System.Func<UnityEngine.Collider, bool> skip);
    }
}
