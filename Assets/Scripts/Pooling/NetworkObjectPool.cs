/// =============================================================================
/// NetworkObjectPool.cs
/// =============================================================================
/// 이 스크립트의 역할:
/// - 네트워크 오브젝트의 풀링(재사용)을 관리하는 싱글톤 매니저입니다.
/// - 투사체, 이펙트 등 자주 생성/파괴되는 오브젝트의 성능을 최적화합니다.
/// - RegisterPrefab(): 프리팹을 풀에 등록하고 미리 인스턴스를 생성(prewarm)합니다.
/// - Spawn(): 풀에서 오브젝트를 가져와 네트워크에 스폰합니다.
/// - Despawn(): 오브젝트를 네트워크에서 디스폰하고 풀에 반환합니다.
/// - IPooledObject 인터페이스를 구현한 오브젝트는 스폰/디스폰 시 콜백을 받습니다.
/// - DontDestroyOnLoad 옵션으로 씬 전환 시에도 유지할 수 있습니다.
/// =============================================================================

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TopDownShooter.Pooling
{
    /// <summary>
    /// 풀링 대상 오브젝트가 구현해야 하는 인터페이스
    /// 스폰/디스폰 시 콜백을 받습니다.
    /// </summary>
    public interface IPooledObject
    {
        /// <summary>풀에서 스폰될 때 호출</summary>
        void OnSpawned();
        
        /// <summary>풀로 반환될 때 호출</summary>
        void OnDespawned();
    }

    /// <summary>
    /// 네트워크 오브젝트 풀 매니저 (싱글톤)
    /// 오브젝트 재사용으로 성능 최적화
    /// </summary>
    public class NetworkObjectPool : MonoBehaviour
    {
        // ===== 싱글톤 =====
        
        private static NetworkObjectPool instance;

        // ===== 설정 =====
        
        [SerializeField] private bool dontDestroyOnLoad = true;  // 씬 전환 시 유지 여부

        // ===== 풀 저장소 =====
        
        /// <summary>프리팹 -> 해당 프리팹의 인스턴스 큐</summary>
        private readonly Dictionary<NetworkObject, Queue<NetworkObject>> poolLookup = new();
        
        /// <summary>인스턴스 -> 원본 프리팹 맵핑 (반환 시 사용)</summary>
        private readonly Dictionary<NetworkObject, NetworkObject> prefabLookup = new();

        /// <summary>싱글톤 인스턴스</summary>
        public static NetworkObjectPool Instance => instance;

        /// <summary>
        /// Awake: 싱글톤 설정
        /// </summary>
        private void Awake()
        {
            // 중복 인스턴스 방지
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            // 씬 전환 시 유지
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// 프리팹을 풀에 등록하고 미리 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="prefab">등록할 프리팹</param>
        /// <param name="prewarmCount">미리 생성할 인스턴스 수</param>
        public void RegisterPrefab(NetworkObject prefab, int prewarmCount)
        {
            // null이거나 이미 등록된 프리팹은 무시
            if (prefab == null || poolLookup.ContainsKey(prefab))
            {
                return;
            }

            // 새 큐 생성
            var queue = new Queue<NetworkObject>();
            poolLookup.Add(prefab, queue);

            // Prewarm: 지정된 수만큼 미리 인스턴스 생성
            for (var i = 0; i < prewarmCount; i++)
            {
                var instance = Instantiate(prefab);
                instance.gameObject.SetActive(false);  // 비활성화 상태로 대기
                queue.Enqueue(instance);
                prefabLookup.Add(instance, prefab);    // 프리팹 맵핑 기록
            }
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져와 네트워크에 스폰합니다.
        /// </summary>
        /// <param name="prefab">스폰할 프리팹</param>
        /// <param name="position">스폰 위치</param>
        /// <param name="rotation">스폰 회전</param>
        /// <returns>스폰된 NetworkObject</returns>
        public NetworkObject Spawn(NetworkObject prefab, Vector3 position, Quaternion rotation)
        {
            // null 체크
            if (prefab == null)
            {
                return null;
            }

            // 아직 등록되지 않은 프리팹이면 자동 등록
            if (!poolLookup.TryGetValue(prefab, out var queue))
            {
                RegisterPrefab(prefab, 0);
                queue = poolLookup[prefab];
            }

            // 큐에서 사용 가능한 인스턴스 찾기
            NetworkObject instance = null;
            while (queue.Count > 0)
            {
                instance = queue.Dequeue();
                if (instance != null)  // 파괴되지 않은 인스턴스 찾음
                {
                    break;
                }
            }

            // 사용 가능한 인스턴스가 없으면 새로 생성
            if (instance == null)
            {
                instance = Instantiate(prefab);
            }

            // 프리팹 맵핑이 없으면 추가
            if (!prefabLookup.ContainsKey(instance))
            {
                prefabLookup.Add(instance, prefab);
            }

            // 위치/회전 설정 및 활성화
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            
            // 네트워크에 스폰 (true: 씬 전환 시에도 유지)
            instance.Spawn(true);

            // IPooledObject 콜백 호출
            if (instance.TryGetComponent<IPooledObject>(out var pooledObject))
            {
                pooledObject.OnSpawned();
            }

            return instance;
        }

        /// <summary>
        /// 오브젝트를 네트워크에서 디스폰하고 풀에 반환합니다.
        /// </summary>
        /// <param name="instance">디스폰할 오브젝트</param>
        public void Despawn(NetworkObject instance)
        {
            if (instance == null)
            {
                return;
            }

            // IPooledObject 콜백 호출
            if (instance.TryGetComponent<IPooledObject>(out var pooledObject))
            {
                pooledObject.OnDespawned();
            }

            // 네트워크에서 디스폰 (true: 클라이언트에서도 제거)
            instance.Despawn(true);
            
            // 비활성화
            instance.gameObject.SetActive(false);

            // 풀에 반환
            if (prefabLookup.TryGetValue(instance, out var prefab))
            {
                poolLookup[prefab].Enqueue(instance);
            }
            else
            {
                // 풀에 등록되지 않은 오브젝트면 파괴
                Destroy(instance.gameObject);
            }
        }
    }
}
