namespace LazyBlox.Localization;

internal static partial class Lang
{
    private static Strings? _ko;
    public static Strings Ko => _ko ??= new Strings
    {
        AppTagline = "브라우저를 거치지 않고 Roblox 비공개 서버에 바로 참가",
        AppDescription = "Roblox 비공개 서버 링크를 브라우저 없이 Roblox에서 바로 열고, 그 밖의 링크는 평소 쓰는 브라우저로 전달합니다.",
        Cancel = "취소",
        Close = "닫기",
        Back = "뒤로",
        Save = "저장",
        Copy = "복사",
        Later = "나중에",
        ErrorPrefix = "오류: {0}",
        On = "켜짐",
        Off = "꺼짐",
        NotSet = "설정 안 됨",
        GenericBrowser = "브라우저",

        UrlEmpty = "URL이 비어 있습니다",
        UrlTooLong = "URL이 너무 깁니다",
        UrlControlChars = "URL에 제어 문자가 포함되어 있습니다",
        UrlMalformed = "URL 형식이 올바르지 않습니다",
        UrlUnsupportedScheme = "지원하지 않는 스킴입니다: {0}",
        UrlNoHost = "URL에 호스트 이름이 없습니다",

        CmdEmpty = "브라우저 명령이 비어 있습니다",
        CmdNotAbsolute = "브라우저 실행 파일은 절대 경로로 지정해야 합니다: {0}",
        CmdIsSelf = "전달 대상 브라우저가 이 런처 자신입니다 (무한 루프 방지를 위해 중지)",
        CmdShellDispatcher = "{0}은(는) 연결 프로그램을 통해 이 런처로 되돌아오므로 전달 대상으로 사용할 수 없습니다 (무한 루프 방지)",

        ErrWindowTitle = "링크를 열 수 없습니다",
        ErrCannotOpen = "이 링크는 열 수 없습니다: {0}",
        ErrLoop = "전달 루프가 감지되어 중지했습니다. 설정에서 전달 대상 브라우저를 확인하세요.",
        ErrNoBrowser = "전달 대상 브라우저가 설정되지 않았습니다.",
        ErrBrowserFailed = "브라우저를 시작할 수 없습니다.",
        ErrBrowserHint = "설정에서 전달 대상 브라우저를 다시 선택하세요 (LazyBlox.exe를 인수 없이 실행).",
        LaunchShellExecuteFailed = "ShellExecuteEx 실패 (Win32 오류 {0})",
        LaunchExeNotFound = "실행 파일을 찾을 수 없습니다: {0}",
        LaunchCreateProcessFailed = "CreateProcess 실패 (Win32 오류 {0}): {1}",

        RegDefaultValue = "(기본값)",
        RegDeleteKey = "(키 전체 삭제)",
        RegDeleteValue = "값 {0} 삭제",
        RegMismatch = "불일치: {0}",
        RegExeMissing = "실행 파일이 없습니다: {0}",
        RegLeftKey = "남은 키: {0}",
        RegLeftValue = "남은 값: {0}",

        PlanInstallHeader = "■ 설치 시 변경 내용 (현재 사용자 HKCU만, 관리자 권한 불필요)",
        PlanCopy = "파일: {0} → {1}(으)로 복사",
        PlanRegistry = "레지스트리:",
        PlanConfig = "설정 파일: {0}",
        PlanBrowser = "  전달 대상 브라우저: {0} ({1})",
        PlanCommand = "  명령: {0}",
        PlanUnchanged = "변경하지 않는 것: 기본 브라우저(UserChoice), 다른 앱의 레지스트리, HKLM.",
        PlanSwitchYourself = "설치 후 기본 앱 전환은 Windows 설정에서 직접 하세요.",
        PlanUninstallHeader = "■ 제거 시 변경 내용",
        PlanUninstallRegistry = "레지스트리 (이 앱이 만든 것만):",
        PlanDeleteConfigDir = "삭제할 폴더: {0} (설정)",
        PlanDeleteInstallDir = "삭제할 폴더: {0} (설치 위치, 있는 경우)",
        PlanUninstallUnchanged = "변경하지 않는 것: 다른 브라우저의 등록 및 설정. 기본 브라우저는 Windows에서 다시 선택하세요.",

        LogNoBrowser = "오류: 전달 대상 브라우저를 찾지 못했습니다. --browser로 지정하세요.",
        LogBrowser = "전달 대상 브라우저: {0} ({1})",
        LogVerifyProblems = "검증 중 문제가 발견되었습니다:",
        LogVerifyOk = "등록 검증 완료: OK",
        LogNextStep = "다음 단계: 설정 > 앱 > 기본 앱 > LazyBlox에서 \"HTTPS\"와 \"HTTP\"를 LazyBlox로 바꾸세요.",
        LogRemovedClean = "레지스트리 삭제 확인: 남은 항목 없음",
        LogRemovedPartial = "일부 레지스트리 항목이 남아 있습니다",
        LogStillDefault = "주의: HTTPS가 아직 LazyBlox로 열립니다. 설정 > 앱 > 기본 앱에서 원래 브라우저({0})로 되돌리세요.",
        LogBadCommand = "등록된 명령이 올바르지 않습니다",
        LogCopied = "복사했습니다: {0}",
        LogDeleted = "삭제했습니다: {0}",
        LogDeleteFailed = "삭제할 수 없습니다: {0} ({1})",
        LogCannotAutoDelete = "자동으로 삭제할 수 없는 경로입니다. 직접 삭제하세요: {0}",
        LogDeleteAfterExit = "종료 후 삭제합니다: {0}",
        LogDeleteManually = "직접 삭제하세요: {0}",
        NoteSpecifiedNotFound = "지정한 '{0}'을(를) 찾을 수 없습니다",
        NoteSpecified = "지정됨",
        NoteKeepExisting = "기존 설정 유지",
        NoteNotDetected = "감지 안 됨",
        NoteDetected = "현재 기본 브라우저에서 감지",

        StRegistered = "등록: {0}",
        StYes = "있음",
        StNo = "없음",
        StVerify = "등록 검증: {0}",
        StExe = "등록된 exe: {0}",
        StHttps = "기본 HTTPS: {0}",
        StHttp = "기본 HTTP : {0}",
        StInUse = "  ← LazyBlox 사용 중",
        StBrowser = "전달 대상 브라우저: {0}",
        StCommand = "  명령: {0}",
        StRoblox = "Roblox (roblox: 프로토콜): {0}",
        StRobloxFound = "등록됨",
        StRobloxMissing = "찾을 수 없음",

        CliNotInstalledClean = "설치되지 않음 (남은 레지스트리 없음)",
        CliInstalledOk = "설치됨, 등록 정상 ({0})",
        CliSetBrowserUsage = "사용법: set-browser <ProgID | 브라우저 이름 | exe 절대 경로>",
        CliBrowserNotFound = "찾을 수 없습니다. 'browsers'로 목록을 확인하세요.",
        CliBrowserSet = "전달 대상 브라우저: {0}\n  {1}",
        CliTiming = "시간 기록: {0}  ({1})",
        CliLegacy = "구형 비공개 서버 링크 변환: {0}",
        CliUnknownCommand = "알 수 없는 명령: {0}",
        CliNeedYes = "위 변경 사항을 적용하려면 --yes를 붙여 다시 실행하세요 (아무것도 변경하지 않았습니다).",
        CliDryRunUsage = "사용법: dry-run <URL>",
        Help = """
            LazyBlox - Roblox 비공개 서버 링크 고속 런처

            사용법:
              LazyBlox.exe                      설정 화면 (GUI)
              LazyBlox.exe <URL>                링크 처리 (Windows가 호출하는 형식)
              LazyBlox.exe status               등록 상태, 기본 앱, 전달 대상 표시
              LazyBlox.exe plan [uninstall]     레지스트리 변경 내용 미리 보기 (아무것도 변경하지 않음)
              LazyBlox.exe install [--yes] [--in-place] [--browser <이름|ProgID|exe경로>] [--no-settings]
              LazyBlox.exe uninstall [--yes] [--keep-config]
              LazyBlox.exe verify               등록 내용 / 남은 설정 검증
              LazyBlox.exe browsers             감지된 브라우저 목록 (* = 현재 전달 대상)
              LazyBlox.exe set-browser <이름|ProgID|exe경로>
              LazyBlox.exe default-apps         Windows 기본 앱 설정 열기
              LazyBlox.exe dry-run <URL>        판정 결과와 실행 예정 표시 (실행하지 않음)
              LazyBlox.exe bench [URL] [횟수]   URL 판정 마이크로 벤치마크
              LazyBlox.exe timing on|off        처리 시간을 timing.log에 기록
              LazyBlox.exe legacy on|off        구형 /games/ID?privateServerLinkCode= 링크 변환 (실험적)

            --yes가 없으면 install / uninstall은 변경 내용만 표시하고 아무것도 하지 않습니다.
            """,

        ConfirmInstallTitle = "LazyBlox를 설치할까요?",
        ConfirmRepairTitle = "등록을 복구할까요?",
        ConfirmInstallBody = "아래 변경은 현재 사용자에게만 적용됩니다. 기본 브라우저나 다른 앱의 설정은 바꾸지 않습니다.",
        ConfirmUninstallTitle = "LazyBlox를 제거할까요?",
        ConfirmUninstallBody = "이 앱이 만든 레지스트리와 설정만 삭제합니다. LazyBlox가 아직 링크의 기본 앱이라면, 나중에 Windows 설정에서 원래 브라우저를 다시 선택하세요.",
        ConfirmGenericBody = "다음 변경을 수행합니다.",
        BtnInstall = "설치",
        BtnRepair = "복구",
        BtnUninstall = "제거",
        BtnOpenDefaultApps = "기본 앱 열기",
        InstalledTitle = "설치했습니다",
        InstalledBody = "마지막 단계: Windows 설정에서 \"HTTPS\"와 \"HTTP\"의 기본 앱으로 LazyBlox를 선택하세요.",
        InstallFailedTitle = "설치하지 못했습니다",
        InstallFailedBody = "아래 세부 정보를 확인하세요.",
        UninstalledTitle = "제거했습니다",
        UninstallPartialTitle = "일부를 삭제하지 못했습니다",
        UninstalledStillDefault = "HTTPS가 아직 LazyBlox로 열립니다. Windows 설정에서 원래 브라우저를 다시 선택하세요.",
        UninstalledThanks = "LazyBlox를 이용해 주셔서 감사합니다.",
        AfterUninstallHint = "기본 브라우저가 아직 LazyBlox라면 설정 > 앱 > 기본 앱에서 원래 브라우저를 다시 선택하세요.",

        SectionActions = "작업",
        TileHttps = "HTTPS 기본값",
        TileBrowser = "전달 대상 브라우저",
        RobloxDetected = "감지됨",
        RobloxMissing = "찾을 수 없음",
        RowDefaultApps = "기본 앱 설정 열기",
        RowDefaultAppsDesc = "HTTPS와 HTTP에 LazyBlox를 선택합니다 (직접 전환)",
        RowInstall = "설치",
        RowRepair = "등록 복구 / 다시 설치",
        RowInstallDesc = "실행 전에 변경 내용을 확인합니다. 관리자 권한은 필요 없습니다",
        RowBrowser = "전달 대상 브라우저",
        RowBrowserDesc = "Roblox 외 링크를 여는 브라우저: {0}",
        RowLegacy = "구형 링크도 Roblox에서 열기",
        RowLegacyDesc = "/games/…?privateServerLinkCode= 형식 지원 (실험적)",
        RowTest = "링크 판정 테스트",
        RowTestDesc = "어떤 링크가 Roblox에서 열리는지 확인합니다 (실행하지 않음)",
        RowUninstallDesc = "이 앱이 만든 레지스트리와 설정만 삭제합니다",
        HeroRobloxMissing = "Roblox를 찾을 수 없어 Roblox 링크도 브라우저에서 열립니다.",
        HeroNotInstalledPill = "설치 안 됨",
        HeroNotInstalledTitle = "먼저 설치하세요",
        HeroNotInstalledDesc = "설치하면 Windows \"기본 앱\"에서 LazyBlox를 선택할 수 있습니다. 기본 브라우저는 바꾸지 않습니다.",
        HeroBrokenPill = "복구 필요",
        HeroBrokenTitle = "등록에 문제가 있습니다",
        HeroOneStepPill = "한 단계 남음",
        HeroOneStepTitle = "기본 앱을 전환하세요",
        HeroOneStepDesc = "설정에서 \"HTTPS\"와 \"HTTP\"에 LazyBlox를 선택하면 Discord 등에서 링크를 받을 수 있습니다.",
        HeroReadyPill = "준비 완료",
        HeroReadyTitle = "Roblox 링크가 바로 실행됩니다",
        HeroReadyDesc = "Discord 등에서 비공개 서버 링크를 열면 브라우저 없이 Roblox가 바로 시작됩니다. 그 밖의 링크는 {0}에서 열립니다.",
        HeroReadyHttpHint = "HTTP도 LazyBlox로 설정하면 http:// 링크도 처리합니다.",

        BrowserTitle = "전달 대상 브라우저",
        BrowserIntro = "Roblox 비공개 서버 링크가 아닌 링크는 여기서 선택한 브라우저에서 열립니다. LazyBlox 자신과 연결 프로그램을 통해 되돌아오는 실행 방식은 목록에 나타나지 않습니다.",
        BrowserNone = "등록된 브라우저를 찾지 못했습니다. CLI 명령 set-browser로 exe 경로를 지정할 수 있습니다.",

        TestTitle = "링크 판정 테스트",
        TestIntro = "URL을 복사한 뒤 \"붙여넣기\"를 누르면 그 링크가 어떻게 처리되는지 표시합니다. 실제로는 아무것도 실행하지 않습니다.",
        TestPaste = "붙여넣고 판정",
        TestSample = "샘플",
        TestInput = "입력",
        TestToRoblox = "Roblox에서 바로 열기",
        TestToBrowser = "브라우저에서 열기",
        TestRejected = "열지 않음 (거부)",
        TestReason = "이유",
        TestDeepLink = "변환된 딥 링크",
        TestForwardUrl = "전달할 URL",
        TestPlanned = "실행 예정",
        TestActRoblox = "Roblox에 전달: {0}",
        TestActBrowser = "브라우저 실행: {0}",
        TestActError = "오류 표시: {0}",
        TestClipboardEmpty = "클립보드에 텍스트가 없습니다.",
    };
}
