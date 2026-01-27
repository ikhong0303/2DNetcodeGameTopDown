# 2DNetcodeGameIsac

**Unity Netcode for GameObjects(NGO)**를 활용한 **2D 탑다운 멀티플레이어 슈팅 게임** 프로젝트입니다.  
**ScriptableObject(SO)**를 활용한 **데이터 주도 설계(Data-Driven Design)**와 **이벤트 기반 아키텍처**를 채택하여 확장성과 유지보수성을 최우선으로 고려했습니다.

---

## 📂 리포지토리 구조 및 아키텍처

현재 프로젝트는 기능별 모듈화가 되어 있으며, 주요 로직은 `Assets/Scripts` 하위에 구성되어 있습니다.

### 1. Core (데이터 & 이벤트)
게임의 밸런스 데이터와 시스템 이벤트를 관리합니다. 코드를 수정하지 않고 유니티 인스펙터에서 게임을 기획할 수 있습니다.
- **ConfigSOs**: `GameConfigSO`를 루트로 하여 적(`Enemy`), 웨이브(`Wave`), 투사체(`Projectile`), 이펙트(`Effect`) 설정을 관리합니다.
- **EventChannels**: `GameEventChannelSO`, `IntEventChannelSO` 등을 통해 컴포넌트 간 직접 참조(Coupling)를 제거하고 이벤트를 발행/구독(Pub/Sub)합니다.

### 2. Networking (핵심 로직 & 동기화)
멀티플레이어 동기화와 핵심 게임플레이 로직이 통합된 영역입니다.
- **NetworkGameManager**: 게임의 **사령탑(Server Authoritative)**입니다.
  - 웨이브 관리 (`WaveLoop`)
  - 적 스폰 및 관리 (`SpawnWave`)
  - 승패 판정 및 게임 오버 체크 (`CheckGameOver`)
- **NetworkSessionLauncher**: UI와 연결되어 Host/Client 세션을 시작하는 진입점입니다.
- **NetworkObjectPool**: 총알이나 이펙트 등 빈번하게 생성되는 네트워크 오브젝트를 미리 생성(Prewarm)하고 재사용하여 성능을 최적화합니다.
- **Gameplay Objects**:
  - `NetworkEnemy`: 추적 AI 및 동기화.
  - `NetworkProjectile`: 투사체 이동 및 충돌 처리.
  - `NetworkHealth`: 체력 및 피격/사망 상태 동기화.

### 3. Player
- **NetworkPlayerController**: 플레이어의 입력 처리, 이동, 공격, 애니메이션 상태를 네트워크상에서 동기화합니다.

---

## 🚀 씬 세팅 가이드 (Scene Setup)

게임을 실행하기 위해 필요한 최소한의 씬 구성 요소입니다.

### 1️⃣ NetworkManager (필수)
- 빈 오브젝트 생성 (`NetworkManager`)
- 컴포넌트: `NetworkManager`, `UnityTransport`
- **설정**: 
  - `Player Prefab`: 플레이어 프리팹 연결
  - `NetworkPrefabs`: 투사체, 적, 이펙트 등 런타임에 스폰될 모든 프리팹 등록

### 2️⃣ NetworkGameManager (게임 로직)
- 빈 오브젝트 생성 (`NetworkGameManager`)
- 컴포넌트: `NetworkGameManager`
- **설정 연결**:
  - **Game Config**: `GameConfigSO` 에셋 연결 (모든 밸런스 데이터 포함)
  - **Spawn Points**: 적이 등장할 `Transform` 위치 배열 할당
  - **Events**: `WaveStarted`, `WaveCompleted`, `GameOver` 이벤트 채널 연결

### 3️⃣ UI 및 실행
- UI 버튼(Host/Client)에 `NetworkSessionLauncher` 스크립트를 연결하거나, 해당 스크립트의 `StartHost()`, `StartClient()` 메서드를 호출하도록 설정합니다.

---

## 🛠️ 개발 현황 및 로드맵

### ✅ 현재 구현된 기능
- **P2P 멀티플레이 연결**: Host-Client 구조
- **데이터 기반 웨이브 시스템**: SO 설정에 따른 적 스폰
- **오브젝트 풀링**: 네트워크 부하 최소화
- **기본 전투 루프**: 이동 → 발사 → 피격 → 파괴

### 🚧 추후 개선 사항 (TODO)
- **UI 시스템 고도화**: 현재 비어있는 UI 폴더 구현 (게임 오버 화면, 로비 등)
- **Combat 로직 분리**: `Networking` 폴더에 뭉쳐있는 전투 로직을 `Combat` 폴더로 리팩토링 및 모듈화
