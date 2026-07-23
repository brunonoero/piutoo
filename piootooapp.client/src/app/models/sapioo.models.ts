// Modelli per l'ottimizzazione Sapioo

export interface SapiooRequest {
  backtestingName: string;
  riskParams: RiskManagementParams;
  name: string;
  evaluationPeriodWeeks: number;
}

export interface RiskManagementParams {
  // Filtri eliminatori
  maxDrawdownPercent: number;
  minWinRate: number;
  minProfitFactor: number;
  maxConsecutiveLosses: number;
  
  // Filtri di qualità
  minSharpeRatio: number;
  minRecoveryFactor: number;
  maxVolatility: number;
  
  // Parametri temporali
  weeksLookback: number;
}

export interface SapiooJob {
  jobId: string;
  status: SapiooJobStatus;
  progressPercent: number;
  startedAt: string;
  completedAt?: string;
  result?: SapiooResult;
  errorMessage?: string;
}

export enum SapiooJobStatus {
  Pending = 'Pending',
  Running = 'Running',
  Completed = 'Completed',
  Failed = 'Failed'
}

export interface SapiooResult {
  jobId: string;
  backtestingName: string;
  parameters: RiskManagementParams;
  weeklyResults: WeeklyOptimizationResult[];
  finalResult: TradingSnapshot;
  filteredEquityCurve: EquityPoint[];
  resultFilePath?: string;
}

export interface WeeklyOptimizationResult {
  year: number;
  week: number;
  weekStart: string;
  weekEnd: string;
  enabledStrategies: StrategyWeight[];
  weeklyProfit: number;
  weeklyDrawdown: number;
  weeklyEquity: number;
}

export interface StrategyWeight {
  strategyName: string;
  multiplier: number;
  isEnabled: boolean;
  disabledReason?: string;
  winRate: number;
  profitFactor: number;
  sharpeRatio: number;
  maxDrawdown: number;
}

export interface TradingSnapshot {
  dateTime: string;
  equity: number;
  balance: number;
  drawdown: number;
  profit: number;
}

export interface EquityPoint {
  date: string;
  balance: number;
  trade?: any; // Opzionale
}

export interface SapiooPreset {
  name: string;
  description: string;
  riskParams: RiskManagementParams;
  evaluationPeriodWeeks?: number;
}
