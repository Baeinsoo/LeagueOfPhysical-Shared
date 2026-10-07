using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 "다시 떨어지기" 표식. 이 높이(<see cref="BelowY"/>) 아래로 빠지면 표식 자리에서 다시 떨어진다.
    /// 판정은 서버(SkydiveRetrySystem)가 한다 — 레이저 부활과 같은 길.
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class RetryVolume : MonoBehaviour
    {
        public float BelowY = -15f;

        private RetryField field;
        private RetryZone registered;
        private bool hasRegistered;

        [Inject]
        public void Construct(RetryField field)
        {
            this.field = field;
            registered = new RetryZone(BelowY, transform.position);
            hasRegistered = true;
            field.Add(registered);
        }

        //  라운드마다 맵을 다시 로드한다 — 안 빼면 다음 판에 둘이 된다.
        private void OnDestroy()
        {
            if (hasRegistered)
            {
                field?.Remove(registered);
            }
        }
    }
}
