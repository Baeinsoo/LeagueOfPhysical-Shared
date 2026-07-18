using System;
using GameFramework.World;

namespace LOP
{
    /// <summary>엔티티가 그릴 모델(Addressable 에셋 경로). 순수 데이터 — 로드는 뷰가 한다.</summary>
    public class Appearance : Component
    {
        public string VisualId { get; }

        public Appearance(string visualId)
        {
            VisualId = visualId ?? throw new ArgumentNullException(nameof(visualId));
        }
    }
}
