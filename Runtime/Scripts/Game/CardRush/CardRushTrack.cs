using System.Collections.Generic;
using GameFramework.Rng;

namespace LOP
{
    /// <summary>장애물 종류. 와이어·코스 계약 값이라 번호를 바꾸지 않는다.</summary>
    public enum CardRushObstacleKind { Barricade = 0, Roller = 1, Pit = 2 }

    /// <summary>
    /// 코스에 깔린 장애물 하나. Variant는 그림만(바리케이드 생김새 / 통·닭·수레).
    /// 구덩이는 [AtMm, AtMm + WidthMm]가 빈 바닥이다. 나머지는 WidthMm 0.
    /// </summary>
    public readonly struct CardRushObstacle
    {
        public readonly CardRushObstacleKind Kind;
        public readonly int AtMm;
        public readonly int Variant;
        public readonly int WidthMm;

        public CardRushObstacle(CardRushObstacleKind kind, int atMm, int variant, int widthMm = 0)
        {
            Kind = kind;
            AtMm = atMm;
            Variant = variant;
            WidthMm = widthMm;
        }

        /// <summary>이 장애물이 끝나는 자리 — 구덩이는 먼 가장자리, 나머지는 그 자리.</summary>
        public int EndMm => AtMm + WidthMm;
    }

    /// <summary>구역 하나 — TriggerMm에 닿으면 [FromMm, ToMm]에 빨간 바닥이 깔리고 WarnTicks 뒤 터진다.</summary>
    public readonly struct CardRushZone
    {
        public readonly int TriggerMm;
        public readonly int FromMm;
        public readonly int ToMm;
        public readonly int WarnTicks;

        public CardRushZone(int triggerMm, int fromMm, int toMm, int warnTicks)
        {
            TriggerMm = triggerMm;
            FromMm = fromMm;
            ToMm = toMm;
            WarnTicks = warnTicks;
        }
    }

    /// <summary>
    /// 한 판의 코스 — 장애물 줄·구역 줄·(레인, 쓴 횟수)마다 첫 장. 매치 씨앗과 튜닝에서 결정론으로 나오고 모든 레인이 같은 줄을 쓴다(4차 spec §3.5).
    /// 뽑는 순서가 계약이다: 바꾸면 같은 씨앗에서 옛 클라와 다른 줄이 나와 끝없이 보정된다.
    /// </summary>
    public sealed class CardRushTrack
    {
        public static readonly ulong ObstacleSalt = Hashing.Fnv1a64("card-rush-obstacles");
        public static readonly ulong FirstCardSalt = Hashing.Fnv1a64("card-rush-first-card");
        private static readonly ulong FingerprintSalt = Hashing.Fnv1a64("card-rush-track");

        private readonly CardRushObstacle[] obstacles;
        private readonly CardRushZone[] zones;
        private readonly ulong seed;

        public int TrackMm { get; }
        public IReadOnlyList<CardRushObstacle> Obstacles => obstacles;
        public IReadOnlyList<CardRushZone> Zones => zones;
        /// <summary>
        /// 장애물·구역 줄 전체 + 첫 장 표본 + 판정이 쓰는 튜닝 값 전부의 해시 — 클·서가 로그로 찍어 마스터데이터나 Shared가 갈렸는지 본다.
        /// 벌(기다림·밀림·기절)은 줄을 안 바꾸지만 갈리면 끝없이 보정하므로 함께 섞는다(최종 리뷰 m4).
        /// </summary>
        public ulong Fingerprint { get; }

        public CardRushTrack(ulong matchSeed, in CardRushTuning tuning)
        {
            seed = matchSeed;
            TrackMm = tuning.TrackMm;
            obstacles = BuildObstacles(matchSeed, tuning);
            //  4.2차: 굴러오는 것·빨간 바닥은 뺐다(노랑 방패 시절 장애물) — 바리케이드·구덩이만 나온다. 판정 코드는 시험용 코스를 위해 남는다.
            zones = new CardRushZone[0];
            Fingerprint = ComputeFingerprint(obstacles, zones, this, tuning);
        }

        private CardRushTrack(int trackMm, CardRushObstacle[] obstacles, CardRushZone[] zones, ulong matchSeed)
        {
            seed = matchSeed;
            TrackMm = trackMm;
            this.obstacles = obstacles;
            this.zones = zones;
            Fingerprint = ComputeFingerprint(obstacles, zones, this, default);
        }

        /// <summary>시험이 장애물·구역을 손으로 놓는다. 첫 장은 씨앗에서 그대로 나온다.</summary>
        public static CardRushTrack ForTests(int trackMm, CardRushObstacle[] obstacles, CardRushZone[] zones, ulong matchSeed)
            => new CardRushTrack(trackMm, (CardRushObstacle[])obstacles.Clone(), (CardRushZone[])zones.Clone(), matchSeed);

