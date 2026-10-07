using UnityEngine;

namespace LOP
{
    /// <summary>높이 한 점의 분위기 — 안개·주변광·햇빛·블룸·노출.</summary>
    [System.Serializable]
    public struct SkydiveMoodKey
    {
        public float Altitude;
        public Color Fog;
        public float FogDensity;
        public Color Ambient;
        public Color Sun;
        public float SunIntensity;
        public float Bloom;
        public float Exposure;
    }

    /// <summary>
    /// 맵마다의 높이별 분위기(맵별 하늘 설정). 그림만 바꾸는 <b>데이터</b>라 판정과 무관하다 — 클라의 SkydiveAtmosphere가 읽는다.
    /// <para>공용 패키지에 두는 이유는 <see cref="LaserVolume"/>과 같다: 서버도 같은 맵 씬을 읽는데 스크립트가 한쪽에만 있으면
    /// 반대쪽에서 missing script가 되어 씬 주입이 끊긴다. 주입은 받지 않는다(클라가 씬에서 찾는다).</para>
    /// </summary>
    public class SkydiveMood : MonoBehaviour
    {
        /// <summary>높은 것부터 아니어도 된다 — 읽는 쪽이 높이로 정렬해 쓴다.</summary>
        public SkydiveMoodKey[] Keys;
    }
}
