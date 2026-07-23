import { Component, OnInit } from '@angular/core';
import { ApiService } from '../../services/api.service';
import {
  OptimizationRequest,
  OptimizationPreset,
  OptimizationParameters,
  RiskParameters,
  EvaluationPeriod,
  AlgorithmSettings,
  PeriodType,
  OptimizationObjective,
  SymbolInfo,
  SymbolSelection,
  SavedSetup,
  FilteredBacktestingResult
} from '../../models/optimization.models';

@Component({
  selector: 'app-optimization',
  templateUrl: './optimization.component.html',
  styleUrls: ['./optimization.component.css'],
  standalone: false
})
export class OptimizationComponent implements OnInit {
  // Form data
  setupName = '';
  description = '';
  selectedSymbols: SymbolSelection[] = [];
  
  evaluationPeriod: EvaluationPeriod = {
    type: PeriodType.Weeks,
    weeks: 4,
    months: 1
  };

  optimizationParams: OptimizationParameters = {
    primaryObjective: OptimizationObjective.SharpeRatio,
    returnWeight: 0.25,
    sharpeWeight: 0.25,
    profitDrawdownRatioWeight: 0.20,
    winRateWeight: 0.15,
    profitFactorWeight: 0.10,
    consistencyWeight: 0.05
  };

  riskParams: RiskParameters = {
    maxDrawdown: -0.15,
    maxConsecutiveLosses: 5,
    minWinRate: 0.45,
    minSharpeRatio: 0.5,
    minProfitFactor: 1.2,
    minTrades: 10,
    stopLossPercent: -0.20,
    requirePositiveBalance: true
  };

  algorithmSettings: AlgorithmSettings = {
    iterations: 100,
    populationSize: 50,
    useWalkForward: false,
    inSamplePercent: 0.7,
    saveIntermediateResults: false,
    parallelize: true
  };

  // Data
  presets: OptimizationPreset[] = [];
  symbols: SymbolInfo[] = [];
  savedSetups: SavedSetup[] = [];
  
  // State
  isLoading = false;
  isOptimizing = false;
  response: FilteredBacktestingResult | null = null;
  error: string | null = null;
  activeTab: 'params' | 'risk' | 'algorithm' | 'results' = 'params';

