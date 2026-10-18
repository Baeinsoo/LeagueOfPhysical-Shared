using System;

namespace LOP
{
    /// <summary>
    /// 캐릭터 외형(피부·머리) — 마스터데이터 TbAppearanceColor의 번호. 계정 데이터라 판이 바뀌어도 같다.
    /// 0은 "모름"(옛 서버·로비 실패)이고, 그리는 쪽이 기본값으로 바꾼다.
    /// </summary>
    public readonly struct CharacterAppearance : IEquatable<CharacterAppearance>
    {
        public int SkinColorId { get; }
        public int HairColorId { get; }

        public CharacterAppearance(int skinColorId, int hairColorId)
        {
            SkinColorId = skinColorId;
            HairColorId = hairColorId;
        }

        public bool Equals(CharacterAppearance other) => SkinColorId == other.SkinColorId && HairColorId == other.HairColorId;
        public override bool Equals(object obj) => obj is CharacterAppearance other && Equals(other);
        public override int GetHashCode() => (SkinColorId * 397) ^ HairColorId;
        public static bool operator ==(CharacterAppearance a, CharacterAppearance b) => a.Equals(b);
        public static bool operator !=(CharacterAppearance a, CharacterAppearance b) => !a.Equals(b);
        public override string ToString() => $"(skin {SkinColorId}, hair {HairColorId})";
    }
}
