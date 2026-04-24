# Coding Convention

## 🌿 1. 브랜치 생성 규칙 

브랜치는 역할에 맞게 명확하게 분리한다.

### 📍 브랜치 구조
- main        → 배포용 (직접 작업 금지)
- develop     → 개발 통합 브랜치
- feature/*   → 기능 개발
- fix/*       → 버그 수정

### 📍 브랜치 예시
- feature/player-movement
- feature/inventory-system
- fix/enemy-ai-error

### 📍 규칙
- main 브랜치 직접 작업 ❌
- 모든 작업은 develop 기준으로 진행
- 작업 완료 후 Merge 진행
- 사용한 브랜치는 삭제 후 필요 시 재생성

---

## 🧠 2. 네이밍 규칙 (필수)

일관성과 가독성을 최우선으로 한다.

### 📍 네이밍 규칙 표

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스 | PascalCase | `CoinSpawner`, `AssetManager` |
| 메서드 | PascalCase | `SpawnCoin()`, `CalculateDecay()` |
| 공개 프로퍼티 | PascalCase | `CurrentCash`, `FamePoint` |
| 비공개 필드 | _camelCase | `_spawnInterval`, `_comboCount` |
| 지역 변수 | camelCase | `decayValue`, `bounceCount` |
| 상수 | UPPER_SNAKE_CASE | `MAX_COMBO_COUNT`, `BASE_DECAY_RATE` |
| 인터페이스 | I + PascalCase | `IPoolable`, `IInvestable` |
| ScriptableObject | PascalCase + SO | `CoinDataSO`, `TierDataSO` |
| 이벤트 | On + PascalCase | `OnCoinCollected`, `OnTierChanged` |
### 접근 제한자
- 모든 필드/메서드에 접근 제한자를 **명시**한다. (생략 금지)
- Unity Inspector에 노출할 필드는 `private` + `[SerializeField]` 사용. `public` 필드 직접 노출 금지.
- `public`으로 열어야 할 경우 **property** 로 열 것.

```
// 좋은 예
[SerializeField] private float _spawnInterval;

// 나쁜 예
public float spawnInterval;
```
---

## ✨ 3. Commit 메시지 규칙

- Commit 은 되도록 작은 단위로 할 것.
- Commit 메시지는 상세히 적을 것. (한줄로 적을 필요는 없음)
- 문서 작성, 간단 수정 등도 적절히 메시지를 넣을 것.
- 말머리 형식
  - 기본적으로 `단어:` 형식
    - 기능 추가: `feat:`
    - 버그 수정: `bugfix:` or `fix:`
    - Refactorying: `refac:`
    - 문서: `doc:`
    - 기타: `etc:`

---

## 🔀 4. Merge & Pull Request 규칙 

PR은 작업 내용을 명확하게 전달하는 문서다.

### 📍 제목 형식
- [기능] 캐릭터 선택 시스템 추가
- [버그] 적 공격 안하는 문제 수정

### 📍 내용 작성 예시
- Photon 동기화 구현
- 캐릭터 선택 UI 추가
- 100초 타이머 기능 구현

### 📍 규칙
- 무엇을 했는지 명확하게 작성
- 간결하게 작성
- 코드 확인 없이도 이해 가능하게 작성

---