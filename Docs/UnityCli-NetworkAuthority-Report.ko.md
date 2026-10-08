# Unity CLI 및 네트워크 권한 분석

분석일: 2026-10-08 (KST). 현재 작업 트리 기준: Unity 6000.3.18f1, NGO 2.12.0, Multiplayer Services 2.3.3. 기존 미커밋 에셋/패키지 업그레이드가 있으므로 이 분석의 설정은 HEAD와 다를 수 있다.

## 1. CLI 설치 및 실제 사용

Unity CLI 1.0.0-beta.12가 Windows MSIX로 이미 설치되어 있었다. 중복 설치 없이 `unity --version`, `unity editors -i`, `unity doctor` 실행에 성공했다. 6000.3.18f1과 6000.0.59f2 에디터를 인식했다. doctor는 WindowsApps와 Local/Unity/bin의 PATH 중복을 경고했다. 버전이 다른 실행 파일이 선택될 가능성이 있어 향후 정리 대상이다.

이 프로젝트에는 에디터 제어용 Pipeline이 없었다. 다음 공식 명령으로 `com.unity.pipeline` 0.8.0-exp.1을 설치했다. 설치 전후 lock 비교에서 변경된 패키지 항목은 Pipeline 하나였다.

```powershell
# CLI가 없는 다른 PC에서 설치
winget install Unity.CLI
# 새 PowerShell에서 확인
unity --version
unity editors -i
# 프로젝트 루트에서 실행
unity pipeline install --project-path . --non-interactive
unity pipeline list
unity command --project-path . --detail compact
```

`unity pipeline list`는 대상 프로젝트의 설치 상태 true와 버전을 반환했다. 그러나 검증 시 서버 포트가 없고 Server Reachable=false였으며, `unity command`는 No Pipeline instance found로 실패했다. 패키지 설치와 CLI 실행은 검증됐지만 에디터 원격 명령/빌드/테스트 성공은 확인하지 못했다. 에디터의 패키지 리로드 완료 및 Pipeline 서버 상태를 확인한 후 마지막 두 명령을 재실행해야 한다. 다른 열려 있는 프로젝트에는 명령을 보내지 않았다. doctor에는 계정/환경 정보가 포함되므로 원본 로그는 커밋하지 않는다.

공식 자료:
- https://docs.unity.com/ko-kr/unity-cli/use-unity-cli
- https://docs.unity.com/ko-kr/unity-cli/unity-pipeline/unity-pipeline-package
- https://docs.unity.com/ko-kr/unity-cli/unity-cli-reference

## 2. 실제 시뮬레이션 권한

구조는 Relay를 경유하는 NGO Host–Client다. Relay가 게임 물리를 실행하는 서버는 아니다. 호스트 PC가 서버 시뮬레이션과 자기 플레이어의 로컬 클라이언트를 함께 실행한다. README의 서버 권위 설명만으로 이동까지 서버 권위라고 판단하면 안 된다.

| 기능 | 호스트/서버 | 원격 클라이언트 | 근거 |
|---|---|---|---|
| 입력/조준/카메라/발소리 | 자기 플레이어만 처리 | 자기 플레이어만 처리 | NetworkPlayerController.IsOwner |
| 플레이어 이동 | 자기 플레이어 물리 실행, 원격 위치 수신 | 자기 플레이어 Rigidbody2D 속도 설정 및 위치 전송 | FixedUpdate, PlayerNet AuthorityMode=1 |
| 발사 | 요청 수신 후 서버 위치의 firePoint에서 투사체 생성 | 방향을 ServerRpc로 전송, 소리는 즉시 재생 | HandleAttack, FireServerRpc |
| 투사체 이동·명중 | 속도·수명·충돌·데미지 처리 | 위치 수신/보간 | NetworkProjectile, ProjectileNet AuthorityMode=0 |
| 적 AI·접촉 피해 | 타겟 검색·이동·피해 실행 | 표시 및 상태 수신 | EnemyAI.FixedUpdate, NetworkEnemy.OnCollisionStay2D |
| HP·다운·부활·점수 | NetworkVariable 변경 | 변경 결과 수신 | NetworkHealth, AddScore |
| 스폰·웨이브·승패·재시작 | 서버 실행 | RPC로 UI/상태 표시 | NetworkGameManager, EnemySpawner |
| 재시작 위치 | 서버 위치 변경 후 ClientRpc | 소유자가 다시 자기 위치 변경 | ResetPosition/ResetPositionClientRpc |

