namespace LOP
{
    /// <summary>
    /// 접지해 있고 발이 발판 위면 그 발판을 저장한다. 클·서가 같은 코드로 돈다 — 서버 값이 부활을 정하고,
    /// 클라는 자기 예측 캐릭터에 돌려 표시만 한다. 공중에서 위를 지나가기만 하면 저장하지 않는다(내려앉는 것이 비용이다).
    /// </summary>
    public static class SkydiveSaveSystem
    {
        public static void Tick(GameFramework.World.Entity diver, SavePadField pads)
        {
            var save = diver.Get<SkydiveSave>();
            if (save == null || pads == null || pads.Count == 0)
            {
                return;
            }
            bool grounded = diver.Get<GameFramework.World.GroundState>()?.IsGrounded ?? false;
            if (grounded == false)
            {
                return;
            }
            if (pads.TryFind(GameFramework.World.EntityMotionExtensions.GetPosition(diver), out int id))
            {
                save.PadId = id;
            }
        }
    }
}
