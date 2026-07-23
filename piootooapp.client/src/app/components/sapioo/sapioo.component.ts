import { Component, OnInit, OnDestroy } from '@angular/core';
import { SapiooService } from '../../services/sapioo.service';
import {
  SapiooRequest,
  SapiooJob,
  SapiooResult,
  SapiooJobStatus,
  RiskManagementParams,
  SapiooPreset
} from '../../models/sapioo.models';

@Component({
  selector: 'app-sapioo',
  templateUrl: './sapioo.component.html',
  styleUrls: ['./sapioo.component.css'],
  standalone: false
})
export class SapiooComponent implements OnInit, OnDestroy {
  // Form
  selectedBacktesting = '';
  name = '';
  evaluationPeriodWeeks = 4;
  riskParams: RiskManagementParams = {
    maxDrawdownPercent: 20,
    minWinRate: 40,
    minProfitFactor: 1.2,
    maxConsecutiveLosses: 5,
    minSharpeRatio: 0.5,
    minRecoveryFactor: 1.0,
    maxVolatility: 30,
    weeksLookback: 4
  };

  // Data
  availableBacktestings: string[] = [];
  presets: SapiooPreset[] = [];

  // State
  isLoading = false;
  isRunning = false;
  currentJobId: string | null = null;
  currentJob: SapiooJob | null = null;
  result: SapiooResult | null = null;
  error: string | null = null;
  activeTab: 'config' | 'results' = 'config';
  
  // List view
  viewMode: 'list' | 'form' | 'detail' = 'list';
  optimizationList: SapiooResult[] = [];
  selectedOptimization: SapiooResult | null = null;
  selectedOptimizationIds: Set<string> = new Set();

  // Polling
  private pollingSubscription: any = null;

  constructor(private sapiooService: SapiooService) {}

  ngOnInit(): void {
    this.viewMode = 'list';
    this.loadOptimizationList();
    this.loadAvailableBacktestings();
    this.initializePresets();
  }

  initializePresets(): void {
    this.presets = [
      {
        name: 'Conservativo',
        description: 'Filtri molto restrittivi per massima sicurezza',
        riskParams: {
          maxDrawdownPercent: 15,
          minWinRate: 50,
          minProfitFactor: 1.5,
          maxConsecutiveLosses: 3,
          minSharpeRatio: 1.0,
          minRecoveryFactor: 2.0,
          maxVolatility: 20,
          weeksLookback: 6
        }
      },
      {
        name: 'Bilanciato',
        description: 'Equilibrio tra sicurezza e performance',
        riskParams: {
          maxDrawdownPercent: 20,
          minWinRate: 40,
          minProfitFactor: 1.2,
          maxConsecutiveLosses: 5,
          minSharpeRatio: 0.5,
          minRecoveryFactor: 1.0,
          maxVolatility: 30,
          weeksLookback: 4
        }
      },
      {
        name: 'Aggressivo',
        description: 'Filtri più permissivi per massimizzare i rendimenti',
        riskParams: {
          maxDrawdownPercent: 30,
          minWinRate: 35,
          minProfitFactor: 1.1,
          maxConsecutiveLosses: 7,
          minSharpeRatio: 0.3,
          minRecoveryFactor: 0.5,
          maxVolatility: 50,
          weeksLookback: 2
        }
      },
      {
        name: 'Alta Frequenza',
        description: 'Ottimizzato per strategie ad alta frequenza',
        riskParams: {
          maxDrawdownPercent: 25,
          minWinRate: 45,
          minProfitFactor: 1.3,
          maxConsecutiveLosses: 4,
          minSharpeRatio: 0.8,
          minRecoveryFactor: 1.5,
          maxVolatility: 35,
          weeksLookback: 3
        }
      },
      {
        name: 'Trend Following',
        description: 'Configurazione per strategie di trend following',
        riskParams: {
          maxDrawdownPercent: 25,
          minWinRate: 38,
          minProfitFactor: 1.4,
          maxConsecutiveLosses: 6,
          minSharpeRatio: 0.6,
          minRecoveryFactor: 1.2,
          maxVolatility: 40,
          weeksLookback: 5
        }
      }
    ];
  }

  applyPreset(preset: SapiooPreset): void {
    this.riskParams = { ...preset.riskParams };
    // Aggiorna anche il periodo di valutazione se presente nel preset
    if (preset.evaluationPeriodWeeks) {
      this.evaluationPeriodWeeks = preset.evaluationPeriodWeeks;
    }
  }

