# 2DNetcodeGameIsac

## Unity 씬 세팅(최소 구성)
아래는 **2DIsac 씬 기준**으로, 스크립트를 실제로 동작시키기 위한 **최소 필수 연결**입니다.  
스크립트는 “로직”만 담고 있으므로 **씬/프리팹 연결이 없으면 게임이 동작하지 않습니다.**

### 1) NetworkManager 오브젝트
1. 빈 오브젝트 생성 → 이름: `NetworkManager`
2. 컴포넌트 추가:
   - `NetworkManager`
   - `UnityTransport`
3. `NetworkManager`의 **Player Prefab**에 **플레이어 프리팹 연결**

> 이 단계가 없으면 Host/Client가 시작되지 않습니다.

### 2) 플레이어/투사체/적 프리팹
**Player Prefab**
- 컴포넌트:
  - `NetworkObject`, `NetworkTransform`, `NetworkRigidbody2D`
  - `Rigidbody2D`, `Collider2D`
- 스크립트:
  - `NetworkPlayerController2D`
  - `NetworkHealth`
- `NetworkPlayerController2D`의 `projectilePrefab`에 **Projectile 프리팹 연결**
  - 연결하지 않으면 발사가 되지 않습니다.

**Projectile Prefab**
- 컴포넌트:
  - `NetworkObject`, `NetworkTransform`, `NetworkRigidbody2D`
  - `Rigidbody2D`, `Collider2D` (**IsTrigger 체크**)
- 스크립트:
  - `NetworkProjectile2D`

**Enemy Prefab**
- 컴포넌트:
  - `NetworkObject`, `NetworkTransform`, `NetworkRigidbody2D`
  - `Rigidbody2D`, `Collider2D`
- 스크립트:
  - `NetworkEnemyChaser`
  - `NetworkHealth`

### 3) UI (Host/Join 버튼)
1. `Canvas` 생성
2. UI 구성:
   - `InputField (TMP)` : `RoomCode`
   - `Button` : `Host`
   - `Button` : `Join`
   - `Text (TMP)` : `Status`
3. `Canvas`에 `SessionManagerUI` 스크립트 추가 후 **각 UI 참조 연결**

### 4) ServerSpawnManager (적 자동 스폰)
1. 빈 오브젝트 생성 → 이름: `ServerSpawnManager`
2. 스크립트: `ServerSpawnManager`
3. **Enemy Prefab 연결**
4. `Spawn Point`(Transform) 1~2개 생성 후 배열에 등록

---

## 초보자를 위한 한 줄 요약
**스크립트는 움직임만 있고, 씬/프리팹이 연결되어야 실제로 게임이 됩니다.**

## 더 좋은 방법(간단 조언)
**초보라면 첫 단계는 “씬 하나만”으로 테스트**하는 것이 가장 안전합니다.  
Host/Client 성공 → 이동/발사 확인 → 그 다음 맵/아이템 확장 순서로 진행하세요.
