namespace LOP
{
    /// <summary>넉백 생존전 몸이 든 총의 상태. 남은 탄·다음 충전 틱·다음 발사 가능 틱만 있다.</summary>
    public sealed class KnockbackGun : GameFramework.World.Component
    {
        public int Ammo;
        //  탄이 가득이면 충전할 게 없어 long.MaxValue
        public long NextReloadTick = long.MaxValue;
        public long CooldownEndTick;

        public KnockbackGun(int ammoMax)
        {
            Ammo = ammoMax;
        }
    }
}
