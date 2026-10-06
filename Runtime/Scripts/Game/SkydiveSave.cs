namespace LOP
{
    /// <summary>이 사람이 마지막으로 내려앉은 세이브 발판. 죽으면 여기서 되살아난다(발판 맵만).</summary>
    public class SkydiveSave : GameFramework.World.Component
    {
        public const int None = -1;
        public int PadId = None;
    }
}
