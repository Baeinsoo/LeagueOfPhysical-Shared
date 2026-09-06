using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 죽었을 때 되돌리는 절차 하나: 마지막으로 지난 선반을 찾고, 여러 명이 겹치지 않게
    /// 흩뿌리고, 텔레포트 + 속도 초기화 + 스태미나 회복 + 자세 되돌리기까지.
    ///
    /// <para>레이저와 문이 똑같은 부활을 한다 — 따로 두면 한쪽만 고쳐지는 날이 온다. 다만
    /// <b>무적 시간·부활 순번</b>처럼 호출마다 값이 바뀌는 상태는 여기 두지 않는다. 레이저와
    /// 문이 같은 무적 타이머를 공유하면, 문에 끼어 죽은 직후 레이저에 걸려도 "아직 무적"으로
    /// 처리되는 것처럼 서로 다른 위험이 한 타이머로 뒤섞인다. 그래서 무적 시간은 각 시스템이
    /// 자기 것을 따로 들고, 부활 순번(선반별로 몇 번째 부활인지)도 호출자가 들고 있다가
    /// <paramref name="spreadOrder"/>로 건넨다 — 이 함수는 그 값을 읽어 각도를 정하고 하나
    /// 늘려 돌려줄 뿐이다.</para>
    /// </summary>
    public static class SkydiveRespawn
    {
        //  같은 자리에 여러 명이 부활하면 서로 밀어낸다(캐릭터끼리는 단단한 벽이다).
        private const float RespawnSpreadRadius = 2f;
        private const int RespawnSpreadCount = 6;

        public static void To(GameFramework.World.Entity diver, float deathY, SkydiveConfig config,
                              IReadOnlyList<float> shelfYs, float spawnY,
                              IReadOnlyDictionary<float, Vector3> respawnPoints,
                              ref int spreadOrder)
        {
            float shelfY = SkydiveCheckpoints.LastPassedShelfY(deathY, shelfYs, spawnY);

            Vector3 basePoint = respawnPoints.TryGetValue(shelfY, out Vector3 point)
                ? point
                : new Vector3(0f, shelfY, 0f);

            float angle = spreadOrder % RespawnSpreadCount * (2f * Mathf.PI / RespawnSpreadCount);
            spreadOrder++;
            var spread = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * RespawnSpreadRadius;

            GameFramework.World.EntityMotionExtensions.Teleport(diver, basePoint + spread);
            GameFramework.World.EntityMotionExtensions.SetVelocity(diver, Vector3.zero);

            var stamina = diver.Get<Stamina>();
            if (stamina != null)
            {
                stamina.Current = config.StaminaMax;
                stamina.EmergencyUsed = false;
                stamina.EmergencyRemaining = 0f;
            }

            //  펴진 채로 부활하면 조작이 끊긴 것처럼 보인다. 대자(Axis 0)로 되돌린다.
            var posture = diver.Get<Posture>();
            if (posture != null)
            {
                posture.Gliding = false;
                posture.Axis = 0f;
            }

            Debug.Log($"[Respawn] {diver.Id} 부활 — 죽은 고도 {deathY:F0} → 선반 {shelfY:F0}");
        }
    }
}