        /// <summary>(레인, 쓴 횟수)마다 따로 — 앞사람 카드로 내 다음 첫 장을 읽을 수 없다.</summary>
        public CardColor FirstCard(int lane, int useCount) => CardOf(lane, useCount, 0);

        /// <summary>
        /// 돌기 시작해 turn장째 넘긴 카드 — 장마다 무작위지만 바로 앞 장과 같은 색은 안 나온다(같은 색이 이어지면 넘어간 줄 모른다).
        /// (레인, 쓴 횟수)마다 한 줄을 처음부터 뽑아 내려간다. 자동 잡기(RollMaxTicks)가 있어 한 줄은 열몇 장을 넘지 않는다.
        /// </summary>
        public CardColor CardOf(int lane, int useCount, long turn)
        {
            var rng = new DeterministicRandom(
                Hashing.Combine(Hashing.Combine(Hashing.Combine(seed, FirstCardSalt), (ulong)lane), (ulong)useCount));
            int color = rng.Range(0, CardColors.Count);
            for (long i = 0; i < turn; i++)
            {
                color = (color + 1 + rng.Range(0, CardColors.Count - 1)) % CardColors.Count;
            }
            return (CardColor)color;
        }

        private static CardRushObstacle[] BuildObstacles(ulong matchSeed, in CardRushTuning t)
        {
            var list = new List<CardRushObstacle>();
            var rng = new DeterministicRandom(Hashing.Combine(matchSeed, ObstacleSalt));
            int at = CardRushRules.FirstObstacleMm;
            while (at < t.TrackMm - CardRushRules.FinishClearMm)
            {
                //  바리케이드 : 구덩이 = 표의 두 몫의 비. 굴러오는 것 몫(나머지)은 4.2차에 쓰지 않는다.
                int r = rng.Range(0, t.BarricadePermille + t.PitPermille);
                var kind = r < t.BarricadePermille ? CardRushObstacleKind.Barricade : CardRushObstacleKind.Pit;
                int variant = rng.Range(0, 3);
                int width = kind == CardRushObstacleKind.Pit ? rng.Range(CardRushRules.PitWidthMinMm, CardRushRules.PitWidthMaxMm + 1) : 0;
                list.Add(new CardRushObstacle(kind, at, variant, width));
                at += rng.Range(t.ObstacleGapMinMm, t.ObstacleGapMaxMm + 1);
            }
            return list.ToArray();
        }

        //  첫 장 표본 크기 — 모든 레인 × 쓴 횟수 앞 몇 번. 첫 장 식(솔트·Combine 순서)이 클·서에서 다르면 지문이 갈린다.
        private const int FingerprintFirstCardUses = 4;

        private static ulong ComputeFingerprint(CardRushObstacle[] obstacles, CardRushZone[] zones, CardRushTrack track, in CardRushTuning t)
        {
            ulong h = FingerprintSalt;
            //  시뮬이 읽는 튜닝 칸 전부 — 새 칸을 CardRushTuning에 더하면 여기도 더한다.
            h = Hashing.Combine(h, (ulong)t.TrackMm);
            h = Hashing.Combine(h, (ulong)t.ObstacleGapMinMm);
            h = Hashing.Combine(h, (ulong)t.ObstacleGapMaxMm);
            h = Hashing.Combine(h, (ulong)t.BarricadePermille);
            h = Hashing.Combine(h, (ulong)t.ZoneGapMinMm);
            h = Hashing.Combine(h, (ulong)t.ZoneGapMaxMm);
            h = Hashing.Combine(h, (ulong)t.BarricadeWaitTicks);
            h = Hashing.Combine(h, (ulong)t.RollKnockMm);
            h = Hashing.Combine(h, (ulong)t.RollStunTicks);
            h = Hashing.Combine(h, (ulong)t.ZoneKnockMm);
            h = Hashing.Combine(h, (ulong)t.ZoneStunTicks);
            h = Hashing.Combine(h, (ulong)t.PitPermille);
            h = Hashing.Combine(h, (ulong)t.PitStunTicks);
            for (int i = 0; i < obstacles.Length; i++)
            {
                h = Hashing.Combine(h, (ulong)(int)obstacles[i].Kind);
                h = Hashing.Combine(h, (ulong)obstacles[i].AtMm);
                h = Hashing.Combine(h, (ulong)obstacles[i].Variant);
                h = Hashing.Combine(h, (ulong)obstacles[i].WidthMm);
            }
            for (int i = 0; i < zones.Length; i++)
            {
                h = Hashing.Combine(h, (ulong)zones[i].TriggerMm);
                h = Hashing.Combine(h, (ulong)zones[i].FromMm);
                h = Hashing.Combine(h, (ulong)zones[i].ToMm);
                h = Hashing.Combine(h, (ulong)zones[i].WarnTicks);
            }
            for (int lane = 0; lane < CardRushLanes.MaxLanes; lane++)
                for (int n = 0; n < FingerprintFirstCardUses; n++)
                    h = Hashing.Combine(h, (ulong)(int)track.FirstCard(lane, n));
            return h;
        }
    }
}
