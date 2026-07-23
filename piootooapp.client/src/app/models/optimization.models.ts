// Modelli per l'ottimizzazione

export interface SymbolSelection {
  symbol: string;
  barType: string;
  key?: string; // computed: symbol|barType
}

export interface OptimizationRequest {
  setupName: string;
  description?: string;
  backtestingId?: string;
  symbols: SymbolSelection[];
  evaluationPeriod: EvaluationPeriod;
  optimizationParams: OptimizationParameters;
  riskParams: RiskParameters;
  algorithmSettings: AlgorithmSettings;
}

export interface EvaluationPeriod {
  type: PeriodType;
  weeks: number;
  months: number;
  startDate?: string;
  endDate?: string;
}

export enum PeriodType {
  Weeks = 'Weeks',
  Months = 'Months',
  DateRange = 'DateRange'
}

export interface OptimizationParameters {
  primaryObjective: OptimizationObjective;
  returnWeight: number;
  sharpeWeight: number;
  profitDrawdownRatioWeight: number;
  winRateWeight: number;
  profitFactorWeight: number;
  consistencyWeight: number;
  targetReturn?: number;
  targetSharpe?: number;
  targetProfitDdRatio?: number;
}

export enum OptimizationObjective {
  MaxReturn = 'MaxReturn',
  SharpeRatio = 'SharpeRatio',
  ProfitDrawdownRatio = 'ProfitDrawdownRatio',
  MinDrawdown = 'MinDrawdown',
  CalmarRatio = 'CalmarRatio',
  WeightedMultiObjective = 'WeightedMultiObjective'
}

export interface RiskParameters {
  maxDrawdown: number;
  maxDrawdownValue?: number;
  maxConsecutiveLosses: number;
  minWinRate: number;
  minSharpeRatio: number;
  minProfitFactor: number;
  minTrades: number;
  maxVolatility?: number;
  stopLossPercent: number;
  requirePositiveBalance: boolean;
  minProfitDrawdownRatio?: number;
}

export interface AlgorithmSettings {
  iterations: number;
  populationSize: number;
  randomSeed?: number;
  useWalkForward: boolean;
  inSamplePercent: number;
  saveIntermediateResults: boolean;
  parallelize: boolean;
  maxThreads?: number;
}

export interface OptimizationResponse {
  setupName: string;
  executionTime: string;
  duration: string;
  status: OptimizationStatus;
  statusMessage?: string;
  optimalConfig?: OptimalConfiguration;
  metrics?: PerformanceMetrics;
  topConfigurations: RankedConfiguration[];
  enabledStrategies: string[];
  strategyEvaluations: StrategyEvaluationResult[];
  requestParameters?: OptimizationRequest;
  stats?: OptimizationStats;
  strategiesFound?: StrategyInfo[];
}

export enum OptimizationStatus {
  Pending = 'Pending',
  Running = 'Running',
  Completed = 'Completed',
  Failed = 'Failed',
  PartialSuccess = 'PartialSuccess'
}

export interface OptimalConfiguration {
  finalScore: number;
  weights: OptimizedWeights;
  riskParams: RiskParameters;
  scoringConfig: any;
}

export interface OptimizedWeights {
  returnWeight: number;
  sharpeWeight: number;
  drawdownWeight: number;
  winRateWeight: number;
  profitFactorWeight: number;
  consistencyWeight: number;
  calmarWeight: number;
}

export interface PerformanceMetrics {
  totalReturn: number;
  annualizedReturn: number;
  sharpeRatio: number;
  sortinoRatio: number;
  calmarRatio: number;
  maxDrawdown: number;
  maxDrawdownValue: number;
  profitDrawdownRatio: number;
  winRate: number;
  profitFactor: number;
  totalTrades: number;
  winningTrades: number;
  losingTrades: number;
  averageWin: number;
  averageLoss: number;
  maxConsecutiveLosses: number;
  volatility: number;
}

export interface RankedConfiguration {
  rank: number;
  score: number;
  configuration: OptimalConfiguration;
  metrics: PerformanceMetrics;
  notes?: string;
}

export interface StrategyEvaluationResult {
  strategyName: string;
  finalScore: number;
  isEnabled: boolean;
  rank: number;
  componentScores: { [key: string]: number };
  avgReturn: number;
  avgSharpeRatio: number;
  avgDrawdown: number;
  avgWinRate: number;
  avgProfitFactor: number;
  totalTrades: number;
  qualificationReasons: string[];
  disqualificationReasons: string[];
  summary: string;
}

