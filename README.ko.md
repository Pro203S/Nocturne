# Nocturne 🌙

[English](README.md)

Nocturne은 평소에 사용해왔던 Windows 터미널에 특별한 경험을 느낄 수 있게 해주는 셸입니다.

> Nocturne은 활발히 개발 중입니다. 릴리스 사이에 명령, 설정, 확장 API가
> 변경될 수 있습니다.

## 주요 기능

- 슬래시 커맨드
- DLL 확장 가능
- Discord RPC 표시
- 파일 편집기 내장

## 설치

Nocturne는 Windows만 지원합니다.

arm64, x86, x64를 지원합니다.

1. [GitHub Releases](https://github.com/Pro203S/Nocturne/releases)에서 본인 아키텍쳐에 맞는 압축 파일을 다운로드하세요.
2. 압축을 해제합니다.
3. `Nocturne.exe`을 실행합니다.

실행에 별도의 .NET Runtime 설치가 필요하지 않습니다.

## Windows Terminal에서 사용하기

Windows Terminal의 자세한 정보는 [microsoft/terminal](https://github.com/microsoft/terminal)에서 확인하실 수 있습니다.

Windows Terminal에서 프로필을 새로 만들어 사용하실 수 있습니다.

[프로필에 대해 알아보기](https://learn.microsoft.com/ko-kr/windows/terminal/customize-settings/profile-general)

## 커스터마이즈

Nocturne을 첫 실행하면 %UserProfile%에 **nocturne.ns** 파일이 생성됩니다.

nocturne.ns를 수정해 셸의 설정을 바꾸거나 셸이 실행될 때 실행할 명령어를 적을 수 있습니다.

## 확장

Nocturne의 확장 DLL을 만들어 커스텀 슬래시 명령어를 추가할 수 있습니다.

확장을 만드는 법은 [여기](/docs/ko/extensions.md)를 참고해주세요.
