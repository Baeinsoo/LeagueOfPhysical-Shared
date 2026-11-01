using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 기둥의 몸(콜라이더). 걷는 몸이 부딪히게 Default 레이어에 둔다.
    /// 켜짐/꺼짐은 틱 인자만 보고 정하므로 되감아 다시 계산해도 그 틱의 기둥 그대로다.
    /// </summary>
    public sealed class KnockbackPillarColliders : System.IDisposable
    {
        private readonly KnockbackPillars pillars;
        private readonly GameObject[] objects;
        private readonly CapsuleCollider[] colliders;

        public KnockbackPillarColliders(KnockbackPillars pillars)
        {
            this.pillars = pillars;
            objects = new GameObject[pillars.Count];
            colliders = new CapsuleCollider[pillars.Count];
            for (int i = 0; i < pillars.Count; i++)
            {
                var center = pillars.Center(i);
                var go = new GameObject($"KnockbackPillar_{i}") { layer = 0 };
                go.transform.position = new Vector3(center.x, 0f, center.y);
                var capsule = go.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                capsule.radius = pillars.Radius;
                capsule.height = pillars.Height;
                //  캡슐 중심이 높이의 절반 위에 있어야 아래 끝이 y=0이다.
                capsule.center = new Vector3(0f, pillars.Height * 0.5f, 0f);
                objects[i] = go;
                colliders[i] = capsule;
            }
        }

        public void Apply(long tick)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                bool on = pillars.IsStanding(i, tick);
                if (colliders[i].enabled != on)
                {
                    colliders[i].enabled = on;
                }
            }
        }

        public bool IsEnabled(int i) => colliders[i].enabled;

        public void Dispose()
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    Object.DestroyImmediate(objects[i]);
                }
            }
        }
    }
}
