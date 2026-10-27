namespace LOP
{
    /// <summary>
    /// 연속 날갯짓 — 마지막으로 기절에 들어간 뒤 이어진, 시뮬이 받은 날갯짓 수. 데이터만 갖는다 — 세기·끊기·고비 보상은
    /// <see cref="FlappyFlapComboSystem"/>이 한다(<see cref="FlappyDash"/>·<see cref="FlappyStun"/>과 같은 짝).
    /// <para>서버가 권위다. 스냅(<c>EntitySnap.flap_combo</c>)이 내 새 예측을 맞추고, 되감기 사진(<see cref="FlappySavedState"/>)에도 든다.</para>
    /// </summary>
    public class FlappyFlapCombo : GameFramework.World.Component
    {
        public int Count;
    }
}
