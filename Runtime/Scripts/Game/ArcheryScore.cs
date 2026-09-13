namespace LOP
{
    /// <summary>
    /// 이 판에서 모은 점수(데이터만). <b>적립은 서버만</b> 한다 — 클라는 스냅샷으로 받은 값을
    /// 덮어쓸 뿐이다(적중을 예측하지 않기로 했으므로, 클라가 스스로 올릴 일이 없다).
    /// </summary>
    public class ArcheryScore : GameFramework.World.Component
    {
        /// <summary>과녁을 맞혀 얻은 합.</summary>
        public int Gained;

        /// <summary>함정을 맞혀 깎인 합.</summary>
        public int Lost;

        /// <summary>최종 점수. 파생값이라 둘과 어긋날 수 없다.</summary>
        public int Value => Gained - Lost;
    }
}
