import { Component, OnInit, OnDestroy } from '@angular/core';
import { BacktestingService } from '../../services/backtesting.service';
import { SettingsService } from '../../services/settings.service';
import {
  BacktestingRequest,
  BacktestingJob,
  BacktestingResult,
  BacktestingJobStatus,
  StrategyInfo
} from '../../models/backtesting-new.models';

@Component({
  selector: 'app-backtesting-new',
  templateUrl: './backtesting-new.component.html',
  styleUrls: ['./backtesting-new.component.css'],
  standalone: false
})
export class BacktestingNewComponent implements OnInit, OnDestroy {
  // Form
  selectedSymbols: string[] = [];
  startDate = '';
  endDate = '';
  initialCapital = 10000;
  commissionPerContract = 2.0;
  name = '';

  // Data
  availableSymbols: string[] = [];

  // State
  isLoading = false;
  isRunning = false;
  currentJobId: string | null = null;
  currentJob: BacktestingJob | null = null;
  result: BacktestingResult | null = null;
  error: string | null = null;
  activeTab: 'config' | 'results' = 'config';
  
  // List view
  viewMode: 'list' | 'form' | 'detail' = 'list';
  backtestingList: BacktestingResult[] = [];
  selectedBacktesting: BacktestingResult | null = null;
  selectedBacktestingIds: Set<string> = new Set();

  // Polling
  private pollingSubscription: any = null;

  constructor(
    private backtestingService: BacktestingService,
    private settingsService: SettingsService
  ) {}

  ngOnInit(): void {
    console.log('BacktestingNewComponent ngOnInit chiamato');
    // Reset stato iniziale
    this.isRunning = false;
    this.currentJobId = null;
    this.currentJob = null;
    this.result = null;
    this.error = null;
    this.viewMode = 'list';
    
    console.log('Caricamento dati...');
    this.loadBacktestingList();
    this.loadSymbols();
    this.setDefaultDates();
  }

