# 클라이언트 스폰 문제 해결 가이드

## 증상
- ✅ 호스트는 플레이어가 정상적으로 나타남
- ❌ 클라이언트는 접속은 되지만 플레이어가 보이지 않음
- ❌ "Join failed: Object reference not set to an instance of an object" 에러 발생

---

## 주요 원인 및 해결 방법

### 1. ⚠️ NetworkManager가 씬에 없거나 비활성화됨 (가장 흔한 원인)

**증상:**
```
Join failed: NetworkManager.Singleton is null.
Make sure there is a NetworkManager in the scene and it is active.
```

**해결 방법:**

#### 1-1. SampleScene에 NetworkManager가 있는지 확인
1. Unity Editor에서 **SampleScene** 열기
2. Hierarchy에서 **NetworkManager** 오브젝트 찾기
3. 없다면:
   - GameObject → Create Empty 클릭
   - 이름을 "NetworkManager"로 변경
   - Add Component → **NetworkManager** 추가
   - Add Component → **UnityTransport** 추가

#### 1-2. NetworkManager가 활성화되어 있는지 확인
1. Hierarchy에서 NetworkManager 선택
2. Inspector 상단의 체크박스가 **체크되어 있는지** 확인 (비활성화되어 있으면 클라이언트가 작동하지 않음)

#### 1-3. NetworkManager 설정 확인
NetworkManager의 Inspector에서:
- **NetworkConfig** → **PlayerPrefab**이 설정되어 있는지 확인 (보통 `PlayerPrefab`이라는 이름의 프리팹)
- **NetworkConfig** → **Network Prefabs** 리스트에 필요한 프리팹이 등록되어 있는지 확인

---

### 2. 🔧 Unity Services 초기화 실패

**증상:**
- "MultiplayerService.Instance is null" 같은 에러
- Unity Cloud Project가 연결되지 않음

**해결 방법:**

1. **Edit** → **Project Settings** → **Services** 클릭
2. Unity 계정으로 로그인되어 있는지 확인
3. Project가 Cloud Project와 연결되어 있는지 확인
4. 연결되지 않았다면:
   - "Create Unity Cloud Project" 버튼 클릭
   - 또는 기존 프로젝트 선택

---

### 3. 🎮 플레이 모드 문제 (Editor 내 테스트 시)

**증상:**
- Host는 되는데 Client는 안 됨 (같은 Editor 내에서 테스트할 때)

**해결 방법:**

#### 방법 1: Multiplayer Play Mode 사용 (권장)
1. **Window** → **Package Manager** 열기
2. 왼쪽 상단 "+" 클릭 → "Add package by name"
3. `com.unity.multiplayer.playmode` 입력 후 설치
4. **Window** → **Multiplayer Play Mode** 열기
5. "Enable Multiplayer Play Mode" 체크
6. Virtual Players를 2로 설정
7. 플레이 모드 실행

#### 방법 2: 빌드로 테스트 (가장 확실함)
1. **File** → **Build Settings**
2. **Build**를 눌러 실행 파일 생성
3. 생성된 실행 파일을 2개 실행:
   - 첫 번째: Host로 실행
   - 두 번째: Client로 Join

---

### 4. 🌐 씬 동기화 문제

**증상:**
- Host와 Client가 다른 씬에 있어서 서로 보이지 않음

**해결 방법:**

1. NetworkManager 선택
2. Inspector에서 **Enable Scene Management** 체크 확인
3. Host와 Client가 **같은 씬에서 시작하는지** 확인 (보통 SampleScene)

---

### 5. 🎯 Player Prefab 문제

**증상:**
- 접속은 되지만 플레이어가 스폰되지 않음

**해결 방법:**

1. **Assets/Prefabs** 폴더에 PlayerPrefab이 있는지 확인
2. PlayerPrefab에 다음 컴포넌트가 있는지 확인:
   - `NetworkObject` (필수)
   - `PlayerController` 또는 유사한 플레이어 컨트롤러
3. NetworkManager의 **Player Prefab** 필드에 해당 프리팹이 할당되어 있는지 확인

---

## 체크리스트 (순서대로 확인)

1. [ ] SampleScene에 NetworkManager 오브젝트가 있는가?
2. [ ] NetworkManager가 **활성화**(체크박스 체크)되어 있는가?
3. [ ] NetworkManager의 Player Prefab이 설정되어 있는가?
4. [ ] Unity Services (Cloud)가 연결되어 있는가?
5. [ ] Multiplayer Play Mode를 사용하거나 별도 빌드로 테스트하고 있는가?
6. [ ] Host와 Client가 같은 Room Code를 사용하고 있는가?
7. [ ] Console에 "Client connected: [번호]" 로그가 나타나는가?

---

## 디버깅 팁

### Console 로그 확인
다음 로그가 나타나는지 확인:

**정상적인 경우:**
```
Client connected: 0
Client connected: 1
```

**문제가 있는 경우:**
```
Join failed: NetworkManager.Singleton is null.
Make sure there is a NetworkManager in the scene and it is active.
```

### 추가 로그 활성화
NetworkManager 선택 → **Log Level**을 "Developer"로 설정하면 더 자세한 로그를 볼 수 있습니다.

---

## 여전히 안 된다면?

1. Unity Editor 재시작
2. Library 폴더 삭제 후 프로젝트 재열기
3. 빌드로 테스트 (Editor 내 테스트보다 안정적)
4. Console에 나타나는 정확한 에러 메시지를 확인하고 해당 메시지로 검색

---

## 참고 자료

- [Unity Netcode for GameObjects 공식 문서](https://docs-multiplayer.unity3d.com/)
- [Unity Multiplayer Play Mode 가이드](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@1.0/manual/index.html)