NGO 설치 소스의 NetworkTransform.AuthorityModes는 Server=0, Owner=1이다. PlayerNet과 ProjectileNet의 Transform GUID는 실제 패키지의 NetworkTransform.cs.meta와 일치한다. PlayerNet에는 NetworkRigidbody2D도 존재하며 패키지 구현은 Transform 권한에 따라 비권한 Rigidbody를 kinematic으로 만든다. 따라서 플레이어를 모든 피어에서 동적 물리로 시뮬레이션한다고 해석하면 틀린다.

**중요한 미확정 에셋 문제:** EnemyNet의 Transform처럼 직렬화된 컴포넌트 GUID `da52bf8bbc1de48cfb221a6ff30f7972`는 검색한 Assets/Packages/NGO 패키지 메타에 대응하지 않았다. 설정 필드는 서버 권위처럼 보이지만 실제 유효한 NetworkTransform인지 에디터에서 확인해야 한다. Missing Script라면 적 위치 동기화 자체에 문제가 된다. ProjectileNet에는 NetworkRigidbody2D가 없고 `m_Simulated=1`이다. OnNetworkSpawn은 서버에서만 simulated=true를 설정할 뿐 클라이언트에서 false로 바꾸지 않는다. 피해는 서버 가드가 막지만 클라이언트 물리 자체를 비활성화한 구현은 아니다.

## 3. 권한 설계 평가

2인 협동 템플릿에서 소유자 이동 + 서버 전투는 사용할 수 있는 절충안이다. 로컬 이동 반응이 빠른 장점이 있다. 다만 서버가 검증한 입력을 재시뮬레이션하는 예측/재조정 구조는 구현되어 있지 않다. 이동 속도/위치 검증도 없어 클라이언트 위치를 신뢰한다.

클라이언트가 보는 자기 현재 위치와 서버가 받은 위치는 시간적으로 다르다. 서버는 그 위치를 기준으로 적 추적·접촉 피해·발사 원점을 계산한다. 따라서 화면상 피했는데 맞거나 총알이 뒤쪽에서 생기는 현상이 가능하다. 호스트는 이 왕복 경로가 없어 유리하다. 플레이어끼리 및 적과 플레이어의 물리 상호작용도 서로 다른 권한의 복제 객체를 대상으로 하므로 동일 결과를 보장하지 않는다.

## 4. 핑 튐/끊김 후보 — 확정 사실과 가설 구분

### 높은 우선순위

1. **발사 왕복 대기(코드 확인).** 클라이언트는 소리만 즉시 재생하고 실제 탄환은 서버 스폰이 돌아와야 표시된다. 예측 탄환, 발사 시각/시퀀스, 서버 보정, 지연 보상 코드가 없다. 체감 발사 지연은 대략 왕복 전송 + 서버/네트워크 틱 대기 + 렌더링/보간이며 실제 수치는 측정해야 한다. 발사 요청에는 방향만 있어 서버가 보는 지연된 발사 위치를 사용한다.
2. **호스트 CPU/GC 부담(코드 확인, 영향량 미측정).** EnemyAI는 적 하나마다 매 FixedUpdate에 FindObjectsByType로 전체 플레이어를 검색한다. 적 수와 물리 스텝 수에 비례해 전역 검색과 배열 생성이 반복된다. 호스트 프레임 정지는 물리와 네트워크 처리를 함께 늦출 수 있다. 플레이어 등록 목록 캐시와 타겟 검색 주기 분리가 우선 후보다.
3. **풀링의 파괴 호출(코드·패키지 구현 확인).** NetworkObjectPool.Despawn은 `instance.Despawn(true)` 후 같은 인스턴스를 큐에 넣는다. NGO에서 true는 GameObject 파괴 의미이며 단순히 클라이언트 제거 여부가 아니다. 별도 PrefabHandler 등록도 현재 풀 코드에 없다. 파괴된 항목은 다음 Spawn에서 건너뛰고 Instantiate하므로 반복 재사용 효과를 잃고 객체 생명주기 문제도 생길 수 있다. 서버 재사용은 Despawn(false) 설계, 클라이언트 재사용은 INetworkPrefabInstanceHandler까지 함께 검토해야 한다.
4. **적 Transform GUID 불일치(정적 확인).** 앞 절의 Missing Script 여부부터 확인해야 한다. 보간 튜닝 전에 위치 동기화가 실제로 존재하는지 검증한다.

