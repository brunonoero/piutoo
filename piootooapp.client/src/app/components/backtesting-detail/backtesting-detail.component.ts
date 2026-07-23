import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BacktestingService } from '../../services/backtesting.service';
import { BacktestingResult, StrategyInfo, HourlyResult } from '../../models/backtesting-new.models';
import { ChartConfiguration, ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

@Component({
  selector: 'app-backtesting-detail',
  templateUrl: './backtesting-detail.component.html',
  styleUrls: ['./backtesting-detail.component.css'],
  standalone: false
})
export class BacktestingDetailComponent implements OnInit {
  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;

  jobId: string | null = null;
  result: BacktestingResult | null = null;
  isLoading = false;
  error: string | null = null;

  // Chart configuration
  lineChartType: 'line' = 'line';
  lineChartData: ChartData<'line'> = {
    labels: [],
    datasets: []
  };
  lineChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      mode: 'index',
      intersect: false,
    },
    plugins: {
      legend: {
        display: true,
        position: 'top',
      },
      tooltip: {
        enabled: true,
        callbacks: {
          label: (context) => {
            const label = context.dataset.label || '';
            const value = context.parsed.y ?? 0;
            if (label.includes('Drawdown')) {
              return `${label}: ${(value * 100).toFixed(2)}%`;
            }
            return `${label}: $${value.toFixed(2)}`;
          }
        }
      }
    },
    scales: {
      x: {
        display: true,
        title: {
          display: true,
          text: 'Data'
        },
        ticks: {
          maxTicksLimit: 10,
          maxRotation: 45,
          minRotation: 0
        }
      },
      y: {
        type: 'linear',
        display: true,
        position: 'left',
        title: {
          display: true,
          text: 'Equity ($)'
        },
        grid: {
          drawOnChartArea: true
        }
      },
      y1: {
        type: 'linear',
        display: true,
        position: 'right',
        title: {
          display: true,
          text: 'Drawdown (%)'
        },
        grid: {
          drawOnChartArea: false
        },
        ticks: {
          callback: (value) => `${(Number(value) * 100).toFixed(0)}%`
        },
        max: 0,
        min: -0.5
      }
    }
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private backtestingService: BacktestingService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('jobId');
      if (id) {
        this.jobId = id;
        this.loadBacktestingDetail(id);
      } else {
        this.error = 'ID backtesting non specificato';
      }
    });
  }

  loadBacktestingDetail(jobId: string): void {
    this.isLoading = true;
    this.error = null;

    this.backtestingService.getResult(jobId).subscribe({
      next: (result) => {
        this.result = result;
        this.prepareChartData();
        this.isLoading = false;
      },
      error: (err) => {
        console.warn('Errore durante il caricamento del risultato:', err);
        
        this.backtestingService.getCompletedBacktestings().subscribe({
          next: (backtestings) => {
            let found = backtestings.find(b => 
              b.jobId && b.jobId.toLowerCase() === jobId.toLowerCase()
            );
            
            if (!found && backtestings.length > 0) {
              const sorted = backtestings.sort((a, b) => {
                const dateA = a.createdAt ? new Date(a.createdAt).getTime() : 0;
                const dateB = b.createdAt ? new Date(b.createdAt).getTime() : 0;
                return dateB - dateA;
              });
              const mostRecent = sorted[0];
              const createdAt = mostRecent.createdAt ? new Date(mostRecent.createdAt) : null;
              const fiveMinutesAgo = new Date(Date.now() - 5 * 60 * 1000);
              if (createdAt && createdAt > fiveMinutesAgo) {
                found = mostRecent;
              }
            }
            
            if (found) {
              this.result = found;
              this.prepareChartData();
            } else {
              this.error = 'Backtesting non trovato';
            }
            this.isLoading = false;
          },
          error: () => {
            this.error = 'Errore durante il caricamento del backtesting';
            this.isLoading = false;
          }
        });
      }
    });
  }

  prepareChartData(): void {
    if (!this.result) return;

    // Usa hourlyResults se disponibili, altrimenti weeklyResults
    if (this.result.hourlyResults && this.result.hourlyResults.length > 0) {
      this.prepareFromHourlyData(this.result.hourlyResults);
    } else if (this.result.weeklyResults && this.result.weeklyResults.length > 0) {
      this.prepareFromWeeklyData();
    }
  }

  prepareFromHourlyData(hourlyResults: HourlyResult[]): void {
    // Campiona i dati se troppi (max 200 punti)
    const maxPoints = 200;
    let data = hourlyResults;
    if (hourlyResults.length > maxPoints) {
      const step = Math.ceil(hourlyResults.length / maxPoints);
      data = hourlyResults.filter((_, i) => i % step === 0);
    }

    const labels = data.map(h => {
      const date = new Date(h.dateTime);
      return date.toLocaleDateString('it-IT', { month: 'short', day: 'numeric' });
    });

    const equityData = data.map(h => h.equity);
    const drawdownData = data.map(h => h.drawdown);

    this.lineChartData = {
      labels: labels,
      datasets: [
        {
          label: 'Equity',
          data: equityData,
          borderColor: '#28a745',
          backgroundColor: 'rgba(40, 167, 69, 0.1)',
          fill: true,
          tension: 0.3,
          yAxisID: 'y',
          pointRadius: 0,
          borderWidth: 2
        },
        {
          label: 'Drawdown',
          data: drawdownData,
          borderColor: '#dc3545',
          backgroundColor: 'rgba(220, 53, 69, 0.1)',
          fill: true,
          tension: 0.3,
          yAxisID: 'y1',
          pointRadius: 0,
          borderWidth: 2
        }
      ]
    };

    // Aggiorna l'asse y1 in base ai dati
    const minDrawdown = Math.min(...drawdownData);
    if (this.lineChartOptions?.scales?.['y1']) {
      (this.lineChartOptions.scales['y1'] as any).min = Math.min(minDrawdown * 1.2, -0.1);
    }
  }

  prepareFromWeeklyData(): void {
    if (!this.result?.weeklyResults) return;

    const labels = this.result.weeklyResults.map(w => `W${w.week}/${w.year}`);
    const equityData = this.result.weeklyResults.map(w => w.weeklyEquity);
    const drawdownData = this.result.weeklyResults.map(w => w.weeklyDrawdown);

    this.lineChartData = {
      labels: labels,
      datasets: [
        {
          label: 'Equity',
          data: equityData,
          borderColor: '#28a745',
          backgroundColor: 'rgba(40, 167, 69, 0.1)',
          fill: true,
          tension: 0.3,
          yAxisID: 'y',
          pointRadius: 2,
          borderWidth: 2
        },
        {
          label: 'Drawdown',
          data: drawdownData,
          borderColor: '#dc3545',
          backgroundColor: 'rgba(220, 53, 69, 0.1)',
          fill: true,
          tension: 0.3,
          yAxisID: 'y1',
          pointRadius: 2,
          borderWidth: 2
        }
      ]
    };
  }

  navigateToList(): void {
    this.router.navigate(['/backtesting']);
  }

  deleteBacktesting(): void {
    if (!this.result) return;

    if (!confirm(`Sei sicuro di voler eliminare il backtesting "${this.result.setupName}"?`)) {
      return;
    }

    this.backtestingService.deleteBacktesting(this.result.jobId).subscribe({
      next: () => {
        this.router.navigate(['/backtesting']);
      },
      error: (err) => {
        console.error('Errore durante l\'eliminazione:', err);
        this.error = 'Errore durante l\'eliminazione: ' + err.message;
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
    
    const strategies = [...new Set(result.strategiesUsed || [])];
    const validSymbols = ['FDAX', 'FNQ', 'FCL', 'FGC', 'DAX', 'NQ', 'CL', 'GC', 'ES', 'YM', 'ZB', 'ZN', 'ZS', 'ZC', 'ZW', 'HE', 'LE', 'KC', 'CT', 'SB', 'HO', 'RB', 'NG'];
    
    return strategies.map(name => {
      const fullMatch = name.match(/^(.+)_([A-Z]{2,5})_(\d+)$/);
      if (fullMatch) {
        const strategyPart = fullMatch[1];
        const symbolPart = fullMatch[2];
        const timeframePart = parseInt(fullMatch[3], 10);
        
        if (validSymbols.includes(symbolPart) && timeframePart >= 1 && timeframePart <= 10080) {
          return {
            name: strategyPart,
            symbol: symbolPart,
            timeframe: timeframePart
          };
        }
      }
      
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

  get hasChartData(): boolean {
    return this.lineChartData.datasets.length > 0 && 
           !!this.lineChartData.labels && 
           this.lineChartData.labels.length > 0;
  }
}
