# Nocturne 확장 가이드

[English](../en/extensions.md)

Nocturne 확장은 Nocturne 프로세스 안에서 실행되는 .NET DLL입니다. 확장은
`INocturneExtension`을 구현하고 초기화 과정에서 하나 이상의 슬래시 명령을
등록합니다.

> 신뢰할 수 있는 확장만 설치하세요. 확장은 샌드박스 안에서 실행되지
> 않으며, Nocturne과 같은 파일 시스템, 네트워크, 환경 변수, 프로세스
> 권한을 가집니다.

## 요구 사항

- Windows
- .NET 10 SDK
- Nocturne 소스 체크아웃

현재 확장 계약은 별도로 버전이 관리되는 NuGet SDK가 아니라 Nocturne
애플리케이션 어셈블리에 포함되어 있습니다. `net10.0-windows`를 대상으로
Nocturne 프로젝트를 참조하고, 호스트가 업데이트되면 확장을 다시 빌드하고
테스트하세요.

## 확장 만들기

다음 예제는 Nocturne 소스를 체크아웃한 루트에서 `Nocturne` 프로젝트
디렉터리와 나란히 `HelloNocturne`을 만든다고 가정합니다.

클래스 라이브러리를 만듭니다.

```powershell
dotnet new classlib -n HelloNocturne -f net10.0
```

`HelloNocturne/HelloNocturne.csproj`를 다음 내용으로 바꿉니다.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Version>1.0.0</Version>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference
      Include="..\Nocturne\Nocturne.csproj"
      Private="false" />
  </ItemGroup>
</Project>
```

`Class1.cs`를 다음 내용으로 바꿉니다.

```csharp
using Nocturne;
using Nocturne.Extensions;

namespace HelloNocturne;

public sealed class HelloExtension : INocturneExtension
{
    public string Name => "Hello Nocturne";

    public string Description => "친근한 /hello 명령을 추가합니다.";

    public void Initialize(ExtensionContext context)
    {
        context.RegisterCommand(
            "hello",
            "인사말과 현재 디렉터리를 표시합니다.",
            (args, shell) =>
            {
                string target = string.Join(' ', args).Trim();
                if (target.Length == 0)
                {
                    target = Environment.UserName;
                }

                Console.WriteLine($"안녕하세요, {target}!");
                Console.WriteLine($"현재 디렉터리: {shell.Cwd}");
            });
    }

