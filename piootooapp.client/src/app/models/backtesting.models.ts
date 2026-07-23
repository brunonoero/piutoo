// Modelli per il Backtesting

export interface BacktestRequest {
  setupId?: string;                    // ID del setup salvato da usare
  setupName: string;                   // Nome del backtest
  symbol: string;                      // Simbolo da tradare
  startDate: string;                   // Data inizio backtest
  endDate: string;                     // Data fine backtest
  initialBalance: number;              // Balance iniziale
  commissionPerTrade: number;          // Commissione per trade
  useWeeklyRotation: boolean;          // Abilita rotazione settimanale
  optimizationLookbackWeeks: number;   // Settimane di lookback per ottimizzazione
}

export interface BacktestResponse {
  setupName: string;
  symbol: string;
  startDate: string;
  endDate: string;
  initialBalance: number;
  finalBalance: number;
  totalWeeks: number;
  status: BacktestStatus;
  statusMessage?: string;
  
  // Metriche aggregate
  summary: BacktestSummary;
  
  // Dati per il chart (serie temporali)
  equityCurve: EquityPoint[];
  
  // Dettaglio settimanale
  weeklyResults: WeeklyBacktestResult[];
  
  // Tutti i trade eseguiti
  trades: TradeRecord[];
}

export enum BacktestStatus {
  Pending = 'Pending',
  Running = 'Running',
  Completed = 'Completed',
  Failed = 'Failed'
}

export interface BacktestSummary {
  totalReturn: number;
  totalReturnPercent: number;
  annualizedReturn: number;
  sharpeRatio: number;
  sortinoRatio: number;
  calmarRatio: number;
  maxDrawdown: number;
  maxDrawdownPercent: number;
  maxDrawdownDate: string;
  recoveryTime?: number;              // Giorni per recuperare dal max DD
  winRate: number;
  profitFactor: number;
  totalTrades: number;
  winningTrades: number;
  losingTrades: number;
  averageWin: number;
  averageLoss: number;
  largestWin: number;
  largestLoss: number;
  maxConsecutiveWins: number;
  maxConsecutiveLosses: number;
  averageHoldingPeriod: number;       // In minuti/ore
  profitDrawdownRatio: number;
}

export interface EquityPoint {
  date: string;
  equity: number;
  profit: number;
  drawdown: number;
  drawdownPercent: number;
  cumulativeReturn: number;
}

export interface WeeklyBacktestResult {
  weekNumber: number;
  year: number;
  weekStart: string;
  weekEnd: string;
  
  // Strategie attive per questa settimana (dopo ottimizzazione)
  enabledStrategies: string[];
  disabledStrategies: string[];
  optimizationScore: number;
  
  // Performance della settimana
  startEquity: number;
  endEquity: number;
  weeklyProfit: number;
  weeklyProfitPercent: number;
  weeklyDrawdown: number;
  weeklyDrawdownPercent: number;
  peakEquity: number;
  
  // Statistiche trading
  tradesCount: number;
  winningTrades: number;
  losingTrades: number;
  winRate: number;
  profitFactor: number;
  
  // Note/eventi
  notes?: string;
}

export interface TradeRecord {
  id: number;
  date: string;
  week: number;
  year: number;
  strategyName: string;
  symbol: string;
  direction: TradeDirection;
  entryPrice: number;
  exitPrice: number;
  entryTime: string;
  exitTime: string;
  quantity: number;
  profit: number;
  profitPercent: number;
  commission: number;
  netProfit: number;
  holdingPeriod: number;              // In minuti
  isWin: boolean;
}

export enum TradeDirection {
  Long = 'Long',
  Short = 'Short'
}

// Per il chart
export interface ChartDataSeries {
  name: string;
  data: ChartPoint[];
  color?: string;
  type?: 'line' | 'area' | 'bar';
}

export interface ChartPoint {
  x: string | number;  // Data o indice
  y: number;
}

// Configurazione chart
export interface ChartConfig {
  showEquity: boolean;
  showProfit: boolean;
  showDrawdown: boolean;
  timeframe: 'daily' | 'weekly' | 'monthly';
}
