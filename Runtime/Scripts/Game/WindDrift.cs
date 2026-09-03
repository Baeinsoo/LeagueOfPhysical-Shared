namespace LOP
{
    /// <summary>
    /// 이 몸이 <b>지금까지 실린</b> 바람. 볼륨에 들어가면 그 바람으로 자라고, 나오면 0으로
    /// 돌아간다 — 걸리는 시간이 자세마다 다르다(<see cref="WindDriftSystem"/>).
    ///
    /// <para>이 지연이 있어서 볼륨 경계를 칼같이 잘라도 된다. 들락날락이 저절로 부드러워지므로
    /// 경계를 흐리게 만드는 코드가 따로 필요 없다.</para>
    ///
    /// 데이터만 — 바꾸는 것은 <see cref="WindDriftSystem"/>이다.
    /// </summary>
    public class WindDrift : GameFramework.World.Component
    {
        public System.Numerics.Vector3 Value;

        /// <summary>
        /// 마지막으로 0이 아니었던 목표 바람. 볼륨을 나가는 순간 목표는 0이 되어 그 크기
        /// 정보가 사라지는데, 들어갈 때와 같은 시간으로 되돌아오려면 그 크기가 있어야 한다.
        /// 그래서 목표가 0이 아닐 때마다 여기 남겨 둔다(<see cref="WindDriftSystem"/>).
        /// </summary>
        public System.Numerics.Vector3 Anchor;
    }
}
