# 테스트 자동 생성 입력과 모델 설정

Testing.Shared의 Generator가 Hell's Kitchen 시험을 만들 때 읽는 입력 파일 모음.

| 파일 | 내용 |
|---|---|
| requirements.json | 요구사항 95건 |
| context_sections.json | 명세 4~6장 본문. 프롬프트의 근거 텍스트 |
| seams.json | 요구사항별 분류 A, B1, B2, B3, C와 시험 대상 클래스 |
| api_catalog.json | 모델이 쓸 수 있는 클래스, 메서드, 주입 가능 필드 전체 |
| test_patterns.json | 시험 패턴 P1~P9 |
| baseline_tc.json | 수기 TC 24건의 JSON 변환본. 일치율 비교용 |
| tc_design.schema.json, exec_plan.schema.json | 1단계, 2단계 출력 형식 |
| models.json | 모델별 프로필. 프로필 이름은 모델 ID이고 환율, 단가, 키 환경 변수 이름을 담음. 워크플로의 모델 선택 목록과 일치해야 함 |

## GitHub Actions 실행

`.github/workflows/generate-tests.yml`이 생성기를 빌드하고 모델을 호출한다. 수동 실행 전용.

1. Settings, Secrets and variables, Actions에 키 등록: `ANTHROPIC_API_KEY`, `GEMINI_API_KEY`, `DEEPSEEK_API_KEY`. `SHARED_REPO_TOKEN`은 CI와 공용.
2. Settings, Actions, General에서 Actions의 PR 생성 허용 체크.
3. Actions 탭, Generate tests, Run workflow에서 회사를 고르고 해당 회사의 모델 칸에서 모델 선택. 다른 회사의 모델 칸은 무시됨. 요구사항 ID는 쉼표로 구분해 직접 입력. Testing.Shared는 항상 main을 사용. GitHub가 선택에 따라 바뀌는 목록을 지원하지 않아 회사별로 칸을 나눔.
4. 실행 순서는 연결 확인, 예상 비용, 시험 코드 생성, 요약. 요약은 실행 화면의 Summary에, 결과 파일은 Artifacts에 남음.
5. PR 생성을 켜면 `gen/<모델>-<실행번호>-<시도번호>` 브랜치와 PR이 만들어지고 CI가 함께 실행됨. 통과율이 모델 비교 자료. PR은 병합하지 않음.
6. 회사를 local로 고르면 self-hosted 실행기에서 실행. LLM이 도는 PC에 실행기를 등록해야 함. 주소와 모델 이름은 `models.json`의 `local` 항목에서 수정. 그 외 회사는 GitHub 호스트 실행기를 사용.

키는 Secrets에만 두고 로그에는 나오지 않음.

## PC에서 직접 실행

키는 파일에 적지 않고 환경 변수로만 둔다. PowerShell에서 현재 창에만 적용하는 예시.

    $env:ANTHROPIC_API_KEY = "여기에 키"

Testing.Shared 폴더에서 실행.

    dotnet publish Tools~\GeneratorCli -c Release -o out\cli
    out\cli\Testing.Shared.Generator.Cli.exe doctor   --models <HK>\Tools\generator\models.json --profile claude-haiku-5-5
    out\cli\Testing.Shared.Generator.Cli.exe estimate --models <HK>\Tools\generator\models.json --data <HK>\Tools\generator --profile claude-haiku-5-5
    out\cli\Testing.Shared.Generator.Cli.exe generate --models <HK>\Tools\generator\models.json --data <HK>\Tools\generator --profile claude-haiku-5-5 --budget-krw 500 --out <작업 폴더>\gen_out

- doctor: 키, 주소, 모델 확인 후 최소 호출 1번. 사용량과 비용을 원화로 표시.
- estimate: 호출 없이 예상 비용만 원화로 계산.
- generate: `--reqs`를 생략하면 파일럿 6건 FR-FRESH-001, FR-FRESH-002, FR-COOK-003, FR-DROP-017, FR-INT-007, FR-FRESH-005 생성.
- `--budget-krw` 초과가 예상되면 호출 전에 중단하고 그때까지의 결과 보존.
- 결과는 `<out>\<모델>\` 아래에 1단계 JSON, 2단계 JSON, `GeneratedTests_<모델>.cs`, `summary.md`, `calls.jsonl`, `raw\`로 저장. `raw\`는 요청과 응답 원문이며 키는 없음.

## 로컬 모델

`local` 항목은 주소와 모델 이름만 사용. Ollama 등 서버 종류와 무관하게 같은 방식으로 연결. `baseUrl`과 `model`을 사용 중인 서버에 맞게 수정. 키 불필요.

## 단가 없는 모델

`price`가 없는 모델은 예산 상한이 적용되지 않아 기본으로 실행을 거부함. 쓰려면 워크플로의 단가 없는 모델 허용을 켜거나 CLI에 `--allow-unpriced true` 지정. 단가가 공개되면 `price`를 채움.

## 주의

- 생각 설정은 `extraBody`에 있음. 서버가 거부하면 doctor가 오류를 그대로 표시.
- 생성된 `.cs`는 사람이 검토하기 전까지 병합하지 않음.
