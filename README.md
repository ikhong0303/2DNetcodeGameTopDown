# Project Boxhead Network

**Unity Netcode for GameObjects(NGO)**를 기반으로 한 **2D 탑다운 멀티플레이어 슈팅 게임**입니다.
**Boxhead**와 **Vampire Survivors**에서 영감을 받아, 끊임없이 몰려오는 적들을 상대로 친구와 함께 생존하는 호드 서바이벌(Horde Survival) 장르를 지향합니다.

---

## 🎮 핵심 컨셉 (Core Concept)

*   **장르**: 2D 탑다운 웨이브 슈팅
*   **게임플레이**: 무한 웨이브 생존 (적 처치 → 웨이브 클리어 → 난이도 상승 → 반복)
*   **멀티플레이**: 2인 협동 P2P (Host-Client 구조)
*   **목표**: 최대한 오래 생존하여 높은 웨이브에 도달하는 것.

---

## 🏗️ 아키텍처 및 구현 (Architecture)

이 프로젝트는 **확장성**과 **구조적 견고함**을 최우선으로 설계되었습니다.

### 1. 네트워크 시스템 (Networking)
*   **프레임워크**: Netcode for GameObjects (NGO)
*   **권한 관리 (Server Authoritative)**: 웨이브 진행, 적 스폰, 판정 등 핵심 로직은 호스트(서버)가 전담하고 클라이언트는 시각적 동기화만 수행.
*   **동기화**:
    *   `NetworkTransform`: 위치/회전 동기화
    *   `NetworkVariable`: 체력, 점수, 게임 상태(Alive/Downed) 동기화
*   **연결 방식**: Unity Transport 기반 (Room Code / IP 접속)

### 2. 프로젝트 폴더 구조 (Project Structure)
기능별로 명확하게 모듈화되어 있습니다. `Assets/Scripts/` 하위 구조:

*   **📂 Core**: 게임 밸런스 데이터(`ScriptableObject`)와 이벤트 채널(`EventChannel`) 관리.
*   **📂 Gameplay**
    *   **Combat**: `NetworkHealth`, `NetworkProjectile` 등 전투 관련 로직.
    *   **Enemy**: `NetworkEnemy` (추적 AI).
*   **📂 Managers**: `NetworkGameManager`(게임 루프), `SoundManager`(오디오) 등 게임 전체 흐름 관리.
*   **📂 Networking**: `NetworkSessionLauncher` 등 순수 연결 진입점.
*   **📂 Player**: `NetworkPlayerController` (입력 및 캐릭터 제어).

### 3. 주요 기술적 특징 (Technical Features)
*   **데이터 주도 설계 (Data-Driven)**: 적 스탯, 웨이브 정보, 무기 데미지 등은 코드가 아닌 `ScriptableObject` 에셋으로 관리되어 기획 수정이 용이함.
*   **이벤트 기반 통신 (Event-Based)**: 옵저버 패턴(`GameEventChannelSO`)을 활용하여 컴포넌트 간 결합도(Coupling) 최소화.
*   **오브젝트 풀링 (Object Pooling)**: 탄환, 이펙트 등 빈번한 생성/파괴 객체를 재사용하여 네트워크 부하 및 GC 최소화.
*   **카메라 시스템**: `Cinemachine`을 활용하여 각 로컬 클라이언트가 본인의 캐릭터만 독립적으로 추적.

---

## 🚀 시작 가이드 (Getting Started)

### 1. 실행 방법
1.  **Lobby Scene**에서 시작합니다. (또는 `Bootstrap` 씬)
2.  **Host** 버튼을 눌러 방을 생성합니다.
3.  다른 플레이어는 **Client** 버튼을 눌러 접속합니다.

### 2. 조작법 (Controls)
*   **이동**: `W`, `A`, `S`, `D`
*   **조준**: 마우스 커서
*   **발사**: 마우스 왼쪽 버튼
*   **상호작용(부활)**: `E` 키 (쓰러진 동료 근처에서)

---

## 🛠️ 개발 로드맵 (Roadmap)
현재 기본 핵심 루프(Core Loop)가 완성된 상태이며, 다음 단계로 콘텐츠 확장이 진행될 예정입니다.

- [x] **기본 멀티플레이 동기화** (이동/발사/피격)
- [x] **웨이브 시스템 & 적 스폰**
- [x] **사운드 매니저** (현재 로그 출력 형태)
- [ ] **UI 고도화** (로비, 게임오버, HUD)
- [ ] **다양한 무기 및 아이템 추가**
