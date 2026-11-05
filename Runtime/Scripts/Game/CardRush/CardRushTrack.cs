using System.Collections.Generic;
using GameFramework.Rng;

namespace LOP
{
    public enum CardRushObstacleKind { Barricade = 0, Roller = 1 }

    /// <summary>코스에 깔린 장애물 하나. Variant는 그림만(바리케이드 생김새 / 통·닭·수레).</summary>
    public readonly struct CardRushObstacle
    {
        public readonly CardRushObstacleKind Kind;
        public readonly int AtMm;
        public readonly int Variant;

        public CardRushObstacle(CardRushObstacleKind kind, int atMm, int variant)
        {
            Kind = kind;
            AtMm = atMm;
            Variant = variant;
        }
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
    /// 한 판의 코스 — 장애물 줄·구역 줄·(레인, 쓴 횟수)마다 첫 장. 매치 씨앗과 튜닝에서 결정론으로 나오고 모든 레인이 같은 줄을 쓴다(spec §3.4).
    /// 뽑는 순서가 계약이다: 바꾸면 같은 씨앗에서 옛 클라와 다른 줄이 나와 끝없이 보정된다.
    /// </summary>
    public sealed class CardRushTrack
    {
        public static readonly ulong ObstacleSalt = Hashing.Fnv1a64("card-rush-obstacles");
        public static readonly ulong ZoneSalt = Hashing.Fnv1a64("card-rush-zones");
        public static readonly ulong FirstCardSalt = Hashing.Fnv1a64("card-rush-first-card");
        private static readonly ulong FingerprintSalt = Hashing.Fnv1a64("card-rush-track");

        //  구역을 바리케이드 뒤로 미는 횟수 — 그래도 겹치면 구역 줄을 거기서 끝낸다(난수를 더 뽑지 않는다).
        private const int ZonePushTries = 8;
        private const int ZonePushPastBarricadeMm = 1000;

        private readonly CardRushObstacle[] obstacles;
        private readonly CardRushZone[] zones;
        private readonly ulong seed;

        public int TrackMm { get; }
        public IReadOnlyList<CardRushObstacle> Obstacles => obstacles;
        public IReadOnlyList<CardRushZone> Zones => zones;
        /// <summary>장애물·구역 줄 전체 + 첫 장 표본의 해시 — 클·서가 로그로 찍어 마스터데이터나 Shared가 갈렸는지 본다.</summary>
        public ulong Fingerprint { get; }

        public CardRushTrack(ulong matchSeed, in CardRushTuning tuning)
        {
            seed = matchSeed;
            TrackMm = tuning.TrackMm;
            obstacles = BuildObstacles(matchSeed, tuning);
            zones = BuildZones(matchSeed, tuning, obstacles);
            Fingerprint = ComputeFingerprint(obstacles, zones, this);
        }

        private CardRushTrack(int trackMm, CardRushObstacle[] obstacles, CardRushZone[] zones, ulong matchSeed)
        {
            seed = matchSeed;
            TrackMm = trackMm;
            this.obstacles = obstacles;
            this.zones = zones;
            Fingerprint = ComputeFingerprint(obstacles, zones, this);
        }

        /// <summary>시험이 장애물·구역을 손으로 놓는다. 첫 장은 씨앗에서 그대로 나온다.</summary>
        public static CardRushTrack ForTests(int trackMm, CardRushObstacle[] obstacles, CardRushZone[] zones, ulong matchSeed)
            => new CardRushTrack(trackMm, (CardRushObstacle[])obstacles.Clone(), (CardRushZone[])zones.Clone(), matchSeed);

        /// <summary>(레인, 쓴 횟수)마다 따로 — 앞사람 카드로 내 다음 첫 장을 읽을 수 없다.</summary>
        public CardColor FirstCard(int lane, int useCount)
        {
            var rng = new DeterministicRandom(
                Hashing.Combine(Hashing.Combine(Hashing.Combine(seed, FirstCardSalt), (ulong)lane), (ulong)useCount));
            return (CardColor)rng.Range(0, CardColors.Count);
        }

        private static CardRushObstacle[] BuildObstacles(ulong matchSeed, in CardRushTuning t)
        {
            var list = new List<CardRushObstacle>();
            var rng = new DeterministicRandom(Hashing.Combine(matchSeed, ObstacleSalt));
            int at = CardRushRules.FirstObstacleMm;
            while (at < t.TrackMm - CardRushRules.FinishClearMm)
            {
                var kind = rng.Range(0, 1000) < t.BarricadePermille ? CardRushObstacleKind.Barricade : CardRushObstacleKind.Roller;
                int variant = rng.Range(0, 3);
                list.Add(new CardRushObstacle(kind, at, variant));
                at += rng.Range(t.ObstacleGapMinMm, t.ObstacleGapMaxMm + 1);
            }
            return list.ToArray();
        }

        private static CardRushZone[] BuildZones(ulong matchSeed, in CardRushTuning t, CardRushObstacle[] obstacles)
        {
            var list = new List<CardRushZone>();
            var rng = new DeterministicRandom(Hashing.Combine(matchSeed, ZoneSalt));
            long ramp = (long)t.TrackMm * 3 / 4;
            int at = CardRushRules.FirstZoneMm;
            while (true)
            {
                int ahead = rng.Range(CardRushRules.ZoneAheadMinMm, CardRushRules.ZoneAheadMaxMm + 1);
                int len = rng.Range(CardRushRules.ZoneLengthMinMm, CardRushRules.ZoneLengthMaxMm + 1);

                //  바리케이드 앞에 갇힌 채 바닥이 터지면 답이 노랑 하나뿐이다 — 그 바리케이드 뒤로 민다.
                int from = 0, to = 0;
                bool clear = false;
                for (int tries = 0; tries < ZonePushTries; tries++)
                {
                    from = at + ahead;
                    to = from + len;
                    int farthest = FarthestBarricadeStoppingIn(obstacles, from - CardRushRules.ZoneMarginMm, to + CardRushRules.ZoneMarginMm);
                    if (farthest < 0) { clear = true; break; }
                    at = farthest + ZonePushPastBarricadeMm;
                }
                if (!clear || to >= t.TrackMm - CardRushRules.FinishClearMm) break;

                //  경고는 코스 앞 3/4에 걸쳐 75 → 45틱 — 같은 자리면 누구에게나 같은 경고(시간으로 줄이면 뒤처진 사람이 손해).
                int warn = (int)(CardRushRules.ZoneWarnStartTicks
                                 - (long)(CardRushRules.ZoneWarnStartTicks - CardRushRules.ZoneWarnEndTicks) * System.Math.Min(at, ramp) / ramp);
                list.Add(new CardRushZone(at, from, to, warn));
                at = to + rng.Range(t.ZoneGapMinMm, t.ZoneGapMaxMm + 1);
            }
            return list.ToArray();
        }

        //  선수가 바리케이드 앞에 서는 자리(At − BarricadeStopMm)가 [lo, hi] 안인 바리케이드 중 가장 먼 것. 없으면 −1.
        private static int FarthestBarricadeStoppingIn(CardRushObstacle[] obstacles, int lo, int hi)
        {
            int farthest = -1;
            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i].Kind != CardRushObstacleKind.Barricade) continue;
                int stop = obstacles[i].AtMm - CardRushRules.BarricadeStopMm;
                if (stop >= lo && stop <= hi && obstacles[i].AtMm > farthest) farthest = obstacles[i].AtMm;
            }
            return farthest;
        }

        //  첫 장 표본 크기 — 모든 레인 × 쓴 횟수 앞 몇 번. 첫 장 식(솔트·Combine 순서)이 클·서에서 다르면 지문이 갈린다.
        private const int FingerprintFirstCardUses = 4;

        private static ulong ComputeFingerprint(CardRushObstacle[] obstacles, CardRushZone[] zones, CardRushTrack track)
        {
            ulong h = FingerprintSalt;
            for (int i = 0; i < obstacles.Length; i++)
            {
                h = Hashing.Combine(h, (ulong)(int)obstacles[i].Kind);
                h = Hashing.Combine(h, (ulong)obstacles[i].AtMm);
                h = Hashing.Combine(h, (ulong)obstacles[i].Variant);
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
