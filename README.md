# Unity 2D 탑다운 멀티플레이 슈팅 게임 탬플릿
Netcode for GameObjects(NGO) 기반 | 2인 협동 P2P(Host-Client)

---

## 🎮 게임 개요

| 항목 | 내용 |
|------|------|
| **장르** | 2D 탑다운 웨이브 슈팅 (호드 서바이벌) |
| **플레이어** | 2인 협동 |
| **목표** | 웨이브를 오래 버티며 생존 |

---

## 🔄 핵심 게임 루프

1. 플레이어 2명이 접속할 때까지 대기
2. 카운트다운 후 게임 시작
3. 웨이브마다 적이 스폰됨
4. 적을 전부 처치하면 다음 웨이브로 진행
5. 모두 다운되면 게임오버
6. 승리/패배 후 재시작 가능

---

## 📁 폴더 구조 (`Assets/Scripts/`)

| 폴더 | 주요 스크립트 | 역할 |
|------|--------------|------|
| **Managers** | `NetworkGameManager`, `EnemySpawner`, `SoundManager` | 게임 전체 흐름(웨이브, 승패, 재시작), 적 스폰, 사운드 |
| **Networking** | `NetworkSessionLauncher` | Relay/Host/Client 연결 관리 |
| **Player** | `NetworkPlayerController`, `PlayerInputHandler` | 이동, 공격, 카메라, 점수 / 입력 처리 분리 |
| **Gameplay/Combat** | `NetworkHealth`, `NetworkProjectile`, `NetworkEffect` | 체력·다운·부활·HP바, 투사체 발사·충돌, 이펙트 재생 |
| **Gameplay/Enemy** | `NetworkEnemy`, `EnemyAI`, `EnemyVisualFeedback` | 적 상태·피격·사망·점수, 이동·추적, 피격 이펙트 |
| **Pooling** | `NetworkObjectPool` | 투사체/이펙트 풀링 |
| **Core** | `GameConfigSO`, `WaveConfigSO`, `EnemyConfigSO`, `ProjectileConfigSO`, `EffectConfigSO`, `GameEventChannelSO` | ScriptableObject 데이터 및 이벤트 채널 |
| **UI** | `NetworkUIManager` | 웨이브, 상태, 남은 적 표시 |

---

## 🌐 네트워크 구조

### 서버 권위 (Server Authoritative)
- 스폰, 웨이브 진행, 판정은 호스트(서버)가 결정

### 동기화 방식
- `NetworkTransform`: 위치/회전 동기화
- `NetworkVariable`: 체력, 점수, 게임 상태 동기화
- `RPC`: 발사/이펙트/게임오버 이벤트 전달

---

## ⚙️ 설계 특징

| 특징 | 설명 |
|------|------|
| **데이터 기반 설계** | ScriptableObject로 밸런스/웨이브/이펙트 설정 |
| **이벤트 채널** | `GameEventChannelSO`로 UI 업데이트 연결 |
| **오브젝트 풀링** | 투사체/이펙트 생성 비용 절약 |

---

## 🚀 시작 가이드

### 실행 방법
1. **SampleScene**에서 시작
2. **Host** 버튼으로 방 생성
3. **Join Code** 입력 후 **Join** 버튼으로 접속

### 조작법
| 입력 | 동작 |
|------|------|
| `W` `A` `S` `D` | 이동 |
| 마우스 커서 | 조준 |
| 마우스 왼쪽 버튼 | 발사 |

---

## 📊 현재 상태

### ✅ 구현 완료
- 기본 멀티플레이 동기화 (이동/발사/피격)
- 웨이브 시스템 및 적 스폰
- 다운/부활 시스템
- 승리/패배 및 재시작 시스템
- 오브젝트 풀링
- UI (웨이브 표시, 남은 적, 상태)

### 🔧 개선 필요
- `NetworkGameManager`, `NetworkPlayerController` 책임 분리
- 싱글톤 의존도 감소
- UI와 게임 로직 경계 분리

---

## 📥 학생용 프로젝트 시작 가이드

### 1단계: 프로젝트 다운로드
1. 이 GitHub 페이지에서 **Code** 버튼 클릭
2. **Download ZIP** 선택
3. 다운로드된 ZIP 파일 압축 해제

### 2단계: Unity Hub에서 열기
1. **Unity Hub** 실행
2. **Projects** → **Add** → 압축 해제한 폴더 선택
3. Unity 버전이 맞지 않으면 설치 안내가 나올 수 있음

### 3단계: Unity 계정 연동 (중요!)
1. Unity 에디터에서 프로젝트 열기
2. 메뉴: **Edit** → **Project Settings** → **Services**
3. **Link your Unity project** 클릭
4. Unity 계정으로 로그인
5. 새 프로젝트 ID 생성 또는 기존 프로젝트 선택

> ⚠️ **Unity 계정 연동을 하지 않으면 Relay 서비스가 작동하지 않아 멀티플레이가 불가능합니다!**

### 4단계: 게임 실행
1. **Assets/Scenes/SampleScene** 열기
2. **Play** 버튼으로 실행
3. **Host** 버튼으로 방 생성 → Join Code 표시됨
4. 두 번째 플레이어: 빌드 파일 실행 또는 다른 PC에서 Join Code 입력 후 접속
5. 2명 접속 완료 시 게임 자동 시작!

> 💡 **혼자서 테스트하기**: Unity 6에서는 **Multiplayer Play Mode**를 사용하면 빌드 없이 에디터에서 2명을 동시에 테스트할 수 있습니다.
> - 메뉴: **Window** → **Multiplayer Play Mode**
> - **Player 2** 활성화 끝난 후 Play 버튼 클릭

---

## ⚠️ Unity Relay 서버 제한량 안내

이 프로젝트는 **Unity Relay** 서비스를 사용하여 멀티플레이를 구현합니다.  
Unity는 무료 플랜에서 **월별 제한량**을 제공하며, 제한량을 모두 사용하면 테스트가 불가능해집니다.

### 무료 플랜 제한 (2024년 기준)
- **동시 접속자 (CCU)**: 200명
- **대역폭**: 50GB/월

### 남은 제한량 확인 방법
1. [Unity Cloud Dashboard](https://cloud.unity.com/) 접속
2. 로그인 후 프로젝트 선택
3. **Multiplayer** → **Relay** 메뉴에서 사용량 확인

> ⚠️ 제한량이 부족하면 접속 시 오류가 발생할 수 있습니다. 미리 확인하세요!

