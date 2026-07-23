import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ApiService } from '../../services/api.service';
import { BacktestingService } from '../../services/backtesting.service';
import { OptimizationService } from '../../services/optimization.service';
import { BacktestingResult } from '../../models/backtesting-new.models';
import {
  OptimizationRequest,
  OptimizationPreset,
  OptimizationParameters,
  RiskParameters,
  EvaluationPeriod,
  AlgorithmSettings,
  PeriodType,
  OptimizationObjective,
  SymbolSelection,
  FilteredBacktestingResult,
  AdvancedOptimizationRequest,
  AdvancedOptimizationResult,
  OptimizationJobStatus,
  OptimizationJob
} from '../../models/optimization.models';

@Component({
  selector: 'app-optimization-request',
  templateUrl: './optimization-request.component.html',
  styleUrls: ['./optimization-request.component.css'],
  standalone: false
})
export class OptimizationRequestComponent implements OnInit, OnDestroy {
  // Form data
  setupName = '';
  description = '';
  selectedBacktestingId: string | null = null;
  
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
  backtestings: BacktestingResult[] = [];
  
  // State
  isOptimizing = false;
  isLoadingBacktestings = false;
  error: string | null = null;
  activeTab: 'params' | 'risk' | 'algorithm' = 'params';
  
  // Job tracking
  currentJobId: string | null = null;
  currentJob: OptimizationJob | null = null;
  progressPercent = 0;
  currentStep = '';

  // Enums for template
  periodTypes = PeriodType;
  objectives = OptimizationObjective;
  Math = Math;

  // Cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private api: ApiService,
    private backtestingService: BacktestingService,
    private optimizationService: OptimizationService,
    private router: Router
  ) {}
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  ngOnInit(): void {
    this.loadPresets();
    this.loadBacktestings();
  }

  loadPresets(): void {
    this.api.getPresets().subscribe({
      next: (presets) => this.presets = presets,
      error: (err) => console.error('Errore caricamento preset:', err)
    });
  }

  loadBacktestings(): void {
    this.isLoadingBacktestings = true;
    this.backtestingService.getCompletedBacktestings().subscribe({
      next: (backtestings) => {
        this.backtestings = backtestings.sort((a, b) => {
          const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
          const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
          return dateB - dateA; // Più recenti prima
        });
        this.isLoadingBacktestings = false;
      },
      error: (err) => {
        console.error('Errore caricamento backtestings:', err);
        this.isLoadingBacktestings = false;
      }
    });
  }

  getSelectedBacktesting(): BacktestingResult | null {
    if (!this.selectedBacktestingId) return null;
    return this.backtestings.find(b => b.jobId === this.selectedBacktestingId) || null;
  }

  applyPreset(preset: OptimizationPreset): void {
    this.optimizationParams = { ...preset.optimizationParams };
    this.riskParams = { ...preset.riskParams };
  }

  normalizeWeights(): void {
    const total = this.getTotalWeight();
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

    if (!this.selectedBacktestingId) {
      this.error = 'Seleziona un backtesting';
      return;
    }

    const selectedBt = this.getSelectedBacktesting();
    if (!selectedBt) {
      this.error = 'Backtesting non trovato';
      return;
    }

    // Estrai i simboli dal backtesting
    const symbols = this.extractSymbolsFromBacktesting(selectedBt);
    if (symbols.length === 0) {
      this.error = 'Nessun simbolo trovato nel backtesting selezionato';
      return;
    }

    this.isOptimizing = true;
    this.error = null;
    this.progressPercent = 0;
    this.currentStep = 'Avvio ottimizzazione...';

    const request: OptimizationRequest = {
      setupName: this.setupName,
      description: this.description,
      backtestingId: this.selectedBacktestingId!,
      symbols: symbols,
      evaluationPeriod: this.evaluationPeriod,
      optimizationParams: this.optimizationParams,
      riskParams: this.riskParams,
      algorithmSettings: this.algorithmSettings
    };

    console.log('Optimization Request:', JSON.stringify(request, null, 2));

    // Avvia il job in background
    this.optimizationService.startOptimization(request).subscribe({
      next: (response) => {
        this.currentJobId = response.jobId;
        console.log('Job avviato:', response.jobId);
        
        // Inizia il polling
        this.pollJobStatus(false);
      },
      error: (err) => {
        console.error('Errore avvio ottimizzazione:', err);
        this.error = err.error?.Error || err.error?.message || err.message || 'Errore durante l\'avvio dell\'ottimizzazione';
        this.isOptimizing = false;
      }
    });
  }

  /**
   * Esegue ottimizzazione AVANZATA con algoritmi sofisticati
   */
  runAdvancedOptimization(): void {
    if (!this.selectedBacktestingId) {
      this.error = 'Seleziona un backtesting';
      return;
    }

    this.isOptimizing = true;
    this.error = null;
    this.progressPercent = 0;
    this.currentStep = 'Avvio ottimizzazione avanzata...';

    const request: AdvancedOptimizationRequest = {
      backtestingId: this.selectedBacktestingId,
      lookbackWeeks: this.evaluationPeriod.weeks,
      filterConfig: {
        minWinRate: this.riskParams.minWinRate,
        maxDrawdownLimit: this.riskParams.maxDrawdown,
        minSharpeRatio: this.riskParams.minSharpeRatio,
        minTrades: this.riskParams.minTrades,
        maxCorrelation: 0.7 // Default
      }
    };

    console.log('Advanced Optimization Request:', JSON.stringify(request, null, 2));

    // Avvia il job in background
    this.optimizationService.startAdvancedOptimization(request).subscribe({
      next: (response) => {
        this.currentJobId = response.jobId;
        console.log('Job avanzato avviato:', response.jobId);
        
        // Inizia il polling
        this.pollJobStatus(true);
      },
      error: (err) => {
        console.error('Errore avvio ottimizzazione avanzata:', err);
        this.error = err.error?.Error || err.error?.message || err.message || 'Errore durante l\'avvio dell\'ottimizzazione avanzata';
        this.isOptimizing = false;
      }
    });
  }

  /**
   * Polling dello stato del job fino al completamento
   */
  private pollJobStatus(isAdvanced: boolean): void {
    if (!this.currentJobId) return;

    this.optimizationService.pollJobStatus(this.currentJobId, 1000)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (job) => {
          this.currentJob = job;
          this.progressPercent = job.progressPercent;
          this.currentStep = job.currentStep || 'Elaborazione...';
          
          // Controlla se completato o fallito
          if (job.status === OptimizationJobStatus.Completed) {
            this.onJobCompleted(job, isAdvanced);
          } else if (job.status === OptimizationJobStatus.Failed) {
            this.onJobFailed(job);
          }
        },
        error: (err) => {
          console.error('Errore polling:', err);
          this.error = 'Errore durante il monitoraggio dell\'ottimizzazione';
          this.isOptimizing = false;
        }
      });
  }

  /**
   * Gestisce il completamento del job
   */
  private onJobCompleted(job: OptimizationJob, isAdvanced: boolean): void {
    this.isOptimizing = false;
    console.log('Job completato:', job);
    
    // Naviga al dettaglio usando il jobId
    // Il risultato viene passato via state per evitare un'altra chiamata API
    if (isAdvanced) {
      if (job.advancedResult) {
        console.log('Navigando al dettaglio avanzato con risultato dal job');
        this.router.navigate(['/optimization', job.jobId], { 
          state: { advancedResult: job.advancedResult } 
        });
      } else {
        console.log('Navigando al dettaglio avanzato (caricamento da API)');
        this.router.navigate(['/optimization', job.jobId]);
      }
    } else {
      if (job.basicResult) {
        console.log('Navigando al dettaglio con risultato dal job');
        this.router.navigate(['/optimization', job.jobId], { 
          state: { result: job.basicResult } 
        });
      } else {
        console.log('Navigando al dettaglio (caricamento da API)');
        this.router.navigate(['/optimization', job.jobId]);
      }
    }
  }

  /**
   * Gestisce il fallimento del job
   */
  private onJobFailed(job: OptimizationJob): void {
    this.isOptimizing = false;
    this.error = job.errorMessage || 'Ottimizzazione fallita';
  }

  /**
   * Estrae i simboli unici dal backtesting selezionato
   */
  extractSymbolsFromBacktesting(bt: BacktestingResult): SymbolSelection[] {
    const symbolMap = new Map<string, SymbolSelection>();

    // Prima prova con strategiesInfo se disponibile
    if (bt.strategiesInfo && bt.strategiesInfo.length > 0) {
      bt.strategiesInfo.forEach(si => {
        const barType = this.timeframeToBarType(si.timeframeMinutes);
        const key = `${si.symbol}|${barType}`;
        if (!symbolMap.has(key)) {
          symbolMap.set(key, {
            symbol: si.symbol,
            barType: barType
          });
        }
      });
    }

    // Fallback: estrai dai nomi delle strategie (es. "Easy_123_ES_15" -> ES, 15min)
    if (symbolMap.size === 0 && bt.strategiesUsed && bt.strategiesUsed.length > 0) {
      const validSymbols = ['ES', 'NQ', 'CL', 'GC', 'FDAX', 'FNQ', 'FCL', 'FGC', 'YM', 'ZB', 'ZN'];
      
      bt.strategiesUsed.forEach(strategyName => {
        // Pattern: nome_simbolo_timeframe
        const match = strategyName.match(/_([A-Z]{2,5})_(\d+)$/);
        if (match) {
          const symbol = match[1];
          const timeframe = parseInt(match[2], 10);
          if (validSymbols.includes(symbol) && timeframe >= 1 && timeframe <= 1440) {
            const barType = this.timeframeToBarType(timeframe);
            const key = `${symbol}|${barType}`;
            if (!symbolMap.has(key)) {
              symbolMap.set(key, {
                symbol: symbol,
                barType: barType
              });
            }
          }
        }
      });
    }

    return Array.from(symbolMap.values());
  }

  /**
   * Converte minuti in BarType string
   */
  timeframeToBarType(minutes: number): string {
    switch (minutes) {
      case 1: return 'OneMinute';
      case 5: return 'FiveMinute';
      case 15: return 'FifteenMinute';
      case 30: return 'ThirtyMinute';
      case 60: return 'OneHour';
      case 240: return 'FourHour';
      case 1440: return 'Daily';
      default: return 'FifteenMinute'; // Default
    }
  }

  navigateToList(): void {
    this.router.navigate(['/optimization']);
  }

  formatPercent(value: number): string {
    return (value * 100).toFixed(1) + '%';
  }

  formatDate(dateString: string | undefined): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('it-IT', { 
      year: 'numeric', 
      month: '2-digit', 
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatCurrency(value: number): string {
    return '$' + value.toFixed(2);
  }
}
