# Fake Shutdown para Windows 11

Aplicação Windows-only que apresenta uma experiência visual fullscreen inspirada
na tela de desligamento do Windows 11 e, ao final da animação, solicita a
**suspensão real** do computador.

Esta é uma simulação visual independente. Não é uma tela oficial da Microsoft,
não substitui componentes do Windows e não coleta credenciais, teclado, tela ou
dados pessoais.

## Estado da integração com o Windows

O Windows não oferece uma API oficial e reversível para substituir somente a
ação `Menu Iniciar > Energia > Desligar` por um executável de terceiros. Esse
comando é controlado pelo Shell/Winlogon e não possui um ponto de extensão
suportado para esse cenário.

Por isso, este projeto **não altera o Explorer, o Winlogon, DLLs protegidas,
Políticas de Grupo ou o Registro**. A experiência é iniciada manualmente pelo
executável. Não há uma configuração oculta que faça o botão nativo `Desligar`
abrir a tela Fake; afirmar o contrário seria enganoso e poderia deixar o
Windows sem um caminho normal de recuperação.

## Fluxo da aplicação

```text
┌──────────────────────────────┐
│ Menu Iniciar                 │
│                              │
│ Energia                      │
│   ├─ Desligar                │
│   │    ↓                     │
│   │ Desligamento normal      │
│   │                          │
│   └─ Suspender               │
│        ↓                     │
│    Suspensão REAL            │
└──────────────────────────────┘

Executar FakeShutdown.exe separadamente
	↓
Tela Fake fullscreen
	↓
Suspensão REAL
```

| Ação | Resultado |
| --- | --- |
| Executar `FakeShutdown.exe` | Animação e solicitação de suspensão real |
| Menu Iniciar > Energia > Suspender | Suspensão normal do Windows, sem passar pela aplicação |
| Menu Iniciar > Energia > Desligar, ou `shutdown /s /t 0` | Desligamento real do Windows |

## Requisitos

- Windows 10 ou Windows 11, em arquitetura x64 ou compatível com o .NET Desktop Runtime.
- .NET 8 Desktop Runtime para executar uma publicação dependente do framework.
- .NET 8 SDK ou superior para compilar. O projeto usa `net8.0-windows` e WinForms.
- Permissão para executar um programa desktop e solicitar suspensão. Não requer administrador.
- Para desenvolvimento: Visual Studio 2022 com “Desenvolvimento para desktop com .NET”, ou SDK .NET equivalente.

O ambiente Linux deste repositório consegue restaurar e compilar o alvo Windows
com `EnableWindowsTargeting`, mas não consegue executar a janela WinForms nem
testar a suspensão física.

## Instalação e execução

No PowerShell, na pasta do projeto:

```powershell
dotnet restore .\FakeShutdown.csproj
dotnet build .\FakeShutdown.csproj
dotnet run --project .\FakeShutdown.csproj
```

A janela cobre o monitor principal, não tem bordas nem barra de título e começa
a animação automaticamente. Pressione `Esc` para sair sem suspender.

Para um build dependente do runtime:

```powershell
dotnet publish .\FakeShutdown.csproj -c Release -r win-x64 --self-contained false
```

Para gerar um executável autocontido:

```powershell
dotnet publish .\FakeShutdown.csproj -c Release -r win-x64 --self-contained true
```

