import { Component, OnInit, Input } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { 
  FilteredBacktestingResult, 
  StrategyAllocation, 
  FilteredWeeklyResult,
  StrategyWeeklyStatus,
  AdvancedOptimizationResult 
} from '../../models/optimization.models';
import { ChartConfiguration, ChartData } from 'chart.js';

@Component({
  selector: 'app-optimization-detail',
  templateUrl: './optimization-detail.component.html',
  styleUrls: ['./optimization-detail.component.css'],
  standalone: false
})
export class OptimizationDetailComponent implements OnInit {
  resultId: string | null = null;
  result: FilteredBacktestingResult | null = null;
  advancedResult: AdvancedOptimizationResult | null = null;
  isLoading = false;
  error: string | null = null;
  
  // Input per ricevere risultati direttamente
  @Input() inputResult: FilteredBacktestingResult | null = null;
  @Input() inputAdvancedResult: AdvancedOptimizationResult | null = null;

  // Tab corrente
  activeTab: 'overview' | 'weekly' | 'strategies' | 'allocations' = 'overview';
  
  // Settimane espanse
  expandedWeeks: Set<number> = new Set();

  // Equity/Drawdown Line Chart
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
      intersect: false
    },
    plugins: {
      legend: {
        display: true,
        position: 'top'
      },
      tooltip: {
        callbacks: {
          label: (context) => {
            const value = context.parsed.y ?? 0;
            if (context.dataset.label === 'Drawdown') {
              return `Drawdown: ${(value * 100).toFixed(2)}%`;
            }
            return `${context.dataset.label}: $${value.toFixed(2)}`;
          }
        }
      }
    },
    scales: {
      x: {
        display: true,
        title: { display: true, text: 'Settimana' }
      },
      y: {
        type: 'linear',
        display: true,
        position: 'left',
        title: { display: true, text: 'Equity ($)' }
      },
      y1: {
        type: 'linear',
        display: true,
        position: 'right',
        title: { display: true, text: 'Drawdown (%)' },
        grid: { drawOnChartArea: false },
        ticks: {
          callback: (value) => `${(Number(value) * 100).toFixed(0)}%`
        }
      }
    }
  };

  // Weekly Performance Bar Chart
  barChartType: 'bar' = 'bar';
  barChartData: ChartData<'bar'> = {
    labels: [],
    datasets: []
  };
  barChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          label: (context) => `Profit: $${(context.parsed.y ?? 0).toFixed(2)}`
        }
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        title: { display: true, text: 'Profit ($)' }
      }
    }
  };

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService
  ) {}

  ngOnInit(): void {
    // 1. Controlla Input diretto
    if (this.inputResult) {
      this.result = this.inputResult;
      this.prepareChartData();
      return;
    }
    
    if (this.inputAdvancedResult) {
      this.advancedResult = this.inputAdvancedResult;
      this.result = this.inputAdvancedResult.filteredBacktesting;
      this.prepareChartData();
      return;
    }

    // 2. Controlla stato dalla navigazione
    const navState = history.state;
    if (navState?.result) {
      this.result = navState.result as FilteredBacktestingResult;
      this.prepareChartData();
      return;
    }
    
    if (navState?.advancedResult) {
      this.advancedResult = navState.advancedResult as AdvancedOptimizationResult;
      this.result = this.advancedResult.filteredBacktesting;
      this.prepareChartData();
      return;
    }

    // 3. Carica da API tramite ID
    this.route.paramMap.subscribe(params => {
      const id = params.get('id') || params.get('setupId');
      if (id) {
        this.resultId = id;
        this.loadResult(id);
      } else {
        this.error = 'ID non specificato';
      }
    });
  }

  loadResult(id: string): void {
    this.isLoading = true;
    this.error = null;

    // Carica l'ottimizzazione salvata
    this.api.getSavedOptimization(id).subscribe({
      next: (result) => {
        this.result = result;
        this.prepareChartData();
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Errore caricamento:', err);
        this.error = 'Risultato non trovato';
        this.isLoading = false;
      }
    });
  }

  // Conversione temporanea per retrocompatibilità
  private convertSetupToFilteredResult(setup: any): FilteredBacktestingResult {
    return {
      originalBacktestingId: setup.id || '',
      setupName: setup.name || '',
      optimizationDate: setup.updatedAt || new Date().toISOString(),
      startDate: setup.evaluationPeriod?.startDate || '',
      endDate: setup.evaluationPeriod?.endDate || '',
      initialCapital: 100000,
      hourlyResults: [],
      weeklyResults: [],
      finalEquity: setup.metrics?.totalReturn ? 100000 * (1 + setup.metrics.totalReturn / 100) : 100000,
      totalProfit: setup.metrics?.totalReturn ? 100000 * setup.metrics.totalReturn / 100 : 0,
      maxDrawdown: setup.metrics?.maxDrawdown || 0,
      totalReturn: setup.metrics?.totalReturn || 0,
      totalTrades: setup.metrics?.totalTrades || 0,
      winRate: setup.metrics?.winRate || 0,
      enabledStrategiesForNextWeek: (setup.enabledStrategies || []).map((name: string, i: number) => ({
        strategyName: name,
        symbol: '',
        timeframeMinutes: 0,
        sizeMultiplier: 1.0,
        allocationPercent: 100 / (setup.enabledStrategies?.length || 1),
        score: setup.finalScore || 0,
        rank: i + 1,
        metrics: {
          winRate: setup.metrics?.winRate || 0,
          totalProfit: 0,
          maxDrawdown: setup.metrics?.maxDrawdown || 0,
          profitFactor: setup.metrics?.profitFactor || 0,
          totalTrades: 0
        }
      })),
      strategyStatuses: [],
      filterParameters: setup.riskParams || {},
      stats: {
        totalStrategiesInBacktesting: setup.enabledStrategies?.length || 0,
        averageActiveStrategiesPerWeek: setup.enabledStrategies?.length || 0,
        weeksAnalyzed: 0,
        lookbackWeeks: setup.evaluationPeriod?.weeks || 4,
        originalTotalProfit: 0,
        filteredTotalProfit: setup.metrics?.totalReturn ? 100000 * setup.metrics.totalReturn / 100 : 0,
        profitDifferencePercent: 0,
        originalMaxDrawdown: 0,
        filteredMaxDrawdown: setup.metrics?.maxDrawdown || 0
      }
    };
  }

  prepareChartData(): void {
    if (!this.result) return;

    // Prepara chart equity/drawdown dai risultati settimanali
    if (this.result.weeklyResults && this.result.weeklyResults.length > 0) {
      const labels = this.result.weeklyResults.map(w => `W${w.week}/${w.year}`);
      const equityData = this.result.weeklyResults.map(w => w.weeklyEquity);
      const drawdownData = this.result.weeklyResults.map(w => w.weeklyDrawdown);
      const profitData = this.result.weeklyResults.map(w => w.weeklyProfit);

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
            yAxisID: 'y'
          },
          {
            label: 'Drawdown',
            data: drawdownData,
            borderColor: '#dc3545',
            backgroundColor: 'transparent',
            borderDash: [5, 5],
            tension: 0.3,
            yAxisID: 'y1'
          }
        ]
      };

      this.barChartData = {
        labels: labels,
        datasets: [{
          data: profitData,
          backgroundColor: profitData.map(p => p >= 0 ? '#28a745' : '#dc3545'),
          borderRadius: 4
        }]
      };
    }
  }

  // Getters per la vista
  get hasChartData(): boolean {
    return this.lineChartData.datasets.length > 0 && 
           !!this.lineChartData.labels && 
           this.lineChartData.labels.length > 0;
  }

  get hasAllocations(): boolean {
    return !!(this.result?.enabledStrategiesForNextWeek && 
           this.result.enabledStrategiesForNextWeek.length > 0);
  }

  get hasWeeklyData(): boolean {
    return !!(this.result?.weeklyResults && this.result.weeklyResults.length > 0);
  }

  // Formattazione
  formatPercent(value: number): string {
    if (value === undefined || value === null) return '-';
    return (value * 100).toFixed(2) + '%';
  }

  formatCurrency(value: number): string {
    if (value === undefined || value === null) return '-';
    return '$' + value.toFixed(2);
  }

  formatDate(dateString: string): string {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('it-IT', { 
      year: 'numeric', 
      month: '2-digit', 
      day: '2-digit'
    });
  }

  formatMultiplier(value: number): string {
    if (value === undefined || value === null) return '1.00x';
    return value.toFixed(2) + 'x';
  }

  getMultiplierClass(multiplier: number): string {
    if (multiplier >= 1.5) return 'multiplier-high';
    if (multiplier >= 1.0) return 'multiplier-medium';
    return 'multiplier-low';
  }

  navigateToList(): void {
    this.router.navigate(['/optimization']);
  }

  toggleWeekExpand(index: number): void {
    if (this.expandedWeeks.has(index)) {
      this.expandedWeeks.delete(index);
    } else {
      this.expandedWeeks.add(index);
    }
  }

  getMultiplierForStrategy(week: FilteredWeeklyResult, strategyName: string): number {
    if (!week.allocationsForNextWeek) return 1;
    const alloc = week.allocationsForNextWeek.find(a => a.strategyName === strategyName);
    return alloc?.sizeMultiplier || 1;
  }

  expandAllWeeks(): void {
    if (this.result?.weeklyResults) {
      this.result.weeklyResults.forEach((_, i) => this.expandedWeeks.add(i));
    }
  }

  collapseAllWeeks(): void {
    this.expandedWeeks.clear();
  }
}
