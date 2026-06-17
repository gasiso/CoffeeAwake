# ☕ CoffeeAwake

<div align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4.svg" alt=".NET 8.0">
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6.svg" alt="Windows">
  <img src="https://img.shields.io/badge/license-MIT-green.svg" alt="License">
</div>

<p align="center">
  <a href="README.en.md">🇺🇸 English</a> | <b>🇧🇷 Português</b>
</p>

<br>

> **Utilitário ultraleve de bandeja para Windows** que mantém o seu PC acordado usando a API oficial Win32 `SetThreadExecutionState` — sem simulação de mouse/teclado, sem alterações forçadas no registro, sem mudanças no plano de energia e sem necessidade de direitos de administrador.

---

## Funcionalidades

| Funcionalidade | Detalhes |
|---|---|
| **Manter acordado** | Evita a suspensão do sistema E o desligamento da tela |
| **Sessões programadas** | Desativação automática após 1h / 2h / 4h |
| **Apenas na Bandeja** | Sem janela principal — vive de forma discreta perto do relógio |
| **Atalho por duplo clique** | Ligue e desligue rapidamente direto pelo ícone |
| **Iniciar com o Windows** | Inicialização automática opcional (HKCU — sem aviso chato de UAC) |
| **Instância Única** | Tentar abrir uma segunda vez não faz nada, preservando a memória |
| **EXE Auto-contido** | Apenas um arquivo, não exige a instalação prévia do .NET no PC do usuário |

---

## Como Funciona

O CoffeeAwake utiliza de forma nativa e limpa a seguinte API Win32:

```csharp
// Ativar
SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);

// Desativar
SetThreadExecutionState(ES_CONTINUOUS);
```

O Windows redefine isso automaticamente quando o processo termina, garantindo que o sistema **nunca** fique preso permanentemente em um estado acordado. O CoffeeAwake também zera explicitamente o estado em saídas normais e caso ocorram exceções não tratadas.

---

## Estrutura do Projeto

```
CoffeeAwake/
├── LICENSE
├── README.md
└── src/
    └── CoffeeAwake/
        ├── CoffeeAwake.csproj       # Arquivo do Projeto
        ├── app.manifest             # Define DPI-aware e bloqueia UAC
        ├── Program.cs               # Ponto de entrada, mutex de instância única
        ├── TrayApplicationContext.cs # O coração do app: ícone, menu e lógica
        ├── Native/
        │   └── NativeMethods.cs     # Ponte P/Invoke para SetThreadExecutionState
        ├── Services/
        │   ├── AwakeService.cs      # Regras de negócio e temporizadores
        │   └── StartupService.cs    # Helper para registro de inicialização
        └── UI/
            └── IconFactory.cs       # Geração do ícone via código (sem arquivos externos)
```

---

## Requisitos

- **Windows 10 / 11** (x64 ou ARM64)
- **.NET 8 SDK** — [Baixe aqui](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Compilar & Rodar (Desenvolvimento)

```powershell
# Clone o repositório e navegue até a pasta src
cd src/CoffeeAwake

# Rode o projeto diretamente
dotnet run
```

---

## Publicar como um EXE auto-contido

### Windows x64 (recomendado para a maioria dos PCs)

```powershell
dotnet publish src/CoffeeAwake/CoffeeAwake.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishReadyToRun=true `
    -o ./publish/win-x64
```

### Windows ARM64 (Surface Pro X, Copilot+ PCs)

```powershell
dotnet publish src/CoffeeAwake/CoffeeAwake.csproj `
    -c Release `
    -r win-arm64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -o ./publish/win-arm64
```

O executável gerado (`CoffeeAwake.exe`) na pasta `publish/` é totalmente independente — copie para qualquer lugar e rode. Não tem instalador e nem dependências ocultas.

---

## Como Usar

1. **Abra** o `CoffeeAwake.exe` — um ícone de xícara de café vai aparecer perto do relógio.
2. **Clique duplo** no ícone para ligar/desligar o modo "acordado".
3. **Clique direito** para abrir o menu completo:
   - Ativar / Desativar
   - Manter acordado por 1h / 2h / 4h
   - Iniciar com o Windows
   - Sair

---

## Segurança e Confiabilidade

- ✅ Não requer privilégios de Administrador  
- ✅ Não fica simulando cliques falsos de mouse ou botões de teclado  
- ✅ Não muda os perfis de energia do Windows  
- ✅ Sem lixo no registro (a chave de iniciar com o Windows é opcional)  
- ✅ O PC nunca trava acordado. Se o app fechar ou travar, o Windows volta a dormir  
- ✅ Impede a abertura de várias instâncias ao mesmo tempo  
- ✅ Zero dependências de pacotes externos ou de terceiros  

---

## Licença

[MIT](LICENSE) © 2024 Contribuidores do CoffeeAwake