export interface OptimizationStats {
  iterationsRun: number;
  configurationsEvaluated: number;
  validConfigurations: number;
  improvementOverBaseline: number;
  baselineScore: number;
  finalScore: number;
  convergenceReached: boolean;
  convergenceIteration?: number;
}

export interface OptimizationPreset {
  name: string;
  description: string;
  optimizationParams: OptimizationParameters;
  riskParams: RiskParameters;
}

export interface SavedSetup {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  status: SetupStatus;
  symbols: SymbolSelection[];
  optimizationParams: OptimizationParameters;
  riskParams: RiskParameters;
  evaluationPeriod: EvaluationPeriod;
  optimalConfig?: OptimalConfiguration;
  metrics?: PerformanceMetrics;
  enabledStrategies: string[];
  finalScore: number;
  notes?: string;
  tags: string[];
  isActive: boolean;
}

export enum SetupStatus {
  Draft = 'Draft',
  Optimizing = 'Optimizing',
  Optimized = 'Optimized',
  Active = 'Active',
  Paused = 'Paused',
  Archived = 'Archived'
}

export interface SymbolInfo {
  symbol: string;
  barType: string;
  firstDate: string;
  lastDate: string;
  totalDays: number;
}

// Strategy Models
export interface StrategyDefinition {
  id: string;
  name: string;
  fileName: string;
  symbol: string;
  timeframeMinutes: number;
  barType: string;
  description: string;
  type: StrategyType;
  isActive: boolean;
  parameters: { [key: string]: any };
  lastModified: string;
  filePath: string;
  key: string;
}

export enum StrategyType {
  Unknown = 'Unknown',
  TrendFollowing = 'TrendFollowing',
  CounterTrend = 'CounterTrend',
  Breakout = 'Breakout',
  MeanReversion = 'MeanReversion',
  Momentum = 'Momentum',
  Scalping = 'Scalping',
  Swing = 'Swing',
  Portfolio = 'Portfolio'
}

export interface SymbolStrategiesInfo {
  symbol: string;
  totalStrategies: number;
  activeStrategies: number;
  availableTimeframes: number[];
  strategies: StrategyDefinition[];
}

export interface StrategyInfo {
  id: string;
  name: string;
  symbol: string;
  timeframeMinutes: number;
  barType: string;
  type: string;
  hasData: boolean;
}

// ============= Filtered Backtesting Result =============

/**
 * Risultato del backtesting filtrato dall'ottimizzazione
 */
export interface FilteredBacktestingResult {
  originalBacktestingId: string;
  setupName: string;
  optimizationDate: string;
  startDate: string;
  endDate: string;
  initialCapital: number;
  
  // Risultati filtrati
  hourlyResults: FilteredHourlyResult[];
  weeklyResults: FilteredWeeklyResult[];
  
  // Metriche globali filtrate
  finalEquity: number;
  totalProfit: number;
  maxDrawdown: number;
  totalReturn: number;
  totalTrades: number;
  winRate: number;
  
  // Strategie per la prossima settimana con allocazioni
  enabledStrategiesForNextWeek: StrategyAllocation[];
  
  // Status per strategia
  strategyStatuses: StrategyWeeklyStatus[];
  
  // Parametri usati
  filterParameters: RiskParameters;
  
  // Statistiche
  stats: FilteredOptimizationStats;
}

export interface FilteredHourlyResult {
  dateTime: string;
  equity: number;
  profit: number;
  drawdown: number;
}

export interface FilteredWeeklyResult {
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
  activeStrategies: string[];
  allocationsForNextWeek: StrategyAllocation[];
  disabledStrategies: StrategyDisqualification[];
}

export interface StrategyAllocation {
  strategyName: string;
  symbol: string;
  timeframeMinutes: number;
  sizeMultiplier: number;
  allocationPercent: number;
  score: number;
  rank: number;
  metrics: StrategyMetricsSummary;
}

export interface StrategyMetricsSummary {
  winRate: number;
  totalProfit: number;
  maxDrawdown: number;
  profitFactor: number;
  totalTrades: number;
}

