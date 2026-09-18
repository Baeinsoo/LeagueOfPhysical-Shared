using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 입력을 읽어 조준 상태를 갱신하고, 손을 뗀 틱에 화살 한 발을 만든다.
    /// 무상태다 — 상태는 <see cref="ArcheryAim"/>에 있다.
    /// </summary>
    public class ArcheryAimSystem
    {
        private readonly ArcheryConfig config;

        public ArcheryAimSystem(ArcheryConfig config)
        {
            this.config = config;
        }

        /// <summary>살짝 당겼을 때의 화살 속도(m/s). 느리고 크게 휜다.</summary>
        public const float MinSpeed = 25f;

        /// <summary>끝까지 당겼을 때의 화살 속도(m/s). 빠르고 곧게 간다.</summary>
        public const float MaxSpeed = 65f;

        /// <summary>이만큼 당기면 최대다. 더 당겨도 세지지 않는다.</summary>
        public const float FullDrawSeconds = 0.8f;

        /// <summary>
        /// 이만큼은 끌어야 시위가 걸린다(0~1). 못 미치면 쏘지 않고 취소다 — 화면을 스치기만 해도
        /// 화살이 나가면 조준하다 실수로 쏘게 된다.
        /// </summary>
        public const float DrawThreshold = 0.15f;

        /// <summary>
        /// 시위가 당겨지는 최대 속도(초당 당김 비율). 완전히 당기는 데 최소 0.1초가 걸린다.
        ///
        /// <para>사람이 엄지로 끄는 속도(보통 0.15~0.25초)보다 빠르게 잡았다 — 여기서 손을 막으면
        /// 끝까지 끌었는데 안 나가는 일이 생긴다. 아주 빠르게 튕기는 입력만 깎아 낸다.</para>
        /// </summary>
        public const float DrawRisePerSecond = 10f;

        /// <summary>
        /// 손을 뗀 뒤 시위가 풀리는 속도(초당 당김 비율). 0까지 1/3초쯤 걸린다.
        ///
        /// <para>당길 때보다 <b>느린 것이 핵심</b>이다. 쏘는 순간 당김을 0으로 떨어뜨리면 화각이
        /// 한 프레임에 벌어져 화면이 튄다. 천천히 풀리게 두면 화면(줌·조준선·게이지)이 각자
        /// 완충을 대지 않고 이 값을 그냥 읽어도 부드럽다 — <b>규칙이 시뮬에 있다</b>.</para>
        /// </summary>
        public const float DrawFallPerSecond = 3f;

        /// <summary>화살이 떠나는 높이 — 발밑이 아니라 눈높이에서 나가야 겨눈 대로 간다.</summary>
        public const float EyeHeight = 1.4f;

        /// <summary>이만큼 당기고 있었다(초). 화면은 정수 틱 사이도 물어보므로 소수 틱을 받는다.</summary>
        public static float HeldSeconds(long drawStartTick, double currentTick, float tickInterval)
        {
            return (float)((currentTick - drawStartTick) * tickInterval);
        }

        /// <summary>당긴 정도 0~1. 오래 당겨도 1을 넘지 않는다.</summary>
        public static float DrawRatio(long drawStartTick, long currentTick, float tickInterval)
        {
            return Mathf.Clamp01(HeldSeconds(drawStartTick, currentTick, tickInterval) / FullDrawSeconds);
        }

        public static float SpeedFor(float drawRatio)
        {
            return Mathf.Lerp(MinSpeed, MaxSpeed, Mathf.Clamp01(drawRatio));
        }

        /// <summary>
        /// 조준 각도(yaw/pitch)에 이 시점(<paramref name="heldSeconds"/>, <paramref name="drawRatio"/>)의
        /// 손떨림을 얹어 최종 발사 방향(단위 벡터)을 만든다.
        ///
        /// <para><b>흔들림을 조준각에 얹는 곳은 여기 하나여야 한다.</b> 한때 조준 가이드선이
        /// 같은 산수를 제 쪽에서 또 했고, 그때 얻은 교훈이 이 함수다 — 두 곳이 각자 더하면
        /// 지금 우연히 같은 값이 나와도 한쪽만 고치는 순간 조용히 갈라진다. 화면은 멀쩡해
        /// 보이면서 조준만 거짓말을 한다. (가이드선은 2026-09-19에 제거됐다. 화면이 착탄점을
        /// 그려 주면 거리별 낙차를 익힐 일이 없어 조준 실력이 성립하지 않아서다.)</para>
        /// </summary>
        public static Vector3 DirectionFor(float yawDegrees, float pitchDegrees,
                                           float heldSeconds, float drawRatio, int phaseSeed, ArcheryConfig config)
        {
            Vector2 sway = ArcheryShake.Offset(heldSeconds, drawRatio, phaseSeed, config);
            return ArcheryTrajectory.DirectionFrom(yawDegrees + sway.x, pitchDegrees + sway.y);
        }

        /// <summary>떼는 틱에만 화살을 돌려준다. 나머지 틱은 null이다.</summary>
        public ArcheryShot? Tick(GameFramework.World.Entity entity, long tick, float tickInterval)
        {
            var aim = entity.Get<ArcheryAim>();
            var command = entity.Get<InputBuffer>()?.Current;
            if (aim == null || command == null)
            {
                return null;
            }

            aim.Yaw = command.AimYaw;
            aim.Pitch = command.AimPitch;
            //  쏘는 힘은 시위가 풀리기 **전** 값이다 — 떼는 틱에도 아래에서 한 틱분이 깎이므로
            //  여기서 먼저 붙들어 둔다. 안 그러면 놓을 때마다 힘이 조금씩 모자란다.
            float drawAtRelease = aim.DrawRatio;

            //  시위는 정해진 속도로만 움직인다 — 손가락이 순간이동해도 활은 못 그런다.
            //  당길 때는 손가락 위치를 향해 빠르게, 뗀 뒤에는 0을 향해 느리게 간다.
            float drawTarget = command.Drawing ? command.DrawRatio : 0f;
            float drawSpeed = drawTarget > aim.DrawRatio ? DrawRisePerSecond : DrawFallPerSecond;
            aim.DrawRatio = Mathf.MoveTowards(
                aim.DrawRatio, drawTarget, drawSpeed * tickInterval);

            // 당기기 시작한 틱은 "안 당기다가 당기기 시작한" 그 틱에만 새로 찍는다.
            if (command.Drawing && aim.Drawing == false)
            {
                aim.DrawStartTick = tick;
            }

            if (command.Release == false)
            {
                aim.Drawing = command.Drawing;
                return null;
            }

            // 당긴 적 없이 뗀 것은 발사가 아니다(손가락이 스친 경우).
            if (aim.Drawing == false)
            {
                return null;
            }

            //  임계치를 못 넘고 뗐으면 취소다. 시위가 걸린 적이 없으니 화살도 없다.
            if (drawAtRelease < DrawThreshold)
            {
                aim.Drawing = false;
                return null;   // 시위는 위에서 같은 속도로 0까지 풀린다
            }

            //  화살이 없으면 시위를 걸었어도 안 나간다. 그릇이 없으면 무제한이다(원형 맵).
            var quiver = entity.Get<ArcheryQuiver>();
            if (quiver != null && quiver.Remaining <= 0)
            {
                aim.Drawing = false;
                return null;
            }

            float speed = SpeedFor(drawAtRelease);
            Vector3 origin = entity.Get<GameFramework.World.Transform>().Position.ToUnity()
                           + new Vector3(0f, EyeHeight, 0f);

            //  오래 당기고 있을수록, 많이 당길수록 손이 떨려 조준이 흔들린다 — 그 대가가 이
            //  발의 방향에 실제로 실려야 "오래·세게 버티면 위험하다"가 된다. drawAtRelease를
            //  쓰는 이유는 위 speed 계산과 같다 — 시위가 풀리기 시작하기 전, 쏘는 그 순간의
            //  당김이어야 한다. 위상은 쏜 사람마다 달라야 두 사수가 똑같이 흔들리지 않는다.
            //  클라(예측)와 서버(권위) 둘 다 이 Tick을 불러 각자 계산하므로 — 와이어로 값을
            //  주고받지 않으므로 — 같은 입력을 넣으면 같은 값이 나와야 한다(그 보장의 근거는
            //  ArcheryShake.PhaseSeedOf의 주석 참고).
            float heldSeconds = HeldSeconds(aim.DrawStartTick, tick, tickInterval);
            int phaseSeed = ArcheryShake.PhaseSeedOf(entity.Id);
            Vector3 velocity = DirectionFor(aim.Yaw, aim.Pitch, heldSeconds, drawAtRelease, phaseSeed, config) * speed;

            if (quiver != null)
            {
                quiver.Remaining--;
            }

            //  화살은 지금 나가지만 시위는 제자리로 돌아오는 데 시간이 걸린다 — 0으로 떨어뜨리지
            //  않는다. 그래야 줌이 한 프레임에 튀지 않는다.
            aim.Drawing = false;
            return new ArcheryShot(entity.Id, tick, origin, velocity);
        }
    }
}
