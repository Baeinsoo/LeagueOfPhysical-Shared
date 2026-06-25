using GameFramework;
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
        }

        public async Task UnloadAsync()
        {
            await Addressables.UnloadSceneAsync(handle).Task;
        }
    }
}
