# Project Boxhead Network

**Unity 2D 탑다운 멀티플레이 슈팅 게임**  
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
1. **Lobby Scene**에서 시작
2. **Host** 버튼으로 방 생성
3. 다른 플레이어는 **Client** 버튼으로 접속

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
