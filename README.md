Nine Chronicles
===============

[![CircleCI][ci-badge]][ci]

[ci-badge]: https://circleci.com/gh/planetarium/nekoyume-unity.svg?style=svg&circle-token=ca79d4f6281fe60cdde55d0f1c3d97d561106bda
[ci]: https://circleci.com/gh/planetarium/nekoyume-unity


### 의존성
 - [Unity Hub]


### 설치 방법

 1. [Unity Hub] 설치
 1. Unity 2019.1.0f2 버전 설치
 1. 저장소 클론
    ```
    $ git clone git@github.com:planetarium/nekoyume-unity.git
    ```
 1. 유니티 실행후 프로젝트 빌드


### 커맨드라인 옵션

 - `--private-key` : 사용할 프라이빗 키를 지정합니다.
 - `--keystore-path`: 비밀키가 저장될 디렉터리 경로.
 - `--host`        : 사용할 Host 이름을 지정합니다.
 - `--port`        : 사용할 Port를 지정합니다.
 - `--no-miner`    : 마이닝을 사용하지 않습니다.
 - `--peer`        : Peer 를 추가합니다. 추가하려는 Peer가 여럿일 경우 --peer peerA peerB ... 와 같이 추가할 수 있습니다.
 - `--ice-servers` : NAT 우회에 사용할 TURN 서버 정보를 지정합니다. 지정하는 서버가 여럿일 경우 `--ice-servers serverA serverB` 와 같이 추가할 수 있습니다.
 - `--storage-path`: 데이터를 저장할 경로를 지정합니다.
 - `--storage-type`: 데이터를 저장할 저장소 타입을 지정합니다. 현재는 `--storage-type rocksdb` 로 `RocksDBStore` 를 지정할 수 있습니다.
 - `--client`      : 체인 데이터를 저장하지 않는 클라이언트 모드로 실행합니다.
 - `--client-host` : 클라이언트 모드에서 접속할 서버의 호스트명을 지정합니다.
 - `--client-port` : 클라이언트 모드에서 접속할 서버의 포트를 지정합니다.
 - `--auto-play`   : 백그라운드에서 캐릭터 생성 및 자동 전투를 수행합니다.
 - `--console-sink`: 로그를 콘솔로 출력합니다.
 - `--development` : 개발 모드로 실행합니다. 디버그용 UI를 표시하고 로그 레벨을 조정합니다.

#### Unity Editor 에서 커맨드라인 옵션 사용

위의 커맨드라인 옵션을 Unity 에디터나 빌드한 플레이어에서 사용하려면 `Assets/StreamingAssets/clo.json` 파일을 작성하면 됩니다. 아래는 작성 예시입니다.

```
{
   "privateKey": "",
   "host": "127.0.0.1",
   "port": 5555,
   "noMiner": true,
   "peers": ["02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1,nekoyume1.koreacentral.cloudapp.azure.com,58598"]
}
```

- `Assets/StreamingAssets/clo.json` 파일은 버전 관리에서 제외되어 있습니다.
  - 필요에 따라 `Assets/StreamingAssets/clo_nekoalpha_nominer.json` 와 같이 프리셋을 제공하는 경우가 있습니다. 이러한 프리셋 파일을 `clo.json`으로 이름을 바꾸면 바로 사용하실 수 있습니다.


### 커맨드라인 빌드

```
$ /UnityPath/Unity -quit -batchmode -projectPath=/path/to/nekoyume/ -executeMethod Editor.Builder.Build[All, MacOS, Windows, Linux, MacOSHeadless, WindowsHeadless, LinuxHeadless]
```

- Example

```
$ /Applications/Unity/Hub/Editor/2019.1.0f2/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath=~/planetarium/nekoyume-unity/nekoyume/ -executeMethod Editor.Builder.BuildAll
```

### 피어 설정

#### 읽기 순서

통신을 하기 위한 피어 목록은 다음과 같은 순서로 로드합니다.  

1. 실행 시 커맨드라인 인자 (`--peer`)
2. (Windows 기준) `%USERPROFILE%\AppData\LocalLow\Planetarium` 에 있는 `peers.dat`
3. 9C 프로젝트의 `Assets\Resources\Config\peers.txt`

현재 프로젝트에선 기본 피어 설정(3번)이 비어있으므로, 1,2번에 별도의 피어 설정이 되어있지 않다면 싱글 노드로 동작합니다.

#### 형식

피어 목록은 평문(Plain Text) 형식으로 저장되며 한 줄마다 한 노드의 정보를 `공개키,호스트명,포트,버전` 형태로 적습니다.

예시) 

```
   02ed49dbe0f2c34d9dff8335d6dd9097f7a3ef17dfb5f048382eebc7f451a50aa1,nekoyume1.koreacentral.cloudapp.azure.com,58598
   02d05be62f8593721f5abfd28fb83c043ed9d9585f45b652cb67fd6eee3fd3748f,nekoyume2.koreacentral.cloudapp.azure.com,58599
```

- 호스트명과 포트는 외부에서 접속 가능한 것이어야 합니다.
    - 실행시 인자(`—host`)를 지정하지 않은 경우에는 자동으로 STUN/TURN에 의해 릴레이 되므로 실제 호스트명과 포트가 달라질 수 있습니다. 
      즉, 피어 목록에 기술되는 노드는 반드시 `--host`를 통해 호스트명을 지정하여 실행되어야 합니다.
- 공개키는 `Swarm` 객체 생성시 사용한 개인키(`PrivateKey`)로부터 유도된 것을 16진수로 부호화한 것입니다.

[Unity Hub]: https://unity3d.com/get-unity/download


### Docker-compose 마이너 테스트

로컬에서 테스트 하는 것을 전제로 Seed 의 개인키와 노드들의 Host가 하드코딩 되어 있습니다.

- `LinuxHeadless` 빌드 후 아래 명령 실행

```bash
cd nekoyume/compose
docker-compose up --build
```

### 오토 플레이 옵션

`--auto-play` 옵션으로 백그라운드에서 캐릭터를 생성하고 자동전투를 수행하도록 할 수 있습니다.
현재 캐릭터의 이름은 노드의 Address 앞 여덟자리로 생성되며 `TxProcessInterval` 마다 1 스테이지 전투를 반복합니다.

### console sink 옵션

`--console-sink` 옵션으로 로그를 ApplicationInsights로 보내지 않고 `UnityDebugSink`를 통하여 내보내게 할 수 있습니다.
