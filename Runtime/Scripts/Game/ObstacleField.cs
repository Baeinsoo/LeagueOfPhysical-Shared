using System.Collections.Generic;

namespace LOP
{
    /// <summary>틱만 넣으면 자세가 정해지는 움직이는 장애물. 시뮬은 정수 틱, 그림은 소수 틱으로 같은 식을 부른다.</summary>
    public interface IPosedObstacle
    {
        void Pose(double tick);
    }

    /// <summary>
    /// 이 판에 놓인 움직이는 대형 장애물 전부(도는 원판·날개, 조리개). 맵 씬의 마커가 로드될 때 스스로 들어온다.
    /// <para><see cref="DoorField"/>와 같은 이유로 MonoBehaviour <b>참조</b>로 담는다 — 시뮬이 매 틱 그 트랜스폼을 직접 옮긴다.</para>
    /// </summary>
    public class ObstacleField
    {
        private readonly List<IPosedObstacle> _all = new List<IPosedObstacle>();

        public IReadOnlyList<IPosedObstacle> All => _all;

        /// <summary>같은 것을 두 번 넣어도 하나만 남는다(재등록 방지 — DoorField와 같은 이유).</summary>
        public void Add(IPosedObstacle obstacle)
        {
            if (obstacle == null || _all.Contains(obstacle))
            {
                return;
            }
            _all.Add(obstacle);
        }

        public void Remove(IPosedObstacle obstacle) => _all.Remove(obstacle);
    }
}
