# Generatore Automatico di Classi Strategia EasyLanguage

## Descrizione

Lo script `GenerateAllStrategies.ps1` converte automaticamente i file strategia EasyLanguage (`.txt`) in classi C# che implementano l'interfaccia `ITradingStrategy`.

## Posizione

- **Script**: `D:\Piootoo\PiootooApp\Piootoo.Strategies\Easy\GenerateAllStrategies.ps1`
- **File sorgente**: `D:\Piootoo\PiootooApp\piootoo-repository\easy\s_*.txt`
- **File generati**: `D:\Piootoo\PiootooApp\Piootoo.Strategies\Easy\Easy_*.cs`

## Come Usare

### Esecuzione Base

```powershell
cd d:\Piootoo\PiootooApp\Piootoo.Strategies\Easy
powershell -ExecutionPolicy Bypass -File GenerateAllStrategies.ps1
```

### Output

Lo script:
1. Cerca tutti i file `s_*.txt` nella cartella `D:\Piootoo\PiootooApp\piootoo-repository\easy` che corrispondono ai pattern `_DAX_`, `_NQ_`, `_CL_`, `_GC_`
2. Per ogni file trovato, genera una classe C# corrispondente
3. Mostra il progresso in console con colori:
   - **Verde**: File trovati e generati con successo
   - **Giallo**: File in elaborazione
   - **Ciano**: Nome classe generata
   - **Rosso**: Errori o pattern non riconosciuti

## Funzionamento Dettagliato

### 1. Estrazione Informazioni dal Nome File

Lo script analizza il nome del file per estrarre:
- **Numero strategia**: Es. `643` da `s_TOP_UA_643_FDAX_60__7.txt`
- **Simbolo**: `FDAX`, `NQ`, `CL`, `GC` (con o senza prefisso `F`)
- **Timeframe**: `5`, `15`, `30`, `60`, ecc.

**Pattern supportati:**
- `s_TOP_UA_643_FDAX_60__7.txt` → `Easy_643_FDAX_60`
- `s_TOP_UA_123_CL_5____120__7.txt` → `Easy_123_CL_5` (gestisce underscore multipli)

### 2. Estrazione Descrizione

Cerca la prima riga non vuota che:
- Non inizia con `input:`, `var:`, `array:`
- È un commento (`//`) con almeno 10 caratteri
- Non contiene `>>>>` o `Michael` (commenti tecnici)

### 3. Estrazione Input e Variabili

#### Input
Cerca pattern come:
```
input: MySize(1);
inputs: PtnNeutYes(16), PtnNeutNo(10);
```

**Gestisce:**
- Input multipli sulla stessa riga separati da virgole
- Commenti inline dopo i valori
- Valori vuoti (assegna default `0`)

#### Variabili
Cerca pattern come:
```
var: OkLong(true);
vars: highd0(0), lowd0(0);
variable: MyCount(0);
```

**Importante:** Se una variabile ha lo stesso nome di un input, viene ignorata (gli input hanno priorità) per evitare duplicati.

### 4. Inferenza dei Tipi C#

Lo script determina automaticamente il tipo C# basandosi sul valore:

| Valore EasyLanguage | Tipo C# | Esempio |
|---------------------|---------|---------|
| `true` / `false` | `bool` | `true` → `bool` |
| `123` | `int` | `1700` → `int` |
| `0.1` / `0.1m` | `decimal` | `0.1` → `decimal` |
| Altro | `string` | `"test"` → `string` |

### 5. Generazione Codice C#

#### Struttura Classe Generata

```csharp
using System;
using System.Collections.Generic;
using Piootoo.Shared.Enums;
using Piootoo.Shared.Interfaces;
using Piootoo.Shared.Models;
using static Piootoo.Strategies.Easy.EasyLib;

namespace Piootoo.Strategies.Easy;

public class Easy_643_FDAX_60 : ITradingStrategy
{
    // INPUTS
    private int _mySize = 1;
    private decimal _multUp = 0.1m;
    
    // VARIABLES
    private bool _okLong = true;
    private int _myCount = 0;
    
    // STATE
    private string _symbol = "@FDAX";
    private int _timeframeMinutes = 60;
    private string _name = "TOP_UA_643";
    private string _description = "...";
    
    // Properties ITradingStrategy
    public string Name => _name;
    public string Description => _description;
    public string Symbol => _symbol;
    public int TimeframeMinutes => _timeframeMinutes;
    public int RequiredCandles => 100;
    
    // Initialize method
    public void Initialize(Dictionary<string, object>? parameters = null) { ... }
    
    // GenerateSignal stub
    public TradeSignal GenerateSignal(OhlcvData[] data, DateTime currentDate) { ... }
}
```

### 6. Gestione Escape Caratteri Speciali

#### Virgolette
- **Nelle stringhe C#**: `"` → `\"`
- **Nei commenti XML**: `"` → `&quot;`