    public void Shutdown()
    {
        // 확장이 소유한 리소스를 여기에서 정리합니다.
    }
}
```

확장 클래스는 추상 클래스가 아니어야 하며 public 매개 변수 없는 생성자를
가져야 합니다. 예제처럼 암시적으로 생성되는 생성자도 사용할 수 있습니다.

확장을 빌드합니다.

```powershell
dotnet build HelloNocturne/HelloNocturne.csproj -c Release
```

확장 DLL은 다음 위치에 생성됩니다.

```text
HelloNocturne\bin\Release\net10.0-windows\HelloNocturne.dll
```

## 확장 설치와 관리

Nocturne 안에서 다음 명령을 실행합니다.

```text
/extension install "C:\path\to\HelloNocturne.dll"
/extension list
/hello Nocturne
/extension remove "Hello Nocturne"
```

`/extension uninstall <이름>`은 `remove`의 별칭입니다. 상대 설치 경로는
현재 셸 디렉터리를 기준으로 해석됩니다. 작업 없이 `/extension`만 실행하면
대화형 관리 화면이 열립니다.

설치할 때 Nocturne은 다음 작업을 수행합니다.

1. 선택한 파일이 관리 DLL인지 검사합니다.
2. DLL을 `%USERPROFILE%\nocturne_extensions`로 복사합니다.
3. DLL에 포함된 모든 확장 형식을 로드하고 초기화합니다.
4. 등록된 명령을 즉시 사용할 수 있게 합니다.

다시 시작할 필요는 없습니다. 이후 Nocturne을 시작하면 확장 디렉터리
최상위에 있는 모든 DLL을 자동으로 불러옵니다. 확장 DLL을 해당 디렉터리에
직접 복사하고 Nocturne을 다시 시작하는 방식으로도 설치할 수 있습니다.

Nocturne은 같은 파일 이름으로 설치된 DLL을 덮어쓰지 않습니다. 새 버전을
설치하기 전에 기존 확장을 제거하세요.

## 확장 API

### `INocturneExtension`

| 멤버 | 용도 |
| --- | --- |
| `string Name` | 필수인 비어 있지 않은 이름. `/extension list`와 `/help`에 표시 |
| `string Description` | 선택 설명. 기본값은 빈 문자열 |
| `Initialize(ExtensionContext context)` | 확장을 불러올 때 한 번 호출 |
| `Shutdown()` | 패키지를 언로드할 때 호출되는 선택적 정리 콜백 |

표시되는 버전은 DLL의 정보 버전, 어셈블리 버전 순으로 결정됩니다. 둘 다
없으면 `unknown`을 표시합니다.

### `ExtensionContext`

| 멤버 | 용도 |
| --- | --- |
| `ExtensionDirectory` | 설치된 DLL이 있는 절대 디렉터리 |
| `ExtensionName` | 현재 확장의 `Name` |
| `RegisterCommand(name, description, execute)` | 현재 확장의 명령으로 등록 |
| `RegisterCommand(name, description, from, execute)` | 출처 표시를 직접 지정해 명령 등록 |

명령 이름 앞의 `/`는 생략할 수 있습니다. 이름은 대소문자를 구분하지 않고,
공백을 포함할 수 없으며, 기본 명령이나 다른 확장 명령과 중복될 수 없습니다.

콜백은 `string[] args`와 현재 `Shell`을 전달받습니다. 현재 작업 디렉터리는
`shell.Cwd`로 읽을 수 있습니다. 현재 명령 디스패처는 인수를 공백으로
나누며 따옴표나 옵션을 자동으로 해석하지 않으므로, 필요한 경우 확장에서
인수를 정리하거나 직접 파싱해야 합니다.

등록한 명령은 확장을 언로드할 때 자동으로 제거됩니다. 명령 콜백에서 발생한
예외는 셸이 보고하며, 메인 루프는 종료되지 않습니다.

## 수명 주기와 패키징

하나의 DLL에 `INocturneExtension` 구현을 여러 개 넣을 수 있습니다.
Nocturne은 모든 구현을 하나의 패키지로 생성하고 초기화합니다. 구현들은 DLL
버전과 파일 경로를 공유하며, 그중 하나를 제거하면 DLL 전체가 언로드되고
삭제됩니다.

초기화는 패키지 단위로 처리됩니다. 확장 형식 하나라도 초기화에 실패하면
Nocturne은 같은 DLL에서 앞서 초기화한 형식을 종료하고 명령을 등록 해제한 뒤
패키지를 거부합니다. 설치 중 실패한 DLL은 확장 디렉터리에서도 제거됩니다.

로드된 패키지를 제거하거나 Nocturne이 정상 종료되면 등록된 명령을 해제하고,
확장 인스턴스의 `Shutdown()`을 호출한 뒤 수집 가능한 로드 컨텍스트를
언로드합니다.

### 의존성

현재 설치 관리자는 선택한 확장 DLL 하나만 복사하는 반면, 시작 스캐너는
확장 디렉터리 최상위의 모든 DLL을 확장 후보로 취급합니다. 문제없이 설치할
수 있도록 별도의 전용 DLL 의존성이 없는 단일 확장 어셈블리로 배포하세요.
Nocturne 자체에 대한 참조는 호스트가 제공합니다.

추가 관리 또는 네이티브 파일이 꼭 필요한 확장은 현재 설치 관리자가
지원하지 않는 패키징 방식으로 보고 충분히 테스트해야 합니다. 향후 확장
SDK나 패키지 형식에서 의존성 배포 방법이 정식으로 정의될 수 있습니다.

## 문제 해결

### 확장 DLL을 찾을 수 없는 경우

상대 경로는 Nocturne에 표시되는 현재 디렉터리를 기준으로 해석됩니다. 절대
경로를 사용하거나 설치 전에 DLL이 있는 디렉터리로 이동하세요.

### DLL에 구현이 없다고 표시되는 경우

하나 이상의 구체 형식이 `INocturneExtension`을 구현하는지 확인하세요. 해당
형식에는 public 매개 변수 없는 생성자가 있어야 합니다.

### 명령이 이미 등록되었다고 표시되는 경우

고유한 명령 이름을 사용하세요. 기본 명령과 확장 명령 이름은 대소문자를
구분하지 않고 비교합니다.

### 확장이 `failed`로 표시되는 경우

`/extension list`로 로더 오류를 확인하세요. `%USERPROFILE%\nocturne.ns`에서
`NOCTURNE_VERBOSE=true`로 설정하고 `/reload`를 실행하면 더 자세한 수명 주기
정보를 볼 수 있습니다. 확장을 수정한 뒤 실패한 DLL을 제거하고 새 빌드를
설치하세요.

### 어떤 빌드에서는 동작하지만 다른 빌드에서는 동작하지 않는 경우

Nocturne은 아직 확장 API 버전을 협상하지 않습니다. 대상 Nocturne 소스
리비전을 참조해 다시 빌드하고 해당 호스트 버전에서 테스트하세요.

## 배포 전 확인 사항

- `net10.0-windows`를 대상으로 설정합니다.
- 확장과 명령에 고유한 이름을 사용합니다.
- 프로젝트에 의미 있는 버전 메타데이터를 지정합니다.
- `Initialize`를 빠르게 완료하고 소유한 리소스는 `Shutdown`에서 정리합니다.
- 현재 설치 관리자에서는 별도의 전용 DLL 의존성을 피합니다.
- 설치, 재시작 후 로드, 명령 실행, 제거 과정을 테스트합니다.
- 빌드에 사용한 Nocturne 리비전이나 릴리스를 명확하게 밝힙니다.