  // Enums for template
  periodTypes = PeriodType;
  objectives = OptimizationObjective;
  Math = Math; // Per usare Math nel template

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadPresets();
    this.loadSymbols();
    this.loadSavedSetups();
  }

  loadPresets(): void {
    this.api.getPresets().subscribe({
      next: (presets) => this.presets = presets,
      error: (err) => console.error('Errore caricamento preset:', err)
    });
  }

  loadSymbols(): void {
    this.api.getSymbols().subscribe({
      next: (symbols) => this.symbols = symbols,
      error: (err) => console.error('Errore caricamento simboli:', err)
    });
  }

  loadSavedSetups(): void {
    this.api.getAllSetups().subscribe({
      next: (setups) => this.savedSetups = setups,
      error: (err) => console.error('Errore caricamento setup:', err)
    });
  }

  applyPreset(preset: OptimizationPreset): void {
    this.optimizationParams = { ...preset.optimizationParams };
    this.riskParams = { ...preset.riskParams };
  }

  loadSetup(setup: SavedSetup): void {
    this.setupName = setup.name;
    this.description = setup.description || '';
    this.selectedSymbols = setup.symbols.map(s => ({ ...s }));
    this.evaluationPeriod = { ...setup.evaluationPeriod };
    this.optimizationParams = { ...setup.optimizationParams };
    this.riskParams = { ...setup.riskParams };
  }

  /** Crea la chiave univoca per un symbol+barType */
  getSymbolKey(symbolInfo: SymbolInfo): string {
    return `${symbolInfo.symbol}|${symbolInfo.barType}`;
  }

  /** Verifica se un simbolo+timeframe è selezionato */
  isSymbolSelected(symbolInfo: SymbolInfo): boolean {
    return this.selectedSymbols.some(
      s => s.symbol === symbolInfo.symbol && s.barType === symbolInfo.barType
    );
  }

  /** Toggle selezione di un simbolo+timeframe */
  toggleSymbol(symbolInfo: SymbolInfo): void {
    const index = this.selectedSymbols.findIndex(
      s => s.symbol === symbolInfo.symbol && s.barType === symbolInfo.barType
    );
    
    if (index > -1) {
      this.selectedSymbols.splice(index, 1);
    } else {
      this.selectedSymbols.push({
        symbol: symbolInfo.symbol,
        barType: symbolInfo.barType
      });
    }
  }

  /** Seleziona tutti i simboli disponibili */
  selectAllSymbols(): void {
    this.selectedSymbols = this.symbols.map(s => ({
      symbol: s.symbol,
      barType: s.barType
    }));
  }

  /** Deseleziona tutti i simboli */
  deselectAllSymbols(): void {
    this.selectedSymbols = [];
  }

  /** Formatta il nome del timeframe per la visualizzazione */
  formatBarType(barType: string): string {
    const barTypeMap: { [key: string]: string } = {
      'OneMinute': '1 min',
      'FiveMinute': '5 min',
      'FifteenMinute': '15 min',
      'ThirtyMinute': '30 min',
      'OneHour': '1 ora',
      'FourHour': '4 ore',
      'Daily': 'Giornaliero',
      'Weekly': 'Settimanale'
    };
    return barTypeMap[barType] || barType;
  }

  normalizeWeights(): void {
    const total = this.optimizationParams.returnWeight +
                  this.optimizationParams.sharpeWeight +
                  this.optimizationParams.profitDrawdownRatioWeight +
                  this.optimizationParams.winRateWeight +
                  this.optimizationParams.profitFactorWeight +
                  this.optimizationParams.consistencyWeight;
    
    if (total > 0) {
      this.optimizationParams.returnWeight /= total;
      this.optimizationParams.sharpeWeight /= total;
      this.optimizationParams.profitDrawdownRatioWeight /= total;
      this.optimizationParams.winRateWeight /= total;
      this.optimizationParams.profitFactorWeight /= total;
      this.optimizationParams.consistencyWeight /= total;
    }
  }

  getTotalWeight(): number {
    return this.optimizationParams.returnWeight +
           this.optimizationParams.sharpeWeight +
           this.optimizationParams.profitDrawdownRatioWeight +
           this.optimizationParams.winRateWeight +
           this.optimizationParams.profitFactorWeight +
           this.optimizationParams.consistencyWeight;
  }

  runOptimization(): void {
    if (!this.setupName) {
      this.error = 'Inserisci un nome per il setup';
      return;
    }

    if (this.selectedSymbols.length === 0) {
      this.error = 'Seleziona almeno un simbolo';
      return;
    }

    this.isOptimizing = true;
    this.error = null;
    this.response = null;

    const request: OptimizationRequest = {
      setupName: this.setupName,
      description: this.description,
      symbols: this.selectedSymbols,
      evaluationPeriod: this.evaluationPeriod,
      optimizationParams: this.optimizationParams,
      riskParams: this.riskParams,
      algorithmSettings: this.algorithmSettings
    };

    this.api.optimize(request).subscribe({
      next: (response) => {
        this.response = response;
        this.isOptimizing = false;
        this.activeTab = 'results';
        this.loadSavedSetups(); // Ricarica la lista
      },
      error: (err) => {
        this.error = err.error?.statusMessage || err.message || 'Errore durante l\'ottimizzazione';
        this.isOptimizing = false;
      }
    });
  }

  deleteSetup(id: string): void {
    if (confirm('Sei sicuro di voler eliminare questo setup?')) {
      this.api.deleteSetup(id).subscribe({
        next: () => this.loadSavedSetups(),
        error: (err) => this.error = 'Errore eliminazione setup'
      });
    }
  }

  formatPercent(value: number): string {
    return (value * 100).toFixed(1) + '%';
  }

  formatNumber(value: number, decimals: number = 2): string {
    return value?.toFixed(decimals) || '0';
  }

  /** Formatta la lista di simboli per la visualizzazione */
  formatSetupSymbols(symbols: SymbolSelection[]): string {
    if (!symbols || symbols.length === 0) return '-';
    return symbols.map(s => `${s.symbol} (${this.formatBarType(s.barType)})`).join(', ');
  }
}