### 추가 후보

- 플레이어/적/탄환 설정에 Interpolate=1, UseUnreliableDeltas=0, PositionThreshold=0.001, XYZ 위치 및 XYZ 회전 동기화가 보인다. 높은 업데이트 빈도와 reliable 전달은 손실 시 재전송 대기를 늘릴 가능성이 있다. unreliable delta 및 XY/필요 회전만 전송하는 변경은 패킷량·손실 조건 비교 후 결정한다. 0.1초 MaxInterpolationTime은 모든 객체에 항상 100ms 지연이 붙는다는 뜻은 아니다.
- SampleScene 네트워크 TickRate=30(약 33.3ms), Fixed Timestep=0.02(50Hz), Rigidbody 보간=0. 틱/물리/렌더링 주기가 달라 업데이트 간격이 고르지 않을 수 있다. 이것만으로 RTT 튐 원인이라고 단정할 수 없으며, 틱을 무조건 높이면 호스트 부하·대역폭이 증가한다.
- 탄환 및 피격 이펙트마다 NetworkObject 스폰/디스폰을 사용한다. 단순 표시 효과를 로컬 VFX로 바꾸는 방안과 탄환 스폰 비용을 프로파일링한다.
- Relay 할당은 region을 명시하지 않는다. 실제 할당 리전과 양쪽 접속 환경을 기록하고 LAN 직결과 Relay를 비교해야 경로 지연을 분리할 수 있다. DTLS 사용 자체를 원인으로 단정하지 않는다.
- 재시작은 서버/소유자 양쪽에서 transform.position을 직접 변경한다. 비행 중 위치 업데이트와 섞이는지 확인하고 권한 측 teleport/reset 절차로 통일할 필요가 있다.
- FireServerRpc에는 서버 측 발사 주기·다운 상태·게임 진행 상태 검증이 없다. 로컬 쿨다운만 존재하므로 요청 남발이 서버 스폰 부하를 만들 수 있다. 일반 지연 문제와 별개인 검증 누락이다.

## 5. 기술보고서 수령 후 적용 순서

이번 변경은 CLI 패키지 설치와 분석 문서다. 게임플레이와 권한 구조는 아직 수정하지 않았다.

1. EnemyNet Missing Script 확인, Pipeline 연결 완료, 현재 동작의 기준 측정.
2. 같은 적 수/발사량에서 호스트·클라이언트 프레임 시간/GC 할당, RTT p50/p95/p99, 패킷 손실, 초당 송수신량, 입력→이동/탄환 표시 시간을 각각 기록. RTT와 체감 지연을 분리한다.
3. LAN 직결 / Relay, 호스트·클라이언트 역할 교대, 빌드 2개 / Multiplayer Play Mode를 비교한다. 현재 씬의 DebugSimulator 지연·지터·드롭은 모두 0이다.
4. 보고서와 대조해 풀링 수명주기와 타겟 검색 부하를 먼저 수정하고 동일 조건으로 재측정한다.
5. 발사 시각·시퀀스 기반 예측 표시/중복 제거, 서버 요청 검증을 적용한다. 이후 필요하면 이동을 서버 권위 + 입력 예측/재조정으로 전환한다. AuthorityMode만 서버로 바꾸면 현재 IsOwner 이동 코드와 충돌하므로 함께 설계해야 한다.
6. 0/50/100/200ms RTT 목표 조건과 지터·손실 조건을 실제 측정값으로 확인하고 이동/발사/피격/다운/부활/재시작 회귀를 검증한다. Transport simulator의 단방향 지연값을 RTT로 오인하지 않는다.

현재는 2인 플레이 재현 및 RTT/Profiler 수집을 하지 않았다. 위 원인들은 코드로 확인한 구조적 문제와 검증할 가설이며, 핑 급증의 실측 원인 확정이나 해결 완료를 의미하지 않는다.
