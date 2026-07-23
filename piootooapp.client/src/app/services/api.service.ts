import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  OptimizationRequest,
  OptimizationResponse,
  OptimizationPreset,
  SavedSetup,
  SymbolInfo,
  StrategyDefinition,
  SymbolStrategiesInfo,
  FilteredBacktestingResult,
  AdvancedOptimizationRequest,
  AdvancedOptimizationResult
} from '../models/optimization.models';
import {
  BacktestRequest,
  BacktestResponse
} from '../models/backtesting.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = '/api';

  constructor(private http: HttpClient) {}

  // ============ OPTIMIZATION ============

  /** Ottimizzazione base - filtra backtesting con parametri di rischio */
  optimizeBasic(request: OptimizationRequest): Observable<FilteredBacktestingResult> {
    return this.http.post<FilteredBacktestingResult>(`${this.baseUrl}/PiootooOptimization/optimize`, request);
  }

  /** Ottimizzazione AVANZATA - algoritmi sofisticati (correlazione, Risk Parity, Kelly, HRP) */
  optimizeAdvanced(request: AdvancedOptimizationRequest): Observable<AdvancedOptimizationResult> {
    return this.http.post<AdvancedOptimizationResult>(`${this.baseUrl}/PiootooOptimization/optimize-advanced`, request);
  }

  // Legacy
  optimize(request: OptimizationRequest): Observable<FilteredBacktestingResult> {
    return this.optimizeBasic(request);
  }

  evaluate(request: OptimizationRequest): Observable<OptimizationResponse> {
    return this.http.post<OptimizationResponse>(`${this.baseUrl}/PiootooOptimization/evaluate`, request);
  }

  getPresets(): Observable<OptimizationPreset[]> {
    return this.http.get<OptimizationPreset[]>(`${this.baseUrl}/PiootooOptimization/presets`);
  }

  getSymbols(): Observable<SymbolInfo[]> {
    return this.http.get<SymbolInfo[]>(`${this.baseUrl}/PiootooOptimization/symbols`);
  }

  // ============ STRATEGIES ============

  getAllStrategies(): Observable<StrategyDefinition[]> {
    return this.http.get<StrategyDefinition[]>(`${this.baseUrl}/PiootooOptimization/strategies`);
  }

  getStrategiesBySymbol(symbol: string): Observable<StrategyDefinition[]> {
    return this.http.get<StrategyDefinition[]>(`${this.baseUrl}/PiootooOptimization/strategies/symbol/${encodeURIComponent(symbol)}`);
  }

  getStrategiesBySymbols(symbols: string[]): Observable<StrategyDefinition[]> {
    return this.http.post<StrategyDefinition[]>(`${this.baseUrl}/PiootooOptimization/strategies/symbols`, symbols);
  }

  getStrategiesGrouped(): Observable<SymbolStrategiesInfo[]> {
    return this.http.get<SymbolStrategiesInfo[]>(`${this.baseUrl}/PiootooOptimization/strategies/grouped`);
  }

  getSymbolsWithStrategies(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/PiootooOptimization/strategies/available-symbols`);
  }

  // ============ SETUPS ============

  getAllSetups(): Observable<SavedSetup[]> {
    return this.http.get<SavedSetup[]>(`${this.baseUrl}/PiootooOptimization/setups`);
  }

  getSetup(id: string): Observable<SavedSetup> {
    return this.http.get<SavedSetup>(`${this.baseUrl}/PiootooOptimization/setups/${id}`);
  }

  getActiveSetups(): Observable<SavedSetup[]> {
    return this.http.get<SavedSetup[]>(`${this.baseUrl}/PiootooOptimization/setups/active`);
  }

  createSetup(setup: SavedSetup): Observable<SavedSetup> {
    return this.http.post<SavedSetup>(`${this.baseUrl}/PiootooOptimization/setups`, setup);
  }

  updateSetup(id: string, setup: SavedSetup): Observable<SavedSetup> {
    return this.http.put<SavedSetup>(`${this.baseUrl}/PiootooOptimization/setups/${id}`, setup);
  }

  deleteSetup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/PiootooOptimization/setups/${id}`);
  }

  activateSetup(id: string, active: boolean): Observable<SavedSetup> {
    return this.http.patch<SavedSetup>(`${this.baseUrl}/PiootooOptimization/setups/${id}/activate?active=${active}`, {});
  }

  // ============ SAVED OPTIMIZATION RESULTS ============

  /** Ottiene tutte le ottimizzazioni salvate */
  getSavedOptimizations(): Observable<FilteredBacktestingResult[]> {
    return this.http.get<FilteredBacktestingResult[]>(`${this.baseUrl}/PiootooOptimization/list`);
  }

  /** Ottiene un'ottimizzazione salvata per ID */
  getSavedOptimization(id: string): Observable<FilteredBacktestingResult> {
    return this.http.get<FilteredBacktestingResult>(`${this.baseUrl}/PiootooOptimization/detail/${id}`);
  }

  /** Elimina un'ottimizzazione salvata */
  deleteOptimization(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/PiootooOptimization/detail/${id}`);
  }

  // ============ BACKTESTING ============

  runBacktest(request: BacktestRequest): Observable<BacktestResponse> {
    return this.http.post<BacktestResponse>(`${this.baseUrl}/PiootooBacktesting/run`, request);
  }

  getHistoricalData(symbol: string, startDate: string, endDate: string, barType: string = 'OneMinute'): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/PiootooBacktesting/data/${symbol}?startDate=${startDate}&endDate=${endDate}&barType=${barType}`);
  }

  getAvailableDates(symbol: string, barType: string = 'OneMinute'): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/PiootooBacktesting/available-dates/${symbol}?barType=${barType}`);
  }

  // ============ REALTIME ============

  getCurrentSetup(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/PiootooRealtime/current-setup`);
  }

  generateSignals(symbol: string, sessions: number = 5): Observable<any[]> {
    return this.http.post<any[]>(`${this.baseUrl}/PiootooRealtime/signals/${symbol}?sessions=${sessions}`, {});
  }

  getRepositoryInfo(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/PiootooRealtime/repository-info`);
  }
}
