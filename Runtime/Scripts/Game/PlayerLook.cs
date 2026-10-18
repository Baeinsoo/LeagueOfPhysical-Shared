using System.Collections.Generic;
using GameFramework.World;

namespace LOP
{
    /// <summary>서버가 방을 열 때 로비에서 받은 "그 판의 룩"(슬롯 코드 → 품목 코드)과 이름·계정 레벨·캐릭터 외형. 생성 데이터로 모든 클라에 간다.</summary>
    public sealed class PlayerLook : Component
    {
        public IReadOnlyDictionary<string, string> Slots { get; }
        public string DisplayName { get; }
        public int AccountLevel { get; }
        public CharacterAppearance CharacterAppearance { get; }

        public PlayerLook(IReadOnlyDictionary<string, string> slots, string displayName, int accountLevel, CharacterAppearance characterAppearance = default)
        {
            Slots = slots != null ? new Dictionary<string, string>(slots) : new Dictionary<string, string>();
            DisplayName = displayName ?? string.Empty;
            AccountLevel = accountLevel < 1 ? 1 : accountLevel;
            CharacterAppearance = characterAppearance;
        }

        public string SlotOrNull(string slotCode)
        {
            if (slotCode == null)
            {
                return null;
            }

            return Slots.TryGetValue(slotCode, out var itemCode) ? itemCode : null;
        }
    }
}
