// Modelli per il nuovo sistema di Backtesting

export interface BacktestingRequest {
  selectedSymbols: string[];
  selectedStrategyIds?: string[];
  startDate: string; // ISO date string
  endDate: string; // ISO date string
  initialCapital: number;
  commissionPerContract: number;
  name: string;
}

export interface BacktestingJob {
  jobId: string;
  status: BacktestingJobStatus;
  progressPercent: number;
  startedAt: string;
  completedAt?: string;
  result?: BacktestingResult;
  errorMessage?: string;
}

export enum BacktestingJobStatus {
  Pending = 'Pending',
  Running = 'Running',
  Completed = 'Completed',
  Failed = 'Failed'
}

export interface BacktestingResult {
  jobId: string;
  setupName: string;
  setupId: string;
  startDate: string;
  endDate: string;
  initialCapital: number;
  createdAt?: string; // Data di creazione del risultato del backtesting
  hourlyResults: HourlyResult[];
  strategyResults: StrategyHourlyResult[];
  weeklyResults: WeeklyResult[];
  finalEquity: number;
  totalProfit: number;
  maxDrawdown: number;
  totalReturn: number;
  totalTrades: number;
  winRate: number;
  strategiesUsed: string[];
  strategiesInfo?: StrategyInfo[];
  resultFilePath?: string;
}

export interface StrategyInfo {
  name: string;
  symbol: string;
  timeframeMinutes: number;
}

export interface HourlyResult {
  dateTime: string;
  equity: number;
  balance: number;
  drawdown: number;
  profit: number;
  openPositionsCount: number;
}

export interface StrategyHourlyResult {
  strategyName: string;
  dateTime: string;
  profit: number;
  contracts: number;
  signal?: SignalType;
  entryPrice?: number;
  exitPrice?: number;
}

export enum SignalType {
  Buy = 'Buy',
  Sell = 'Sell',
  Hold = 'Hold'
}

export interface WeeklyResult {
  year: number;
  week: number;
  weekStart: string;
  weekEnd: string;
  weeklyProfit: number;
  weeklyEquity: number;
  weeklyDrawdown: number;
  winRate: number;
  totalTrades: number;
  winningTrades: number;
}
