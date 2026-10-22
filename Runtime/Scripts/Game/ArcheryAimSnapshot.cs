using System.Collections.Generic;

namespace LOP
{
    /// <summary>되감기용 조준 상태 저장/복원. 화살 목록(<see cref="ArcheryShotBook"/>)과 짝으로 쓴다.</summary>
    public static class ArcheryAimSnapshot
    {
        public static Dictionary<string, ArcheryAim> Capture(GameFramework.World.EntityRegistry registry)
        {
            var aims = new Dictionary<string, ArcheryAim>();
            foreach (var entity in registry.All)
            {
                // 원격(비-Simulated) 몸은 베이스가 되감지 않는 대상이다 — 여기서 같이 저장했다가
                // 되돌리면, 되감는 사이 네트워크로 도착한 남의 새 조준값을 옛 값으로 덮어쓴다.
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                var aim = entity.Get<ArcheryAim>();
                if (aim != null)
                {
                    aims[entity.Id] = new ArcheryAim
                    {
                        Yaw = aim.Yaw, Pitch = aim.Pitch,
                        Drawing = aim.Drawing, DrawStartTick = aim.DrawStartTick,
                        DrawRatio = aim.DrawRatio,
                    };
                }
            }
            return aims;
        }

        public static void Restore(GameFramework.World.EntityRegistry registry,
                                   IReadOnlyDictionary<string, ArcheryAim> saved)
        {
            foreach (var pair in saved)
            {
                var aim = registry.Get(pair.Key)?.Get<ArcheryAim>();
                if (aim == null)
                {
                    continue;
                }
                aim.Yaw = pair.Value.Yaw;
                aim.Pitch = pair.Value.Pitch;
                aim.Drawing = pair.Value.Drawing;
                aim.DrawStartTick = pair.Value.DrawStartTick;
                aim.DrawRatio = pair.Value.DrawRatio;
            }
        }
    }
}
