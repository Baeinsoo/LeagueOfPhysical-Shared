using System;

namespace LOP
{
    /// <summary>
    /// 매치 데이터에서 이번 판이 쓸 씬을 고를 때의 공통 검증 규칙.
    /// 마스터데이터 조회 자체는 호출자가 하고, 여기서는 "무엇을 잘못된 상태로 볼지"만 정한다.
    /// </summary>
    public static class MatchSceneResolver
    {
        /// <summary>
        /// 이번 판이 쓸 라운드의 인덱스. 지금은 항상 첫 라운드다 —
        /// 한 매치에서 여러 게임을 연속으로 도는 로테이션은 아직 구현하지 않았다.
        /// </summary>
        public static int CurrentRoundIndex(int roundCount)
        {
            if (roundCount <= 0)
            {
                throw new InvalidOperationException("매치에 라운드가 없어 씬을 정할 수 없습니다.");
            }

            return 0;
        }

        /// <summary>
        /// 마스터데이터에서 찾은 씬 경로를 검증해 돌려준다.
        /// 데이터 누락을 조용히 넘기면 씬이 안 뜨는 이유를 런타임에 추적해야 하므로 여기서 끊는다.
        /// </summary>
        public static string RequireScenePath(string tableName, int id, string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new InvalidOperationException(
                    $"{tableName}의 씬 경로가 비어 있습니다. id: {id}");
            }

            return scenePath;
        }

        /// <summary>
        /// 마스터데이터에서 찾은 행을 검증해 돌려준다.
        /// 없는 id와 값이 빈 행은 원인이 달라, 뭉뚱그리면 데이터를 고칠 곳을 못 찾는다.
        /// </summary>
        public static T RequireRow<T>(string tableName, int id, T row) where T : class
        {
            if (row == null)
            {
                throw new InvalidOperationException($"{tableName}에 없는 id입니다. id: {id}");
            }

            return row;
        }
    }
}