export interface StrategyWeeklyStatus {
  strategyName: string;
  symbol: string;
  timeframeMinutes: number;
  activeWeeks: number[];
  disabledWeeks: number[];
  totalProfitWhenActive: number;
  totalProfitIfAlwaysActive: number;
  averageScore: number;
  isEnabledForNextWeek: boolean;
  sizeMultiplier: number;
}

export interface StrategyDisqualification {
  strategyName: string;
  reasons: string[];
  score: number;
}

export interface FilteredOptimizationStats {
  totalStrategiesInBacktesting: number;
  averageActiveStrategiesPerWeek: number;
  weeksAnalyzed: number;
  lookbackWeeks: number;
  originalTotalProfit: number;
  filteredTotalProfit: number;
  profitDifferencePercent: number;
  originalMaxDrawdown: number;
  filteredMaxDrawdown: number;
}

// ============= Advanced Optimization =============

/** Richiesta per ottimizzazione avanzata */
export interface AdvancedOptimizationRequest {
  backtestingId: string;
  lookbackWeeks: number;
  filterConfig?: AdvancedFilterConfigDto;
}

/** Configurazione filtri avanzati */
export interface AdvancedFilterConfigDto {
  minWinRate?: number;
  maxDrawdownLimit?: number;
  minSharpeRatio?: number;
  minTrades?: number;
  minCompositeScore?: number;
  minWeeksRequired?: number;
  maxCorrelation?: number;
  
  // Pesi score composito
  sharpeWeight?: number;
  sortinoWeight?: number;
  calmarWeight?: number;
  omegaWeight?: number;
  recoveryWeight?: number;
  winRateWeight?: number;
  tailRatioWeight?: number;
  gainToPainWeight?: number;
  ulcerPenalty?: number;
  drawdownPenalty?: number;
  stabilityBonus?: number;
  
  // Pesi ottimizzazione portafoglio
  riskParityWeight?: number;
  kellyWeight?: number;
  hrpWeight?: number;
}

/** Risultato dell'ottimizzazione avanzata */
export interface AdvancedOptimizationResult {
  backtestingId: string;
  setupName: string;
  optimizationDate: string;
  duration: string;
  
  originalStrategiesCount: number;
  filteredStrategiesCount: number;
  filteredStrategies: FilteredStrategyDto[];
  
  correlation: CorrelationInfoDto;
  portfolioMetrics: PortfolioMetricsDto;
  
  filteredBacktesting: FilteredBacktestingResult;
  filterConfigUsed?: any;
}

/** Strategia filtrata con metriche avanzate */
export interface FilteredStrategyDto {
  strategyName: string;
  symbol: string;
  timeframeMinutes: number;
  weight: number;
  sizeMultiplier: number;
  rank: number;
  metrics: StrategyAdvancedMetricsDto;
}

/** Metriche avanzate per strategia */
export interface StrategyAdvancedMetricsDto {
  totalReturn: number;
  winRate: number;
  totalTrades: number;
  avgWin: number;
  avgLoss: number;
  sharpeRatio: number;
  sortinoRatio: number;
  calmarRatio: number;
  omegaRatio: number;
  maxDrawdown: number;
  recoveryFactor: number;
  ulcerIndex: number;
  tailRatio: number;
  vaR95: number;
  cVaR95: number;
  gainToPainRatio: number;
  compositeScore: number;
}

/** Info correlazione */
export interface CorrelationInfoDto {
  averageCorrelation: number;
  strategyNames: string[];
  matrix: number[][];
}

/** Metriche portafoglio */
export interface PortfolioMetricsDto {
  expectedReturn: number;
  volatility: number;
  sharpeRatio: number;
  maxDrawdown: number;
  diversificationRatio: number;
}

// ============= Optimization Job (Async) =============

/** Stato di un job di ottimizzazione */
export enum OptimizationJobStatus {
  Pending = 'Pending',
  Running = 'Running',
  Completed = 'Completed',
  Failed = 'Failed'
}

/** Tipo di ottimizzazione */
export enum OptimizationType {
  Basic = 'Basic',
  Advanced = 'Advanced'
}

/** Job di ottimizzazione in esecuzione */
export interface OptimizationJob {
  jobId: string;
  status: OptimizationJobStatus;
  progressPercent: number;
  currentStep?: string;
  startedAt: string;
  completedAt?: string;
  type: OptimizationType;
  basicResult?: FilteredBacktestingResult;
  advancedResult?: AdvancedOptimizationResult;
  errorMessage?: string;
}
