using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 바람 표시. 맵이 올라올 때 <see cref="WindField"/>를 주입받아 스스로 등록한다
    /// (<c>GameLifetimeScope</c>가 <c>sceneLoaded</c>를 듣고 <c>InjectSceneObjects</c>를 부른다).
    ///
    /// <see cref="SpawnPoint"/>와 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서 만들고
    /// 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈 컴포넌트가
    /// 씬 주입을 끊는다.
    ///
    /// <para>(Unreal의 <c>APhysicsVolume</c>에 대응 — 볼륨이 그 안의 운동 규칙을 덮어쓴다.
    /// Unity의 <c>WindZone</c>은 나뭇잎·천 연출 전용이라 캐릭터에 안 먹어 쓸 수 없다.)</para>
    /// </summary>
    // 이 표시가 없으면 유니티는 플레이 중이 아닐 때 Awake/OnDestroy 같은 생명주기 함수를
    // 아예 불러 주지 않는다 — 그러면 씬 편집 중에 마커를 지워도 등록 해제가 안 걸린다.
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class WindVolume : MonoBehaviour
    {
        /// <summary>원기둥 반지름. 기류 기둥은 좁게, 횡풍 구간은 코스를 다 덮게 넓힌다.</summary>
        public float Radius = 25f;

        /// <summary>원기둥 높이. 이 값이 <b>누가 바람을 느끼는지</b>를 정한다 — 짧으면 패러세일만, 구간을 다 덮으면 셋 다.</summary>
        public float Height = 120f;

        /// <summary>방향 × 세기 (m/s).</summary>
        public Vector3 Wind = new Vector3(0f, 14f, 0f);

        private WindField field;
        private WindCylinder cylinder;

        [Inject]
        public void Construct(WindField field)
        {
            this.field = field;
            cylinder = new WindCylinder(
                transform.position.ToNumerics(), Radius, Height, Wind.ToNumerics());
            field.Add(cylinder);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 빼면 바람이 두 배가 된다.
            if (field != null && cylinder != null)
            {
                field.Remove(cylinder);
            }
        }

        // 배치가 곧 코스 설계다. 에디터에서 어디에 얼마나 부는지 보이게 한다.
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position, new Vector3(Radius * 2f, Height, Radius * 2f));
            Gizmos.DrawRay(transform.position, Wind);
        }
    }
}