  ngOnDestroy(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
    }
  }

  loadSymbols(): void {
    console.log('loadSymbols chiamato');
    this.settingsService.getAvailableSymbols().subscribe({
      next: (symbols) => {
        console.log('Simboli ricevuti dal servizio:', symbols);
        this.availableSymbols = symbols || [];
        console.log('Simboli caricati:', this.availableSymbols.length, this.availableSymbols);
        if (this.availableSymbols.length === 0) {
          console.warn('Nessun simbolo disponibile');
        }
      },
      error: (err) => {
        console.error('Errore caricamento simboli:', err);
        console.error('Dettagli errore:', JSON.stringify(err, null, 2));
        this.error = 'Errore durante il caricamento dei simboli: ' + (err.error?.error || err.message || 'Errore sconosciuto');
      }
    });
  }

  setDefaultDates(): void {
    const today = new Date();
    const start = new Date(today.getFullYear() - 2, today.getMonth(), today.getDate());
    
    this.startDate = this.formatDateInputValue(start);
    this.endDate = this.formatDateInputValue(today);
  }

  private formatDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  toggleSymbol(symbol: string): void {
    const index = this.selectedSymbols.indexOf(symbol);
    if (index > -1) {
      this.selectedSymbols.splice(index, 1);
    } else {
      this.selectedSymbols.push(symbol);
    }
  }

  isSymbolSelected(symbol: string): boolean {
    return this.selectedSymbols.includes(symbol);
  }

  selectAllSymbols(): void {
    this.selectedSymbols = [...this.availableSymbols];
  }

  deselectAllSymbols(): void {
    this.selectedSymbols = [];
  }

  startBacktesting(): void {
    if (this.selectedSymbols.length === 0) {
      this.error = 'Seleziona almeno un simbolo';
      return;
    }

    if (!this.name.trim()) {
      this.error = 'Inserisci un nome per il backtesting';
      return;
    }

    if (!this.startDate || !this.endDate) {
      this.error = 'Seleziona le date di inizio e fine';
      return;
    }

    this.isRunning = true;
    this.error = null;
    this.result = null;
    this.currentJob = null;

    const request: BacktestingRequest = {
      selectedSymbols: this.selectedSymbols,
      startDate: this.startDate,
      endDate: this.endDate,
      initialCapital: this.initialCapital,
      commissionPerContract: this.commissionPerContract,
      name: this.name
    };

    this.backtestingService.startBacktesting(request).subscribe({
      next: (response) => {
        this.currentJobId = response.jobId;
        this.startPolling();
      },
      error: (err) => {
        this.error = err.error?.error || err.message || 'Errore durante l\'avvio del backtesting';
        this.isRunning = false;
      }
    });
  }

  loadResult(): void {
    if (!this.currentJobId) {
      this.isRunning = false;
      return;
    }

    this.isRunning = false;

    const job = this.currentJob;
    if (job?.result) {
      this.result = job.result;
      this.activeTab = 'results';
      if (this.viewMode === 'form') {
        this.viewMode = 'detail';
      }
      return;
    }

    this.backtestingService.getResult(this.currentJobId).subscribe({
      next: (result) => {
        this.result = result;
        this.activeTab = 'results';
        if (this.viewMode === 'form') {
          this.viewMode = 'detail';
        }
        this.isRunning = false;
      },
      error: (err) => {
        this.isRunning = false;
        console.warn('Errore durante il caricamento del risultato:', err);
        
        if (err.status === 404) {
          console.log('Cercando risultato nella lista dei backtesting completati per JobId:', this.currentJobId);
          this.backtestingService.getCompletedBacktestings().subscribe({
            next: (backtestings) => {
              console.log(`Trovati ${backtestings.length} backtesting completati`);
              
              let found = backtestings.find(b => 
                b.jobId && 
                b.jobId.toLowerCase() === this.currentJobId?.toLowerCase()
              );
              
              if (!found && backtestings.length > 0) {
                console.warn('JobId non trovato, cerco il risultato creato più di recente come fallback');
                // Ordina per createdAt (data di creazione del risultato) invece di startDate
                const sorted = backtestings.sort((a, b) => {
                  const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
                  const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
                  return dateB - dateA;
                });
                const mostRecent = sorted[0];
                // Applica fallback solo se il risultato è stato creato negli ultimi 5 minuti
                const createdAt = mostRecent.createdAt ? new Date(mostRecent.createdAt) : null;
                const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000);
                if (createdAt && createdAt > fiveMinutesAgo) {
                  found = mostRecent;
                  console.log('Fallback: risultato creato di recente - JobId:', found.jobId, 'SetupName:', found.setupName, 'CreatedAt:', found.createdAt);
                } else {
                  console.warn('Fallback NON applicato: nessun risultato creato di recente');
                }
              }
              
              if (found) {
                this.result = found;
                this.activeTab = 'results';
                if (this.viewMode === 'form') {
                  this.viewMode = 'detail';
                }
                console.log('Risultato caricato con successo');
              } else {
                console.error('Nessun risultato trovato per JobId:', this.currentJobId);
                this.error = 'Risultato non trovato. Il backtesting potrebbe non essere stato completato correttamente.';
              }
              this.isRunning = false;
            },
            error: (listErr) => {
              console.error('Errore durante il caricamento della lista dei backtesting:', listErr);
              this.error = 'Errore caricamento risultato: ' + err.message;
              this.isRunning = false;
            }
          });
        } else {
          this.error = 'Errore caricamento risultato: ' + err.message;
        }
      }
    });
  }

  startPolling(): void {
    if (!this.currentJobId) {
      this.isRunning = false;
      return;
    }

    this.pollingSubscription = this.backtestingService.pollJobStatus(this.currentJobId).subscribe({
      next: (job) => {
        if (!job) {
          // Job non trovato, potrebbe essere completato e rimosso
          // Prova a caricare il risultato direttamente
          this.isRunning = false;
          if (this.pollingSubscription) {
            this.pollingSubscription.unsubscribe();
            this.pollingSubscription = null;
          }
          this.loadResult();
          return;
        }

        this.currentJob = job;

        if (job.status === BacktestingJobStatus.Completed) {
          this.isRunning = false;
          // Ferma il polling prima di caricare il risultato
          if (this.pollingSubscription) {
            this.pollingSubscription.unsubscribe();
            this.pollingSubscription = null;
          }
          this.loadResult();
        } else if (job.status === BacktestingJobStatus.Failed) {
          this.isRunning = false;
          this.error = job.errorMessage || 'Backtesting fallito';
          // Ferma il polling
          if (this.pollingSubscription) {
            this.pollingSubscription.unsubscribe();
            this.pollingSubscription = null;
          }
        }
      },
      error: (err) => {
        // Se il job è completato, potrebbe essere stato rimosso dal dizionario
        // Prova a caricare il risultato direttamente
        this.isRunning = false;
        if (this.pollingSubscription) {
          this.pollingSubscription.unsubscribe();
          this.pollingSubscription = null;
        }
        
        if (err.status === 404) {
          console.warn('Job non trovato durante polling, potrebbe essere completato');
          this.loadResult();
        } else {
          this.error = 'Errore durante il polling: ' + err.message;
        }
      },
      complete: () => {
        // Polling completato normalmente - assicurati che isRunning sia false
        this.isRunning = false;
        console.log('Polling completato');
      }
    });
  }

  formatPercent(value: number): string {
    return (value * 100).toFixed(2) + '%';
  }

  formatCurrency(value: number): string {
    return '$' + value.toFixed(2);
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('it-IT', { 
      year: 'numeric', 
      month: '2-digit', 
      day: '2-digit' 
    });
  }

  getDistinctStrategies(strategies: string[]): string[] {
    if (!strategies || strategies.length === 0) return [];
    return [...new Set(strategies)].sort();
  }

  getDistinctStrategiesInfo(strategiesInfo?: StrategyInfo[]): StrategyInfo[] {
    if (strategiesInfo && strategiesInfo.length > 0) {
      // Rimuovi duplicati basati su nome, symbol e timeframe
      const unique = new Map<string, StrategyInfo>();
      strategiesInfo.forEach(s => {
        const key = `${s.name}|${s.symbol}|${s.timeframeMinutes}`;
        if (!unique.has(key)) {
          unique.set(key, s);
        }
      });
      return Array.from(unique.values()).sort((a, b) => {
        if (a.name !== b.name) return a.name.localeCompare(b.name);
        if (a.symbol !== b.symbol) return a.symbol.localeCompare(b.symbol);
        return a.timeframeMinutes - b.timeframeMinutes;
      });
    }
    // Fallback: se strategiesInfo non è disponibile, prova a estrarre da strategiesUsed
    // Questo è per retrocompatibilità con vecchi risultati
    return [];
  }

  getStrategiesDisplay(result: BacktestingResult): Array<{name: string, symbol: string, timeframe: number}> {
    if (result.strategiesInfo && result.strategiesInfo.length > 0) {
      const unique = new Map<string, StrategyInfo>();
      result.strategiesInfo.forEach(s => {
        const key = `${s.name}|${s.symbol}|${s.timeframeMinutes}`;
        if (!unique.has(key)) {
          unique.set(key, s);
        }
      });
      return Array.from(unique.values()).map(s => ({
        name: s.name,
        symbol: s.symbol,
        timeframe: s.timeframeMinutes
      })).sort((a, b) => {
        if (a.name !== b.name) return a.name.localeCompare(b.name);
        if (a.symbol !== b.symbol) return a.symbol.localeCompare(b.symbol);
        return a.timeframe - b.timeframe;
      });
    }
    // Fallback per retrocompatibilità: prova a estrarre da strategiesUsed
    // Pattern supportati:
    // - "Easy_643_FDAX_60" -> name="Easy_643", symbol="FDAX", timeframe=60
    // - "TOP_UA_643_FDAX_60" -> name="TOP_UA_643", symbol="FDAX", timeframe=60
    // - "TOP_UA_643" -> name="TOP_UA_643", symbol="", timeframe=0 (non contiene symbol/timeframe)
    const strategies = [...new Set(result.strategiesUsed || [])];
    
    // Lista di simboli validi per verificare se il pattern estratto è un symbol reale
    const validSymbols = ['FDAX', 'FNQ', 'FCL', 'FGC', 'DAX', 'NQ', 'CL', 'GC', 'ES', 'YM', 'ZB', 'ZN', 'ZS', 'ZC', 'ZW', 'HE', 'LE', 'KC', 'CT', 'SB', 'HO', 'RB', 'NG'];
    
    return strategies.map(name => {
      // Pattern: cerca l'ultimo pattern _SYMBOL_TIMEFRAME dove SYMBOL è un simbolo valido e TIMEFRAME è un numero
      // Esempi: "Easy_643_FDAX_60", "TOP_UA_643_FDAX_60"
      const fullMatch = name.match(/^(.+)_([A-Z]{2,5})_(\d+)$/);
      if (fullMatch) {
        const strategyPart = fullMatch[1];
        const symbolPart = fullMatch[2];
        const timeframePart = parseInt(fullMatch[3], 10);
        
        // Verifica che symbolPart sia un simbolo valido e timeframePart sia ragionevole
        if (validSymbols.includes(symbolPart) && timeframePart >= 1 && timeframePart <= 10080) {
          return {
            name: strategyPart,
            symbol: symbolPart,
            timeframe: timeframePart
          };
        }
      }
      
      // Se non corrisponde al pattern o il symbol non è valido, mostra solo il nome
      return {
        name: name,
        symbol: '',
        timeframe: 0
      };
    }).sort((a, b) => {
      if (a.name !== b.name) return a.name.localeCompare(b.name);
      if (a.symbol !== b.symbol) return a.symbol.localeCompare(b.symbol);
      return a.timeframe - b.timeframe;
    });
  }

  loadBacktestingList(): void {
    console.log('loadBacktestingList chiamato');
    this.isLoading = true;
    this.backtestingService.getCompletedBacktestings().subscribe({
      next: (backtestings) => {
        console.log('Backtesting ricevuti dal servizio:', backtestings);
        this.backtestingList = backtestings || [];
        console.log('Backtesting caricati:', this.backtestingList.length, this.backtestingList);
        this.isLoading = false;
        if (this.backtestingList.length === 0) {
          console.warn('Nessun backtesting trovato');
        }
      },
      error: (err) => {
        console.error('Errore caricamento lista backtesting:', err);
        console.error('Dettagli errore:', JSON.stringify(err, null, 2));
        this.error = 'Errore durante il caricamento della lista: ' + (err.error?.error || err.message || 'Errore sconosciuto');
        this.isLoading = false;
      }
    });
  }

  toggleBacktestingSelection(backtesting: BacktestingResult): void {
    if (this.selectedBacktestingIds.has(backtesting.jobId)) {
      this.selectedBacktestingIds.delete(backtesting.jobId);
    } else {
      this.selectedBacktestingIds.add(backtesting.jobId);
    }
  }

  isBacktestingSelected(backtesting: BacktestingResult): boolean {
    return this.selectedBacktestingIds.has(backtesting.jobId);
  }

  selectAllBacktestings(): void {
    this.backtestingList.forEach(bt => this.selectedBacktestingIds.add(bt.jobId));
  }

  deselectAllBacktestings(): void {
    this.selectedBacktestingIds.clear();
  }

  deleteSelectedBacktestings(): void {
    if (this.selectedBacktestingIds.size === 0) {
      this.error = 'Seleziona almeno un backtesting da eliminare';
      return;
    }

    if (!confirm(`Sei sicuro di voler eliminare ${this.selectedBacktestingIds.size} backtesting?`)) {
      return;
    }

    const deletePromises = Array.from(this.selectedBacktestingIds).map(jobId =>
      this.backtestingService.deleteBacktesting(jobId).toPromise()
    );

    Promise.all(deletePromises).then(() => {
      this.selectedBacktestingIds.clear();
      this.loadBacktestingList();
      if (this.viewMode === 'detail' && this.result && this.selectedBacktestingIds.has(this.result.jobId)) {
        this.viewMode = 'list';
        this.result = null;
      }
    }).catch((err) => {
      console.error('Errore durante l\'eliminazione:', err);
      this.error = 'Errore durante l\'eliminazione dei backtesting';
      this.loadBacktestingList();
    });
  }

  deleteBacktesting(backtesting: BacktestingResult): void {
    if (!confirm(`Sei sicuro di voler eliminare il backtesting "${backtesting.setupName}"?`)) {
      return;
    }

    this.backtestingService.deleteBacktesting(backtesting.jobId).subscribe({
      next: () => {
        this.loadBacktestingList();
        if (this.viewMode === 'detail' && this.result?.jobId === backtesting.jobId) {
          this.viewMode = 'list';
          this.result = null;
        }
      },
      error: (err) => {
        console.error('Errore durante l\'eliminazione:', err);
        this.error = 'Errore durante l\'eliminazione: ' + err.message;
      }
    });
  }

  viewBacktestingDetail(backtesting: BacktestingResult): void {
    this.selectedBacktesting = backtesting;
    this.currentJobId = backtesting.jobId;
    this.viewMode = 'detail';
    this.loadResult();
  }

  showNewBacktestingForm(): void {
    this.viewMode = 'form';
    this.result = null;
    this.selectedBacktesting = null;
    this.currentJobId = null;
    this.error = null;
  }

  backToList(): void {
    this.viewMode = 'list';
    this.result = null;
    this.selectedBacktesting = null;
    this.currentJobId = null;
    this.activeTab = 'config';
  }
}
