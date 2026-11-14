namespace LazyBlox.Localization;

internal static partial class Lang
{
    private static Strings? _pt;
    public static Strings Pt => _pt ??= new Strings
    {
        AppTagline = "Entre em servidores privados do Roblox direto, sem passar pelo navegador",
        AppDescription = "Abre links de servidores privados do Roblox diretamente no Roblox, sem navegador, e envia os demais links para o seu navegador de sempre.",
        Cancel = "Cancelar",
        Close = "Fechar",
        Back = "Voltar",
        Save = "Salvar",
        Copy = "Copiar",
        Later = "Depois",
        ErrorPrefix = "Erro: {0}",
        On = "ativado",
        Off = "desativado",
        NotSet = "Não definido",
        GenericBrowser = "seu navegador",

        UrlEmpty = "A URL está vazia",
        UrlTooLong = "A URL é longa demais",
        UrlControlChars = "A URL contém caracteres de controle",
        UrlMalformed = "A URL é inválida",
        UrlUnsupportedScheme = "Esquema não suportado: {0}",
        UrlNoHost = "A URL não tem nome de host",

        CmdEmpty = "O comando do navegador está vazio",
        CmdNotAbsolute = "O executável do navegador precisa ser um caminho absoluto: {0}",
        CmdIsSelf = "O navegador de destino é o próprio iniciador (interrompido para evitar um loop infinito)",
        CmdShellDispatcher = "{0} não pode ser usado como navegador porque devolveria o link para este iniciador (prevenção de loop infinito)",

        ErrWindowTitle = "Não foi possível abrir o link",
        ErrCannotOpen = "Este link não pode ser aberto: {0}",
        ErrLoop = "Interrompido porque um loop de encaminhamento foi detectado. Verifique o navegador nas configurações.",
        ErrNoBrowser = "Nenhum navegador está configurado.",
        ErrBrowserFailed = "Não foi possível iniciar o navegador.",
        ErrBrowserHint = "Escolha o navegador novamente nas configurações (execute o LazyBlox.exe sem argumentos).",
        LaunchShellExecuteFailed = "ShellExecuteEx falhou (erro Win32 {0})",
        LaunchExeNotFound = "Executável não encontrado: {0}",
        LaunchCreateProcessFailed = "CreateProcess falhou (erro Win32 {0}): {1}",

        RegDefaultValue = "(Padrão)",
        RegDeleteKey = "(excluir a chave inteira)",
        RegDeleteValue = "excluir o valor {0}",
        RegMismatch = "Divergência: {0}",
        RegExeMissing = "Executável ausente: {0}",
        RegLeftKey = "Chave restante: {0}",
        RegLeftValue = "Valor restante: {0}",

        PlanInstallHeader = "■ Alterações feitas na instalação (somente o usuário atual, HKCU; sem permissões de administrador)",
        PlanCopy = "Arquivo: copiar {0} → {1}",
        PlanRegistry = "Registro:",
        PlanConfig = "Arquivo de configurações: {0}",
        PlanBrowser = "  Navegador: {0} ({1})",
        PlanCommand = "  Comando: {0}",
        PlanUnchanged = "Não é alterado: seu navegador padrão (UserChoice), o registro de outros apps e o HKLM.",
        PlanSwitchYourself = "Depois de instalar, você mesmo troca o app padrão nas Configurações do Windows.",
        PlanUninstallHeader = "■ Alterações feitas na desinstalação",
        PlanUninstallRegistry = "Registro (somente o que este app criou):",
        PlanDeleteConfigDir = "Pasta a excluir: {0} (configurações)",
        PlanDeleteInstallDir = "Pasta a excluir: {0} (local de instalação, se existir)",
        PlanUninstallUnchanged = "Não é alterado: registro e configurações de outros navegadores. Escolha de novo seu navegador padrão no Windows.",

        LogNoBrowser = "Erro: nenhum navegador foi detectado. Indique um com --browser.",
        LogBrowser = "Navegador: {0} ({1})",
        LogVerifyProblems = "A verificação encontrou problemas:",
        LogVerifyOk = "Registro verificado: OK",
        LogNextStep = "Próximo passo: em Configurações > Aplicativos > Aplicativos padrão > LazyBlox, mude \"HTTPS\" e \"HTTP\" para LazyBlox.",
        LogRemovedClean = "Remoção do registro verificada: nada restou",
        LogRemovedPartial = "Algumas entradas do registro permanecem",
        LogStillDefault = "Atenção: o HTTPS ainda abre com o LazyBlox. Em Configurações > Aplicativos > Aplicativos padrão, volte para o seu navegador original ({0}).",
        LogBadCommand = "O comando registrado é inválido",
        LogCopied = "Copiado: {0}",
        LogDeleted = "Excluído: {0}",
        LogDeleteFailed = "Não foi possível excluir: {0} ({1})",
        LogCannotAutoDelete = "Este caminho não pode ser excluído automaticamente. Exclua manualmente: {0}",
        LogDeleteAfterExit = "Será excluído ao sair: {0}",
        LogDeleteManually = "Exclua manualmente: {0}",
        NoteSpecifiedNotFound = "'{0}' não foi encontrado",
        NoteSpecified = "especificado",
        NoteKeepExisting = "mantendo a configuração atual",
        NoteNotDetected = "não detectado",
        NoteDetected = "detectado a partir do navegador padrão atual",

        StRegistered = "Registrado: {0}",
        StYes = "sim",
        StNo = "não",
        StVerify = "Verificação: {0}",
        StExe = "Exe registrado: {0}",
        StHttps = "HTTPS padrão: {0}",
        StHttp = "HTTP padrão:  {0}",
        StInUse = "  ← LazyBlox em uso",
        StBrowser = "Navegador: {0}",
        StCommand = "  Comando: {0}",
        StRoblox = "Roblox (protocolo roblox:): {0}",
        StRobloxFound = "registrado",
        StRobloxMissing = "não encontrado",

        CliNotInstalledClean = "Não instalado (nenhuma entrada restante no registro)",
        CliInstalledOk = "Instalado, registro OK ({0})",
        CliSetBrowserUsage = "Uso: set-browser <ProgID | nome do navegador | caminho absoluto do exe>",
        CliBrowserNotFound = "Não encontrado. Execute 'browsers' para ver a lista.",
        CliBrowserSet = "Navegador: {0}\n  {1}",
        CliTiming = "Registro de tempo: {0}  ({1})",
        CliLegacy = "Conversão de links antigos de servidor privado: {0}",
        CliUnknownCommand = "Comando desconhecido: {0}",
        CliNeedYes = "Execute novamente com --yes para aplicar as alterações acima (nada foi alterado).",
        CliDryRunUsage = "Uso: dry-run <URL>",
        Help = """
            LazyBlox - iniciador rápido para links de servidores privados do Roblox

            Uso:
              LazyBlox.exe                      Configurações (GUI)
              LazyBlox.exe <URL>                Processa um link (é assim que o Windows o chama)
              LazyBlox.exe status               Mostra registro, apps padrão e navegador
              LazyBlox.exe plan [uninstall]     Prévia das alterações no registro (não altera nada)
              LazyBlox.exe install [--yes] [--in-place] [--browser <nome|ProgID|caminho exe>] [--no-settings]
              LazyBlox.exe uninstall [--yes] [--keep-config]
              LazyBlox.exe verify               Verifica o registro / restos de configuração
              LazyBlox.exe browsers             Lista os navegadores detectados (* = atual)
              LazyBlox.exe set-browser <nome|ProgID|caminho exe>
              LazyBlox.exe default-apps         Abre as configurações de Aplicativos padrão do Windows
              LazyBlox.exe dry-run <URL>        Mostra a decisão e a ação prevista (não executa nada)
              LazyBlox.exe bench [URL] [vezes]  Microbenchmark da análise de URL
              LazyBlox.exe timing on|off        Registra o tempo de processamento em timing.log
              LazyBlox.exe legacy on|off        Converte links antigos /games/ID?privateServerLinkCode= (experimental)

            Sem --yes, install / uninstall apenas mostram as alterações e não fazem nada.
            """,

        ConfirmInstallTitle = "Instalar o LazyBlox?",
        ConfirmRepairTitle = "Reparar o registro?",
        ConfirmInstallBody = "As alterações abaixo valem só para o usuário atual. Seu navegador padrão e as configurações de outros apps não são alterados.",
        ConfirmUninstallTitle = "Desinstalar o LazyBlox?",
        ConfirmUninstallBody = "Somente as entradas de registro e configurações criadas por este app são removidas. Se o LazyBlox ainda for o app padrão para links, escolha de novo seu navegador original nas Configurações do Windows.",
        ConfirmGenericBody = "As seguintes alterações serão feitas.",
        BtnInstall = "Instalar",
        BtnRepair = "Reparar",
        BtnUninstall = "Desinstalar",
        BtnOpenDefaultApps = "Abrir Aplicativos padrão",
        InstalledTitle = "Instalado",
        InstalledBody = "Último passo: nas Configurações do Windows, escolha o LazyBlox como app padrão para \"HTTPS\" e \"HTTP\".",
        InstallFailedTitle = "Falha na instalação",
        InstallFailedBody = "Veja os detalhes abaixo.",
        UninstalledTitle = "Desinstalado",
        UninstallPartialTitle = "Alguns itens não puderam ser removidos",
        UninstalledStillDefault = "O HTTPS ainda abre com o LazyBlox. Escolha de novo seu navegador original nas Configurações do Windows.",
        UninstalledThanks = "Obrigado por usar o LazyBlox.",
        AfterUninstallHint = "Se o LazyBlox ainda for seu navegador padrão, escolha de novo seu navegador original em Configurações > Aplicativos > Aplicativos padrão.",

        SectionActions = "Ações",
        TileHttps = "HTTPS padrão",
        TileBrowser = "Navegador",
        RobloxDetected = "Detectado",
        RobloxMissing = "Não encontrado",
        RowDefaultApps = "Abrir Aplicativos padrão",
        RowDefaultAppsDesc = "Escolha o LazyBlox para HTTPS e HTTP (você mesmo faz a troca)",
        RowInstall = "Instalar",
        RowRepair = "Reparar / reinstalar",
        RowInstallDesc = "Revise as alterações antes de aplicar. Não precisa de permissões de administrador",
        RowBrowser = "Navegador",
        RowBrowserDesc = "Navegador para links que não são do Roblox: {0}",
        RowLegacy = "Abrir também links antigos no Roblox",
        RowLegacyDesc = "Suporta links /games/…?privateServerLinkCode= (experimental)",
        RowTest = "Testador de links",
        RowTestDesc = "Veja quais links abrem no Roblox (não executa nada)",
        RowUninstallDesc = "Remove apenas as entradas de registro e configurações criadas por este app",
        HeroRobloxMissing = "O Roblox não foi encontrado, então os links do Roblox também abrirão no navegador.",
        HeroNotInstalledPill = "Não instalado",
        HeroNotInstalledTitle = "Instale para começar",
        HeroNotInstalledDesc = "Depois de instalar, você pode escolher o LazyBlox em \"Aplicativos padrão\" do Windows. Seu navegador padrão não é alterado.",
        HeroBrokenPill = "Precisa de reparo",
        HeroBrokenTitle = "Há um problema no registro",
        HeroOneStepPill = "Falta um passo",
        HeroOneStepTitle = "Troque o app padrão",
        HeroOneStepDesc = "Escolha o LazyBlox para \"HTTPS\" e \"HTTP\" nas Configurações para receber links do Discord e de outros apps.",
        HeroReadyPill = "Pronto",
        HeroReadyTitle = "Links do Roblox abrem direto",
        HeroReadyDesc = "Abra um link de servidor privado no Discord ou em qualquer lugar e o Roblox inicia sem navegador. Os demais links abrem no {0}.",
        HeroReadyHttpHint = "Defina também o HTTP como LazyBlox para tratar links http://.",

        BrowserTitle = "Navegador",
        BrowserIntro = "Links que não são de servidores privados do Roblox abrem no navegador escolhido aqui. O próprio LazyBlox e iniciadores que devolveriam o link pela associação não aparecem na lista.",
        BrowserNone = "Nenhum navegador registrado foi encontrado. Você pode definir o caminho de um exe com o comando set-browser da CLI.",

        TestTitle = "Testador de links",
        TestIntro = "Copie uma URL e clique em Colar para ver como esse link seria tratado. Nada é executado de verdade.",
        TestPaste = "Colar e testar",
        TestSample = "Exemplo",
        TestInput = "Entrada",
        TestToRoblox = "Abre direto no Roblox",
        TestToBrowser = "Abre no navegador",
        TestRejected = "Não abre (rejeitado)",
        TestReason = "Motivo",
        TestDeepLink = "Deep link convertido",
        TestForwardUrl = "URL a encaminhar",
        TestPlanned = "Ação prevista",
        TestActRoblox = "Enviar ao Roblox: {0}",
        TestActBrowser = "Iniciar navegador: {0}",
        TestActError = "Mostrar erro: {0}",
        TestClipboardEmpty = "Não há texto na área de transferência.",
    };
}