  ngOnDestroy(): void {
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
    }
  }

  loadAvailableBacktestings(): void {
    this.sapiooService.getAvailableBacktestings().subscribe({
      next: (backtestings) => {
        this.availableBacktestings = backtestings;
        if (backtestings.length > 0 && !this.selectedBacktesting) {
          this.selectedBacktesting = backtestings[0];
          this.name = `Sapioo - ${backtestings[0]}`;
        }
      },
      error: (err) => {
        this.error = 'Errore caricamento backtestings: ' + err.message;
      }
    });
  }

  startOptimization(): void {
    if (!this.selectedBacktesting) {
      this.error = 'Seleziona un backtesting';
      return;
    }

    if (!this.name.trim()) {
      this.error = 'Inserisci un nome per l\'ottimizzazione';
      return;
    }

    this.isRunning = true;
    this.error = null;
    this.result = null;
    this.currentJob = null;

    const request: SapiooRequest = {
      backtestingName: this.selectedBacktesting,
      riskParams: this.riskParams,
      name: this.name,
      evaluationPeriodWeeks: this.evaluationPeriodWeeks
    };

    this.sapiooService.startOptimization(request).subscribe({
      next: (response) => {
        this.currentJobId = response.jobId;
        this.startPolling();
      },
      error: (err) => {
        this.error = err.error?.error || err.message || 'Errore durante l\'avvio dell\'ottimizzazione';
        this.isRunning = false;
      }
    });
  }

  startPolling(): void {
    if (!this.currentJobId) {
      this.isRunning = false;
      return;
    }

    this.pollingSubscription = this.sapiooService.pollJobStatus(this.currentJobId).subscribe({
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

        if (job.status === SapiooJobStatus.Completed) {
          this.isRunning = false;
          // Ferma il polling prima di caricare il risultato
          if (this.pollingSubscription) {
            this.pollingSubscription.unsubscribe();
            this.pollingSubscription = null;
          }
          // Se il job ha già il risultato, usalo direttamente
          if (job.result) {
            console.log('Risultato trovato nel job, uso quello');
            this.result = job.result;
            this.activeTab = 'results';
            if (this.viewMode === 'form') {
              this.viewMode = 'detail';
            }
          } else {
            // Altrimenti carica il risultato (con retry)
            console.log('Job completato ma senza risultato, carico il risultato');
            // Aspetta un po' per dare tempo al server di salvare il file
            setTimeout(() => {
              this.loadResult();
            }, 500);
          }
        } else if (job.status === SapiooJobStatus.Failed) {
          this.isRunning = false;
          this.error = job.errorMessage || 'Ottimizzazione fallita';
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
          // Aspetta un po' per dare tempo al server di salvare il file
          setTimeout(() => {
            this.loadResult();
          }, 1000);
        } else {
          this.error = 'Errore durante il polling: ' + err.message;
        }
      },
      complete: () => {
        // Polling completato normalmente - assicurati che isRunning sia false
        this.isRunning = false;
      }
    });
  }

  loadResult(retryCount: number = 0): void {
    if (!this.currentJobId) {
      console.warn('loadResult chiamato senza currentJobId');
      return;
    }

    console.log(`loadResult chiamato per JobId: ${this.currentJobId}, tentativo: ${retryCount + 1}`);

    this.sapiooService.getResult(this.currentJobId).subscribe({
      next: (result) => {
        console.log('Risultato ricevuto dal servizio:', result);
        if (result && result.jobId) {
          this.result = result;
          this.activeTab = 'results';
          if (this.viewMode === 'form') {
            this.viewMode = 'detail';
          }
          this.error = null;
          console.log('Risultato caricato con successo');
        } else {
          console.warn('Risultato ricevuto ma incompleto:', result);
          this.tryLoadFromList();
        }
      },
      error: (err) => {
        console.error('Errore caricamento risultato:', err);
        console.error('Dettagli errore:', JSON.stringify(err, null, 2));
        
        // Se il risultato non viene trovato, prova a cercarlo nella lista delle ottimizzazioni completate
        if (err.status === 404) {
          // Se è il primo tentativo, aspetta un po' e riprova (potrebbe essere ancora in salvataggio)
          if (retryCount < 3) {
            console.log(`Risultato non trovato, riprovo tra 1 secondo (tentativo ${retryCount + 1}/3)`);
            setTimeout(() => {
              this.loadResult(retryCount + 1);
            }, 1000);
            return;
          }
          
          console.warn('Risultato non trovato dopo 3 tentativi, cercando nella lista delle ottimizzazioni completate per JobId:', this.currentJobId);
          this.tryLoadFromList();
        } else {
          this.error = 'Errore caricamento risultato: ' + (err.error?.error || err.message || 'Errore sconosciuto');
        }
      }
    });
  }

  private tryLoadFromList(): void {
    console.log('Cercando risultato nella lista delle ottimizzazioni completate per JobId:', this.currentJobId);
    this.sapiooService.getCompletedOptimizations().subscribe({
      next: (optimizations) => {
        console.log(`Trovate ${optimizations.length} ottimizzazioni completate`);
        console.log('Lista JobId trovati:', optimizations.map(o => o.jobId));
        
        // Cerca con confronto case-insensitive
        let found = optimizations.find(opt => 
          opt.jobId && 
          opt.jobId.toLowerCase() === this.currentJobId?.toLowerCase()
        );
        
        if (found) {
          console.log('Risultato trovato nella lista - JobId:', found.jobId, 'BacktestingName:', found.backtestingName);
        } else {
          console.warn('JobId non trovato nella lista. JobId cercato:', this.currentJobId);
          console.log('JobId disponibili:', optimizations.map(o => `'${o.jobId}'`).join(', '));
        }
        
        // Se non trovato, prova a usare il risultato più recente come fallback
        if (!found && optimizations.length > 0) {
          console.warn('JobId non trovato nella lista, uso il risultato più recente come fallback');
          found = optimizations.sort((a, b) => {
            const dateA = a.finalResult?.dateTime ? new Date(a.finalResult.dateTime).getTime() : 0;
            const dateB = b.finalResult?.dateTime ? new Date(b.finalResult.dateTime).getTime() : 0;
            return dateB - dateA;
          })[0];
          console.log('Fallback: risultato più recente - JobId:', found.jobId, 'BacktestingName:', found.backtestingName);
        }
        
        if (found) {
          this.result = found;
          this.activeTab = 'results';
          if (this.viewMode === 'form') {
            this.viewMode = 'detail';
          }
          this.error = null;
          console.log('Risultato caricato con successo dalla lista');
        } else {
          console.error('Nessun risultato trovato per JobId:', this.currentJobId);
          this.error = 'Risultato non trovato. L\'ottimizzazione potrebbe non essere stata completata correttamente.';
        }
      },
      error: (listErr) => {
        console.error('Errore durante il caricamento della lista delle ottimizzazioni:', listErr);
        this.error = 'Errore caricamento risultato: ' + (listErr.error?.error || listErr.message || 'Errore sconosciuto');
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

  loadOptimizationList(): void {
    this.isLoading = true;
    this.sapiooService.getCompletedOptimizations().subscribe({
      next: (optimizations) => {
        this.optimizationList = optimizations;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Errore caricamento lista ottimizzazioni:', err);
        this.error = 'Errore durante il caricamento della lista: ' + err.message;
        this.isLoading = false;
      }
    });
  }

  toggleOptimizationSelection(optimization: SapiooResult): void {
    if (this.selectedOptimizationIds.has(optimization.jobId)) {
      this.selectedOptimizationIds.delete(optimization.jobId);
    } else {
      this.selectedOptimizationIds.add(optimization.jobId);
    }
  }

  isOptimizationSelected(optimization: SapiooResult): boolean {
    return this.selectedOptimizationIds.has(optimization.jobId);
  }

  selectAllOptimizations(): void {
    this.optimizationList.forEach(opt => this.selectedOptimizationIds.add(opt.jobId));
  }

  deselectAllOptimizations(): void {
    this.selectedOptimizationIds.clear();
  }

  deleteSelectedOptimizations(): void {
    if (this.selectedOptimizationIds.size === 0) {
      this.error = 'Seleziona almeno un\'ottimizzazione da eliminare';
      return;
    }

    if (!confirm(`Sei sicuro di voler eliminare ${this.selectedOptimizationIds.size} ottimizzazioni?`)) {
      return;
    }

    const deletePromises = Array.from(this.selectedOptimizationIds).map(jobId =>
      this.sapiooService.deleteOptimization(jobId).toPromise()
    );

    Promise.all(deletePromises).then(() => {
      this.selectedOptimizationIds.clear();
      this.loadOptimizationList();
      if (this.viewMode === 'detail' && this.result && this.selectedOptimizationIds.has(this.result.jobId)) {
        this.viewMode = 'list';
        this.result = null;
      }
    }).catch((err) => {
      console.error('Errore durante l\'eliminazione:', err);
      this.error = 'Errore durante l\'eliminazione delle ottimizzazioni';
      this.loadOptimizationList();
    });
  }

  deleteOptimization(optimization: SapiooResult): void {
    if (!confirm(`Sei sicuro di voler eliminare l'ottimizzazione "${optimization.backtestingName}"?`)) {
      return;
    }

    this.sapiooService.deleteOptimization(optimization.jobId).subscribe({
      next: () => {
        this.loadOptimizationList();
        if (this.viewMode === 'detail' && this.result?.jobId === optimization.jobId) {
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

  viewOptimizationDetail(optimization: SapiooResult): void {
    this.selectedOptimization = optimization;
    this.currentJobId = optimization.jobId;
    this.viewMode = 'detail';
    this.loadResult();
  }

  showNewOptimizationForm(): void {
    this.viewMode = 'form';
    this.result = null;
    this.selectedOptimization = null;
    this.currentJobId = null;
    this.error = null;
  }

  backToList(): void {
    this.viewMode = 'list';
    this.result = null;
    this.selectedOptimization = null;
    this.currentJobId = null;
    this.activeTab = 'config';
  }
}
