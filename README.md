# LeagueOfPhysical-Shared (com.baegames.lop.shared)

League of Physical 클라/서버가 공유 사용하는 도메인 Unity 패키지.

## 책임

- proto 산출물 (wire 메시지 클래스)
- 메시지 인프라 (MessageFactory, MessageHandler<T>, MessageIds, MessageInitializer)
- (Slice 2+) MasterData 스키마/로더
- (Slice 4+) `LOPGameSimulation` — 결정론 시뮬 코어

## Use-side Requirements

이 패키지에 의존하는 프로젝트는 다음을 제공해야 한다:

- `com.baegames.gameframework` (이 패키지의 `dependencies`에 선언됨)
- `org.nuget.google.protobuf` 3.28.x (UnityNuGet scoped registry로 설치)
- Mirror (Asset Store / git URL)
- (이후 슬라이스에서 사용 시) R3, VContainer, UniTask

상세 토폴로지: 사용 측 저장소의 `docs/lop-repo-topology.md` 참조.

## Editing

패키지는 use-side `Packages/manifest.json`에서 `file:` 참조로 들어와 있다. 이 폴더 안에서 직접 편집·커밋·push.

## Codegen

`.proto` 정의는 `Protos/`, 도구는 `Tools/Protobuf/`, 스크립트는 `Scripts/`. 새 메시지 추가 시:
1. `Protos/*.proto` 수정
2. `Scripts/`에서 `./generate_protos.sh`
3. 산출물이 `Runtime.Generated/Scripts/`에 생성
4. 사용 측 Unity가 자동 reimport
