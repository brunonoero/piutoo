import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { BacktestingService } from '../../services/backtesting.service';
import { SettingsService } from '../../services/settings.service';
import { ApiService } from '../../services/api.service';
import {
  BacktestingRequest,
  BacktestingJob,
  BacktestingJobStatus
} from '../../models/backtesting-new.models';
import { StrategyDefinition } from '../../models/optimization.models';

@Component({
  selector: 'app-backtesting-request',
  templateUrl: './backtesting-request.component.html',
  styleUrls: ['./backtesting-request.component.css'],
  standalone: false
})
export class BacktestingRequestComponent implements OnInit, OnDestroy {
  // Form fields
  selectedSymbols: string[] = [];
  startDate = '';
  endDate = '';
  initialCapital = 10000;
  commissionPerContract = 2.0;
  name = '';
  strategyTextFilter = '';
  selectedStrategyIds: string[] = [];

  // Data
  availableSymbols: string[] = [];
  availableStrategies: StrategyDefinition[] = [];

  // State
  isRunning = false;
  currentJobId: string | null = null;
  currentJob: BacktestingJob | null = null;
  error: string | null = null;

  // Polling
  private pollingSubscription: any = null;

  constructor(
    private router: Router,
    private backtestingService: BacktestingService,
    private settingsService: SettingsService,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    this.loadSymbols();
    this.loadStrategies();
    this.setDefaultDates();
  }

  ngOnDestroy(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
    }
  }

  loadSymbols(): void {
    this.settingsService.getAvailableSymbols().subscribe({
      next: (symbols) => {
        this.availableSymbols = symbols || [];
      },
      error: (err) => {
        console.error('Errore caricamento simboli:', err);
        this.error = 'Errore durante il caricamento dei simboli';
      }
    });
  }

  loadStrategies(): void {
    this.apiService.getAllStrategies().subscribe({
      next: (strategies) => {
        this.availableStrategies = strategies || [];
      },
      error: (err) => {
        console.error('Errore caricamento strategie:', err);
        this.error = 'Errore durante il caricamento delle strategie';
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

  getFilteredStrategies(): StrategyDefinition[] {
    const filter = this.strategyTextFilter.trim().toLowerCase();

    return this.availableStrategies.filter(strategy => {
      const symbolMatches = this.selectedSymbols.length === 0 || this.selectedSymbols.includes(strategy.symbol);
      const textMatches = !filter || [
        strategy.name,
        strategy.id,
        strategy.fileName,
        strategy.symbol,
        strategy.description
      ].some(value => (value || '').toLowerCase().includes(filter));

      return symbolMatches && textMatches;
    });
  }

  toggleStrategy(strategyId: string): void {
    const index = this.selectedStrategyIds.indexOf(strategyId);
    if (index > -1) {
      this.selectedStrategyIds.splice(index, 1);
    } else {
      this.selectedStrategyIds.push(strategyId);
    }
  }

  isStrategySelected(strategyId: string): boolean {
    return this.selectedStrategyIds.includes(strategyId);
  }

  selectFilteredStrategies(): void {
    const ids = this.getFilteredStrategies().map(strategy => strategy.id);
    this.selectedStrategyIds = Array.from(new Set([...this.selectedStrategyIds, ...ids]));
  }

  deselectFilteredStrategies(): void {
    const visibleIds = new Set(this.getFilteredStrategies().map(strategy => strategy.id));
    this.selectedStrategyIds = this.selectedStrategyIds.filter(id => !visibleIds.has(id));
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
    this.currentJob = null;

    const selectedStrategyIds = this.selectedStrategyIds.length > 0
      ? this.selectedStrategyIds
      : this.strategyTextFilter.trim()
        ? this.getFilteredStrategies().map(strategy => strategy.id)
        : [];

    const request: BacktestingRequest = {
      selectedSymbols: this.selectedSymbols,
      selectedStrategyIds,
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

  startPolling(): void {
    if (!this.currentJobId) {
      this.isRunning = false;
      return;
    }

    this.pollingSubscription = this.backtestingService.pollJobStatus(this.currentJobId).subscribe({
      next: (job) => {
        if (!job) {
          // Job completato e rimosso, naviga al dettaglio
          this.navigateToResult();
          return;
        }

        this.currentJob = job;

        if (job.status === BacktestingJobStatus.Completed) {
          this.stopPolling();
          this.navigateToResult();
        } else if (job.status === BacktestingJobStatus.Failed) {
          this.stopPolling();
          this.isRunning = false;
          this.error = job.errorMessage || 'Backtesting fallito';
        }
      },
      error: (err) => {
        this.stopPolling();
        
        if (err.status === 404) {
          // Job potrebbe essere completato
          this.navigateToResult();
        } else {
          this.isRunning = false;
          this.error = 'Errore durante il polling: ' + err.message;
        }
      }
    });
  }

  stopPolling(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = null;
    }
  }

  navigateToResult(): void {
    if (this.currentJobId) {
      this.router.navigate(['/backtesting', this.currentJobId]);
    } else {
      this.router.navigate(['/backtesting']);
    }
  }

  navigateToList(): void {
    this.stopPolling();
    this.router.navigate(['/backtesting']);
  }

  cancelBacktesting(): void {
    this.stopPolling();
    this.isRunning = false;
    this.currentJobId = null;
    this.currentJob = null;
  }
}
