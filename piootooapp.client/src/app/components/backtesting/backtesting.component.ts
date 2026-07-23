import { Component, OnInit, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { ApiService } from '../../services/api.service';
import { SavedSetup, SymbolInfo, SymbolSelection } from '../../models/optimization.models';
import {
  BacktestRequest,
  BacktestResponse,
  WeeklyBacktestResult,
  EquityPoint,
  ChartConfig
} from '../../models/backtesting.models';

@Component({
  selector: 'app-backtesting',
  templateUrl: './backtesting.component.html',
  styleUrls: ['./backtesting.component.css'],
  standalone: false
})
export class BacktestingComponent implements OnInit {
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  // Form
  selectedSetupId = '';
  setupName = 'Backtest';
  selectedSymbol = '@ES';
  startDate = '';
  endDate = '';
  initialBalance = 10000;
  commissionPerTrade = 2;
  useWeeklyRotation = true;
  optimizationLookbackWeeks = 4;

  // Data
  symbols: SymbolInfo[] = [];
  savedSetups: SavedSetup[] = [];

  // State
  isLoading = false;
  isRunning = false;
  result: BacktestResponse | null = null;
  error: string | null = null;
  activeTab: 'config' | 'chart' | 'table' | 'trades' = 'config';

  // Chart config
  chartConfig: ChartConfig = {
    showEquity: true,
    showProfit: true,
    showDrawdown: true,
    timeframe: 'weekly'
  };

  // Chart dimensions
  chartWidth = 800;
  chartHeight = 400;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadSymbols();
    this.loadSavedSetups();
    this.setDefaultDates();
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

  setDefaultDates(): void {
    const end = new Date();
    const start = new Date();
    start.setFullYear(start.getFullYear() - 2);
    
    this.startDate = this.formatDateInputValue(start);
    this.endDate = this.formatDateInputValue(end);
  }

  private formatDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  onSetupSelect(): void {
    const setup = this.savedSetups.find(s => s.id === this.selectedSetupId);
    if (setup) {
      this.setupName = `Backtest - ${setup.name}`;
      if (setup.symbols?.length > 0) {
        this.selectedSymbol = setup.symbols[0].symbol;
      }
    }
  }

  runBacktest(): void {
    if (!this.selectedSetupId) {
      this.error = 'Seleziona un setup di ottimizzazione';
      return;
    }

    this.isRunning = true;
    this.error = null;
    this.result = null;

    const request: BacktestRequest = {
      setupId: this.selectedSetupId,
      setupName: this.setupName,
      symbol: this.selectedSymbol,
      startDate: this.startDate,
      endDate: this.endDate,
      initialBalance: this.initialBalance,
      commissionPerTrade: this.commissionPerTrade,
      useWeeklyRotation: this.useWeeklyRotation,
      optimizationLookbackWeeks: this.optimizationLookbackWeeks
    };

    this.api.runBacktest(request).subscribe({
      next: (response: BacktestResponse) => {
        this.result = response;
        this.isRunning = false;
        this.activeTab = 'chart';
        setTimeout(() => this.drawChart(), 100);
      },
      error: (err) => {
        this.error = err.error?.statusMessage || err.message || 'Errore durante il backtest';
        this.isRunning = false;
      }
    });
  }

  // ============ CHART DRAWING ============

  drawChart(): void {
    if (!this.result?.equityCurve?.length) return;

    const canvas = this.chartCanvas?.nativeElement;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Clear canvas
    ctx.clearRect(0, 0, this.chartWidth, this.chartHeight);

    const data = this.result.equityCurve;
    const padding = { top: 40, right: 80, bottom: 60, left: 80 };
    const chartArea = {
      x: padding.left,
      y: padding.top,
      width: this.chartWidth - padding.left - padding.right,
      height: this.chartHeight - padding.top - padding.bottom
    };

    // Calculate scales
    const equityValues = data.map(d => d.equity);
    const profitValues = data.map(d => d.profit);
    const ddValues = data.map(d => d.drawdownPercent);

    const minEquity = Math.min(...equityValues) * 0.98;
    const maxEquity = Math.max(...equityValues) * 1.02;
    const minDD = Math.min(...ddValues);

    // Draw background
    ctx.fillStyle = '#1a1a2e';
    ctx.fillRect(0, 0, this.chartWidth, this.chartHeight);

    // Draw grid
    this.drawGrid(ctx, chartArea, minEquity, maxEquity);

    // Draw data series
    if (this.chartConfig.showDrawdown) {
      this.drawAreaSeries(ctx, chartArea, data, d => d.drawdownPercent, minDD, 0, 'rgba(244, 67, 54, 0.3)', 'rgba(244, 67, 54, 0.8)');
    }

    if (this.chartConfig.showEquity) {
      this.drawLineSeries(ctx, chartArea, data, d => d.equity, minEquity, maxEquity, '#4CAF50', 2);
    }

    if (this.chartConfig.showProfit) {
      this.drawLineSeries(ctx, chartArea, data, d => d.cumulativeReturn, 
        Math.min(...data.map(d => d.cumulativeReturn)), 
        Math.max(...data.map(d => d.cumulativeReturn)), 
        '#2196F3', 1.5);
    }

    // Draw labels
    this.drawLabels(ctx, chartArea, data, minEquity, maxEquity);

    // Draw legend
    this.drawLegend(ctx);
  }

  private drawGrid(ctx: CanvasRenderingContext2D, area: any, minY: number, maxY: number): void {
    ctx.strokeStyle = 'rgba(255,255,255,0.1)';
    ctx.lineWidth = 1;

    // Horizontal lines
    for (let i = 0; i <= 5; i++) {
      const y = area.y + (area.height / 5) * i;
      ctx.beginPath();
      ctx.moveTo(area.x, y);
      ctx.lineTo(area.x + area.width, y);
      ctx.stroke();
    }

    // Vertical lines
    for (let i = 0; i <= 10; i++) {
      const x = area.x + (area.width / 10) * i;
      ctx.beginPath();
      ctx.moveTo(x, area.y);
      ctx.lineTo(x, area.y + area.height);
      ctx.stroke();
    }
  }

  private drawLineSeries(
    ctx: CanvasRenderingContext2D, 
    area: any, 
    data: EquityPoint[], 
    getValue: (d: EquityPoint) => number,
    minY: number, 
    maxY: number, 
    color: string, 
    lineWidth: number
  ): void {
    ctx.strokeStyle = color;
    ctx.lineWidth = lineWidth;
    ctx.beginPath();

    data.forEach((point, i) => {
      const x = area.x + (i / (data.length - 1)) * area.width;
      const y = area.y + area.height - ((getValue(point) - minY) / (maxY - minY)) * area.height;

      if (i === 0) {
        ctx.moveTo(x, y);
      } else {
        ctx.lineTo(x, y);
      }
    });

    ctx.stroke();
  }

  private drawAreaSeries(
    ctx: CanvasRenderingContext2D,
    area: any,
    data: EquityPoint[],
    getValue: (d: EquityPoint) => number,
    minY: number,
    maxY: number,
    fillColor: string,
    strokeColor: string
  ): void {
    ctx.fillStyle = fillColor;
    ctx.strokeStyle = strokeColor;
    ctx.lineWidth = 1;
    ctx.beginPath();

    const zeroY = area.y + area.height - ((0 - minY) / (maxY - minY)) * area.height;

    ctx.moveTo(area.x, zeroY);

    data.forEach((point, i) => {
      const x = area.x + (i / (data.length - 1)) * area.width;
      const y = area.y + area.height - ((getValue(point) - minY) / (maxY - minY)) * area.height;
      ctx.lineTo(x, y);
    });

    ctx.lineTo(area.x + area.width, zeroY);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
  }

  private drawLabels(ctx: CanvasRenderingContext2D, area: any, data: EquityPoint[], minY: number, maxY: number): void {
    ctx.fillStyle = '#fff';
    ctx.font = '11px Arial';
    ctx.textAlign = 'right';

    // Y-axis labels (equity)
    for (let i = 0; i <= 5; i++) {
      const value = minY + ((maxY - minY) / 5) * (5 - i);
      const y = area.y + (area.height / 5) * i;
      ctx.fillText(this.formatCurrency(value), area.x - 10, y + 4);
    }

    // X-axis labels (dates)
    ctx.textAlign = 'center';
    const step = Math.ceil(data.length / 10);
    data.forEach((point, i) => {
      if (i % step === 0) {
        const x = area.x + (i / (data.length - 1)) * area.width;
        const date = new Date(point.date);
        ctx.fillText(`${date.getDate()}/${date.getMonth() + 1}`, x, area.y + area.height + 20);
      }
    });

    // Title
    ctx.font = 'bold 14px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('Equity Curve & Drawdown', this.chartWidth / 2, 25);
  }

  private drawLegend(ctx: CanvasRenderingContext2D): void {
    const legendX = this.chartWidth - 150;
    const legendY = 50;

    ctx.font = '11px Arial';
    ctx.textAlign = 'left';

    if (this.chartConfig.showEquity) {
      ctx.fillStyle = '#4CAF50';
      ctx.fillRect(legendX, legendY, 15, 3);
      ctx.fillStyle = '#fff';
      ctx.fillText('Equity', legendX + 20, legendY + 4);
    }

    if (this.chartConfig.showProfit) {
      ctx.fillStyle = '#2196F3';
      ctx.fillRect(legendX, legendY + 20, 15, 3);
      ctx.fillStyle = '#fff';
      ctx.fillText('Cum. Return', legendX + 20, legendY + 24);
    }

    if (this.chartConfig.showDrawdown) {
      ctx.fillStyle = 'rgba(244, 67, 54, 0.5)';
      ctx.fillRect(legendX, legendY + 40, 15, 10);
      ctx.fillStyle = '#fff';
      ctx.fillText('Drawdown', legendX + 20, legendY + 48);
    }
  }

  // ============ FORMATTING ============

  formatPercent(value: number): string {
    if (value === null || value === undefined) return '-';
    return (value * 100).toFixed(2) + '%';
  }

  formatNumber(value: number, decimals: number = 2): string {
    if (value === null || value === undefined) return '-';
    return value.toFixed(decimals);
  }

  formatCurrency(value: number): string {
    if (value === null || value === undefined) return '-';
    return '$' + value.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('it-IT', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  getWeekLabel(week: WeeklyBacktestResult): string {
    return `W${week.weekNumber}/${week.year}`;
  }

  getProfitClass(value: number): string {
    if (value > 0) return 'positive';
    if (value < 0) return 'negative';
    return '';
  }

  toggleChartSeries(series: 'equity' | 'profit' | 'drawdown'): void {
    switch (series) {
      case 'equity':
        this.chartConfig.showEquity = !this.chartConfig.showEquity;
        break;
      case 'profit':
        this.chartConfig.showProfit = !this.chartConfig.showProfit;
        break;
      case 'drawdown':
        this.chartConfig.showDrawdown = !this.chartConfig.showDrawdown;
        break;
    }
    this.drawChart();
  }
}