O executável estará em `bin\Release\net8.0-windows\win-x64\publish\`. Use
`win-arm64` se essa for a arquitetura do computador.

## Como configurar o botão “Desligar”

Não existe configuração suportada para fazer o botão nativo chamar esta
aplicação. O projeto deliberadamente não instala serviço, tarefa agendada,
substituição do Shell, associação de protocolo ou alteração do Registro para
interceptar o desligamento.

O procedimento seguro e reversível é:

1. Compile ou publique o executável.
2. Crie um atalho comum para `FakeShutdown.exe` no desktop ou no menu Iniciar.
3. Inicie esse atalho quando quiser a experiência visual seguida de suspensão.
4. Para voltar ao comportamento original, apague somente o atalho.

O botão `Menu > Energia > Desligar` continuará desligando o Windows, como deve.
O botão `Menu > Energia > Suspender` continuará suspendendo o Windows sem abrir
a tela Fake. Essa limitação é intencional: a alternativa de substituir
componentes do sistema seria frágil, não oficial e desnecessária.

## Suspensão e desligamento real

A aplicação chama `SetSuspendState` de `powrprof.dll` depois de uma animação de
aproximadamente 6,5 segundos. A chamada não bloqueia a thread de UI. Se o
Windows recusar a solicitação, a tela mostra um erro, registra o diagnóstico
localmente e fecha.

Para desligar o computador de verdade, use um método normal do Windows:

```powershell
shutdown.exe /s /t 0
```

Ou use `Menu Iniciar > Energia > Desligar`. `Desligar` personalizado e
desligamento real são coisas diferentes; o primeiro é apenas executar a
aplicação e termina em suspensão.

## Como desfazer

Não há instalação persistente para remover. Para desfazer o uso da experiência:

1. Feche a aplicação com `Esc`, se ela estiver visível.
2. Exclua o atalho criado para `FakeShutdown.exe`.
3. Remova a pasta publicada, se não for mais necessária.

O Windows não terá sido modificado e `Menu > Energia > Desligar` continuará no
comportamento original.

## Segurança e tratamento de erros

- Não solicita senha, PIN ou conta Microsoft.
- Não captura teclado ou tela; `Esc` é tratado somente enquanto a janela está ativa.
- Não cria persistência oculta, não desativa segurança e não exige elevação.
- Um mutex impede duas instâncias simultâneas.
- `Esc`, fechamento da janela e falhas de inicialização interrompem a experiência.
- O timer de animação é limitado e não usa loop infinito nem `sleep` na UI.
- O arquivo de diagnóstico local fica em `%LOCALAPPDATA%\FakeShutdown\errors.log`.
- A janela usa `PerMonitorV2`, escala via DPI e as dimensões do monitor primário.

## Solução de problemas

### “Fake Shutdown” não inicia

Confirme que está executando em Windows e que o .NET 8 Desktop Runtime está
instalado, ou use uma publicação `--self-contained true`. Verifique também se
outra instância já está aberta.

### O botão “Desligar” continua desligando

Esse é o comportamento esperado: a integração direta não é suportada pelo
Windows e não é alterada por este projeto. Use o atalho da aplicação para o
fluxo Fake.

### A suspensão não acontece

Confira políticas de energia, aplicativos que bloqueiam suspensão e o arquivo
`%LOCALAPPDATA%\FakeShutdown\errors.log`. Teste a suspensão normal pelo menu
Iniciar para separar um problema do Windows de um problema da aplicação.

### Tela preta, fullscreen ou DPI incorreto

Atualize o driver gráfico, teste a publicação correta para a arquitetura e
confira a escala do Windows. O manifesto e `ApplicationHighDpiMode` usam
`PerMonitorV2`.

### Animação trava ou a aplicação fecha

Pressione `Esc` para recuperar o desktop e consulte o log local. Uma falha de
arquivo de diagnóstico não impede o fechamento seguro da janela.

### Múltiplos monitores

A versão atual abre no monitor primário, em suas dimensões atuais, e não cria
uma janela invisível nos demais monitores. Mudar a configuração de monitores
antes de iniciar é o comportamento recomendado.

### Windows Defender ou permissões

O aplicativo não requer administrador. Use binários publicados pelo próprio
usuário e valide a origem do build; não desative o Defender para executar o
projeto.

## Desenvolvimento

```text
FakeShutdown.csproj  Configuração WinForms e alvo Windows
Program.cs           Ponto de entrada e mutex de instância única
MainForm.cs          Fullscreen, animação, desenho e suspensão via P/Invoke
app.manifest         Manifesto desktop da aplicação
README.md            Operação, limitações e testes
```

O caminho de execução é `Program.Main` → `MainForm` → `FakeShutdownSurface`.
O desenho ocorre em `OnPaint`; um `System.Windows.Forms.Timer` atualiza o
progresso e, ao final, `SetSuspendState` solicita a suspensão.

Comandos úteis:

```powershell
dotnet build .\FakeShutdown.csproj -c Release
dotnet publish .\FakeShutdown.csproj -c Release -r win-x64 --self-contained true
```

## Testes

O build deve ser executado em cada alteração. Os testes de comportamento
precisam ser feitos em uma máquina Windows, pois Linux não fornece WinForms
nem suspensão Windows:

- Fullscreen em 1280×720, 1920×1080, 2560×1440 e 3840×2160.
- Escalas de 100%, 125%, 150% e 200%.
- Monitor primário único e configurações com múltiplos monitores.
- Execução normal, segunda instância, cancelamento por `Esc` e fechamento.
- Animação completa e suspensão real.
- Suspensão nativa pelo menu, sem abrir a aplicação.
- Desligamento real pelo menu e por `shutdown /s /t 0`.
- Falha de suspensão, resolução alterada e desconexão de monitor.
- Ausência de janelas órfãs, flickering, loops infinitos e alterações persistentes.

Validação realizada neste repositório:

```text
dotnet restore FakeShutdown.csproj
dotnet build FakeShutdown.csproj --no-restore
Build succeeded: net8.0-windows
```
