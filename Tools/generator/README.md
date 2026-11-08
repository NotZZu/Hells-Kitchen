# 테스트 자동 생성 입력과 모델 설정

이 폴더는 Testing.Shared의 Generator가 Hell's Kitchen용 시험을 만들 때 읽는 입력 파일이다.

| 파일 | 내용 |
|---|---|
| requirements.json | 요구사항 95건 |
| context_sections.json | 명세 §4~§6 본문(프롬프트에 넣는 근거 텍스트) |
| seams.json | 요구사항별 분류(A, B1, B2, B3, C)와 시험 대상 클래스 |
| api_catalog.json | 모델이 쓸 수 있는 클래스, 메서드, 주입 가능 필드의 전부 |
| test_patterns.json | 시험 패턴 P1~P9 |
| baseline_tc.json | 수기 TC 24건을 JSON으로 옮긴 것(일치율 비교용) |
| tc_design.schema.json, exec_plan.schema.json | 1단계, 2단계 출력 형식 |
| models.json | 모델 프로필(환율, 단가, 키를 담을 환경 변수 이름) |

## GitHub Actions에서 실행(기본 방법)

`.github/workflows/generate-tests.yml`이 생성기를 빌드하고 모델을 호출한다. 수동 실행 전용이다.

1. Settings > Secrets and variables > Actions에 쓰려는 키를 등록한다: `ANTHROPIC_API_KEY`, `GEMINI_API_KEY`, `DEEPSEEK_API_KEY`. `SHARED_REPO_TOKEN`은 CI와 같은 것을 쓴다.
2. Actions 탭 > "Generate tests (LLM)" > Run workflow에서 프로필, 요구사항(비우면 파일럿 6건), 호출 단위, 예산 상한(원)을 정한다.
3. 실행 순서는 연결 확인(doctor), 예상 비용, 생성, 요약이다. 요약 표와 결과 파일(1·2단계 JSON, `.cs`, `calls.jsonl`)은 실행 화면의 Summary와 Artifacts에 남는다.
4. "결과를 브랜치와 PR로 올리고 EditMode 시험도 돌린다"를 켜 두면 `gen/<프로필>-<실행번호>` 브랜치와 PR이 만들어지고 CI가 같이 돈다. 통과율이 비교 자료다. PR은 통째로 병합하지 않는다.
5. 로컬 모델은 LLM이 도는 PC에 self-hosted 실행기를 등록하고, 실행기 라벨(runner 입력)에 그 라벨을 준다. 주소와 모델 이름은 아래 `models.json`의 `local` 프로필에서 고친다.

키 값은 로그에 나오지 않는다. Secrets에만 둔다.

## PC에서 직접 실행(확인용)

### 키 설정

키는 파일에 적지 않고 환경 변수로만 둔다. PowerShell에서 현재 창에만 적용하는 예시는 다음과 같다.

    $env:ANTHROPIC_API_KEY = "..."
    $env:GEMINI_API_KEY    = "..."
    $env:DEEPSEEK_API_KEY  = "..."

## 실행(Testing.Shared 폴더에서)

    dotnet build Tools~\GeneratorCli -c Release -o out\cli
    out\cli\Testing.Shared.Generator.Cli.exe doctor   --models <HK>\Tools\generator\models.json --profile haiku
    out\cli\Testing.Shared.Generator.Cli.exe estimate --models <HK>\Tools\generator\models.json --data <HK>\Tools\generator
    out\cli\Testing.Shared.Generator.Cli.exe generate --models <HK>\Tools\generator\models.json --data <HK>\Tools\generator --profile haiku --budget-krw 500 --out <작업 폴더>\gen_out

- `doctor`는 키 환경 변수, 주소, 모델을 확인하고 최소 호출 1번을 보낸다. 사용량과 비용(원)을 보여 준다.
- `estimate`는 호출하지 않고 예상 비용(원)만 계산한다. 출력 토큰은 가정치다.
- `generate`는 `--reqs`를 생략하면 파일럿 6건(FR-FRESH-001, 002, FR-COOK-003, FR-DROP-017, FR-INT-007, FR-FRESH-005)을 만든다.
- `--budget-krw`를 넘길 것 같으면 호출 전에 멈추고, 그때까지의 결과는 남긴다.
- 결과는 `<out>\<프로필>\` 아래에 1단계 JSON, 2단계 JSON, `GeneratedTests_<프로필>.cs`, `summary.md`, `calls.jsonl`, `raw\`(요청·응답 원문, 키 없음)로 저장된다.

## 로컬 모델

`local` 프로필은 주소와 모델 이름만 본다. Ollama든 다른 서버든 같은 방식으로 연결한다. 주소(`baseUrl`)와 모델 이름(`model`)을 사용 중인 서버에 맞게 고친다. 키는 필요 없다.

## 주의

- `gemini-3-8-flash`는 단가를 확인하지 못해 `price`가 비어 있다. 첫 `doctor` 결과의 사용량과 Google 가격 페이지를 보고 채운다.
- 생각(thinking) 설정은 `extraBody`에 있다. 서버가 거부하면 `doctor`가 오류를 그대로 보여 준다.
- 생성된 `.cs`는 사람이 검토하기 전에는 병합하지 않는다.
