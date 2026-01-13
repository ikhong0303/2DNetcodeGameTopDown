# 2DNetcodeGameIsac

## Unity 씬 세팅(최소 구성)
아래는 **2DIsac 씬 기준**으로, 스크립트를 실제로 동작시키기 위한 **최소 필수 연결**입니다.  
스크립트는 “로직”만 담고 있으므로 **씬/프리팹 연결이 없으면 게임이 동작하지 않습니다.**

### 1) SessionConnector 오브젝트
1. 빈 오브젝트 생성 → 이름: `SessionConnector`
2. 컴포넌트 추가:
   - `SessionConnector`
3. `DontDestroyOnLoad`로 씬 이동 시 유지됩니다.

> 이 단계가 없으면 Host/Join 버튼이 동작하지 않습니다.

### 2) NetworkManager 오브젝트
1. 빈 오브젝트 생성 → 이름: `NetworkManager`
2. 컴포넌트 추가:
   - `NetworkManager`
   - `UnityTransport`
3. `NetworkManager`의 **Player Prefab**에 **플레이어 프리팹 연결**

> 이 단계가 없으면 Host/Client가 시작되지 않습니다.

### 3) 플레이어/투사체/적 프리팹
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

### 4) UI (Host/Join 버튼)
1. `Canvas` 생성
2. UI 구성:
   - `InputField (TMP)` : `RoomCode`
   - `Button` : `Host`
   - `Button` : `Join`
   - `Text (TMP)` : `Status`
3. `Canvas`에 `SessionManagerUI` 스크립트 추가 후 **각 UI 참조 연결**

### 5) ServerSpawnManager (적 자동 스폰)
1. 빈 오브젝트 생성 → 이름: `ServerSpawnManager`
2. 스크립트: `ServerSpawnManager`
3. **Enemy Prefab 연결**
4. `Spawn Point`(Transform) 1~2개 생성 후 배열에 등록

### 6) Unity Services 설정 확인
1. `Project Settings → Services → Project ID` 연결 확인
2. `Authentication` 활성화 확인
3. `Multiplayer` 활성화 확인

---

## 초보자를 위한 한 줄 요약
**스크립트는 움직임만 있고, 씬/프리팹이 연결되어야 실제로 게임이 됩니다.**

## 더 좋은 방법(간단 조언)
**초보라면 첫 단계는 “씬 하나만”으로 테스트**하는 것이 가장 안전합니다.  
Host/Client 성공 → 이동/발사 확인 → 그 다음 맵/아이템 확장 순서로 진행하세요.

---

## 지금 우선순위로 하면 좋은 것 (코딩 아님, 계획/검증 중심)
### 1) 1P/2P “핵심 플레이 루프”만 완벽하게
**목표:** 두 명이 들어와서 **이동 → 발사 → 피격 → 사망 → 재시작**까지 문제없이 되는지 확인하세요.  
몬스터나 콘텐츠를 늘리기 전에 **기본 루프가 안정적이어야 이후 확장이 훨씬 쉬워집니다.**

### 2) 아주 안전한 테스트 순서 유지
현재처럼 **단일 테스트 씬**에서 **Host → Client** 순서로 눌러 확인하는 방식이 가장 안전합니다.  
문제가 생겨도 어디서 막혔는지 추적하기 쉬워서 초보자에게 특히 좋습니다.

### 3) UI 바인딩만 실수 없이
UI 연결은 가장 흔한 실수 포인트입니다.  
**입력 필드/버튼/상태 텍스트 참조**가 꼬이면 코드 문제처럼 보여도 사실은 연결 문제인 경우가 많습니다.

### “몬스터 종류 늘리기”는 언제?
**지금은 조금 뒤가 안전합니다.**  
기본 네트워크 루프가 완벽하지 않으면 몬스터를 늘릴수록 버그 범위가 폭발합니다.  
먼저 2P까지 안정화하면 이후 몬스터 추가는 **콘텐츠 확장**처럼 자연스럽게 됩니다.

### 한 줄 조언 (더 좋은 방법)
**“부트스트랩/테스트 씬 하나만 두고 Host/Client 테스트에 집중”**하세요.  
복잡한 씬으로 들어가기 전에 단순한 씬에서 모든 것을 검증하면 디버깅이 훨씬 쉽습니다.
