using GameFramework;
using GameFramework.Runner;
using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace LOP
{
    /// <summary>
    /// Addressables 기반 맵 씬 로더. 클·서 동일 구현이라 LOP-Shared에 1벌로 둔다.
    /// (DI 등록은 use-side RoomLifetimeScope에서 — 정책은 use-side.)
    /// </summary>
    public class AddressablesMapLoader : IMapLoader
    {
        private AsyncOperationHandle<SceneInstance> handle;

        public async Task LoadAsync(string mapId)
        {
            handle = Addressables.LoadSceneAsync(mapId, LoadSceneMode.Additive);
            await handle.Task;

            // handle.Task는 실패해도 예외 없이 끝난다 — 확인하지 않으면 맵 없이 판이 돈다.
            ThrowIfFailed(mapId, handle.Status == AsyncOperationStatus.Succeeded, handle.OperationException?.Message);
        }

        /// <summary>맵이 안 떴으면 판 시작을 막는다. 바닥이 없으면 캐릭터가 계속 떨어지고 서버가 되돌리길 반복한다.</summary>
        public static void ThrowIfFailed(string mapId, bool succeeded, string cause)
        {
            if (succeeded)
            {
                return;
            }
            throw new InvalidOperationException(
                $"맵 씬을 불러오지 못했다: {mapId} — {cause}. " +
                "에디터라면 Assets/Art 서브모듈이 클라가 가리키는 커밋과 같은지 확인할 것(git submodule update), " +
                "빌드라면 그 타깃의 어드레서블 카탈로그가 올라가 있는지 확인할 것.");
        }

        public async Task UnloadAsync()
        {
            if (!handle.IsValid())
            {
                return;
            }
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                // 뜬 씬이 없으니 내릴 것도 없다 — 핸들만 놓는다.
                Addressables.Release(handle);
                return;
            }
            await Addressables.UnloadSceneAsync(handle).Task;
        }
    }
}
