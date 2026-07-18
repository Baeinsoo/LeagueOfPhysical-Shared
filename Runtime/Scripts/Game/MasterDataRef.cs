using System;
using GameFramework.World;

namespace LOP
{
    /// <summary>이 엔티티의 설정 테이블 키(TbCharacter/TbItem code). 스폰 데이터 재구성 시 서버가 릴레이.</summary>
    public class MasterDataRef : Component
    {
        public string Code { get; }

        public MasterDataRef(string code)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }
    }
}