**Esempio:**
```
EasyLanguage: Rif. BBBB ("EB4B")
C# generato:  "Rif. BBBB (\"EB4B\")"
```

#### Backslash
- **Nelle stringhe C#**: `\` → `\\`

**Esempio:**
```
EasyLanguage: C:\Titan\Reports
C# generato:  "C:\\Titan\\Reports"
```

### 7. Gestione Valori Default

Se un valore è vuoto o non riconosciuto, viene assegnato un default basato sul tipo:

| Tipo | Default |
|------|---------|
| `bool` | `false` |
| `int` | `0` |
| `decimal` | `0m` |
| `string` | `""` |

## Funzioni Helper

### `GetCSharpType`
Determina il tipo C# basandosi sul valore estratto.

### `GetDefaultValue`
Converte il valore EasyLanguage nel formato C# corretto:
- `true` → `true` (bool)
- `1700` → `1700` (int)
- `0.1` → `0.1m` (decimal)
- `"test"` → `"test"` (string)

### `GenerateCSharpClass`
Genera il codice completo della classe C# con:
- Using statements
- Namespace
- Commenti XML
- Campi per input/variabili
- Proprietà ITradingStrategy
- Metodo Initialize
- Metodo GenerateSignal (stub da completare manualmente)

## Limitazioni e Note

### Documentazione di ogni strategia

Ogni nuova classe in `Easy/` deve iniziare con un commento XML `<summary>` in
italiano che descriva il comportamento atteso, non soltanto l'origine della
strategia. Il commento deve indicare:

1. simbolo, timeframe e timezone;
2. sessione e dati storici usati per i livelli o i pattern;
3. filtri, trigger long/short, tipo e durata dell'ordine;
4. limite di entrate e comportamento overnight;
5. uscite dichiarate nel `TradeSignal` (SL/TP, `CloseAtUtc`,
   `MaxBarsInPosition`, trailing e break-even, se presenti).

La documentazione deve rimanere allineata alla classe quando se ne modificano
parametri o logica.

### TODO Manuali

1. **Metodo `GenerateSignal`**: Lo script genera solo uno stub. La logica della strategia deve essere implementata manualmente convertendo le condizioni EasyLanguage (`buy`, `sellshort`, ecc.) in `TradeSignal`.

2. **`RequiredCandles`**: Attualmente è hardcoded a `100`. Dovrebbe essere calcolato in base agli indicatori utilizzati dalla strategia.

3. **Logica Strategia**: Le condizioni, pattern, e calcoli devono essere convertiti manualmente usando le funzioni helper in `EasyLib.cs`.

### Pattern Non Supportati

Alcuni file potrebbero non essere riconosciuti se:
- Il nome file non segue il pattern standard
- Il simbolo non è `DAX`, `NQ`, `CL`, `GC` (con o senza `F`)
- Il timeframe non è un numero

In questi casi, lo script mostra un messaggio di errore in rosso ma continua con gli altri file.

## Esempi di Conversione

### Input EasyLanguage
```
input: MySize(1);
input: MyStopL(3000), MyProfitL(0);
inputs: PtnNeutYes(16), PtnNeutNo(10);
```

### C# Generato
```csharp
// INPUTS
private int _mySize = 1;
private int _myStopL = 3000;
private int _myProfitL = 0;
private int _ptnNeutYes = 16;
private int _ptnNeutNo = 10;
```

### Variabili EasyLanguage
```
var: OkLong(true);
var: HighRange(0), LowRange(0);
```

### C# Generato
```csharp
// VARIABLES
private bool _okLong = true;
private int _highRange = 0;
private int _lowRange = 0;
```

## Troubleshooting

### Errore: "Pattern non riconosciuto"
- Verifica che il nome file segua il pattern: `s_*_SYMBOL_TIMEFRAME*.txt`
- Controlla che il simbolo sia supportato (`DAX`, `NQ`, `CL`, `GC`)

### Errore: "Valore vuoto"
- Lo script assegna automaticamente un default, ma potrebbe essere necessario verificare il file sorgente

### Errore: "Definizione duplicata"
- Lo script ora evita duplicati tra input e variabili
- Se persiste, verifica manualmente il file generato

### Errore: "Escape sequence"
- Lo script gestisce automaticamente virgolette e backslash
- Se persiste, rigenera il file con lo script aggiornato

## Prossimi Passi

Dopo aver generato le classi:

1. **Implementare `GenerateSignal`**: Convertire la logica EasyLanguage in C#
2. **Calcolare `RequiredCandles`**: Basato sugli indicatori utilizzati
3. **Testare le strategie**: Verificare che generino i segnali corretti
4. **Registrare in `StrategyFactory`**: Aggiungere le nuove strategie al factory per l'istanziazione

## Riferimenti

- **EasyLib.cs**: Funzioni helper per la conversione EasyLanguage → C#
- **ITradingStrategy**: Interfaccia che tutte le strategie devono implementare
- **StrategyFactory.cs**: Factory per l'istanziazione delle strategie
