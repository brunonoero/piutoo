import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, of } from 'rxjs';
import { switchMap, takeWhile, catchError } from 'rxjs/operators';
import {
  OptimizationRequest,
  FilteredBacktestingResult,
  AdvancedOptimizationRequest,
  AdvancedOptimizationResult,
  OptimizationJob,
  OptimizationJobStatus
} from '../models/optimization.models';

@Injectable({
  providedIn: 'root'
})
export class OptimizationService {
  private readonly baseUrl = '/api/PiootooOptimization';

  constructor(private http: HttpClient) {}

  /**
   * Avvia un'ottimizzazione BASE in background
   */
  startOptimization(request: OptimizationRequest): Observable<{ jobId: string }> {
    return this.http.post<{ jobId: string }>(`${this.baseUrl}/start`, request);
  }

  /**
   * Avvia un'ottimizzazione AVANZATA in background
   */
  startAdvancedOptimization(request: AdvancedOptimizationRequest): Observable<{ jobId: string }> {
    return this.http.post<{ jobId: string }>(`${this.baseUrl}/start-advanced`, request);
  }

  /**
   * Ottiene lo stato di un job di ottimizzazione
   */
  getStatus(jobId: string): Observable<OptimizationJob> {
    return this.http.get<OptimizationJob>(`${this.baseUrl}/status/${jobId}`);
  }

  /**
   * Ottiene il risultato BASE di un job completato
   */
  getResult(jobId: string): Observable<FilteredBacktestingResult> {
    return this.http.get<FilteredBacktestingResult>(`${this.baseUrl}/result/${jobId}`);
  }

  /**
   * Ottiene il risultato AVANZATO di un job completato
   */
  getAdvancedResult(jobId: string): Observable<AdvancedOptimizationResult> {
    return this.http.get<AdvancedOptimizationResult>(`${this.baseUrl}/result-advanced/${jobId}`);
  }

  /**
   * Polling per lo stato del job fino al completamento
   */
  pollJobStatus(jobId: string, intervalMs: number = 1000): Observable<OptimizationJob> {
    return interval(intervalMs).pipe(
      switchMap(() => {
        return this.getStatus(jobId).pipe(
          catchError(err => {
            // Se 404, il job potrebbe essere completato e rimosso
            if (err.status === 404) {
              console.warn('Job non trovato durante polling, potrebbe essere completato');
              return of({
                jobId: jobId,
                status: OptimizationJobStatus.Completed,
                progressPercent: 100,
                startedAt: new Date().toISOString(),
                completedAt: new Date().toISOString()
              } as OptimizationJob);
            }
            throw err;
          })
        );
      }),
      takeWhile((job): job is OptimizationJob => {
        if (!job) return false;
        const shouldContinue = job.status === OptimizationJobStatus.Pending || 
                               job.status === OptimizationJobStatus.Running;
        return shouldContinue;
      }, true) // Include l'ultimo valore (Completed o Failed)
    );
  }

  // ========== LEGACY SYNC METHODS ==========
  
  /**
   * Ottimizzazione BASE sincrona (legacy)
   */
  optimizeSync(request: OptimizationRequest): Observable<FilteredBacktestingResult> {
    return this.http.post<FilteredBacktestingResult>(`${this.baseUrl}/optimize`, request);
  }

  /**
   * Ottimizzazione AVANZATA sincrona (legacy)
   */
  optimizeAdvancedSync(request: AdvancedOptimizationRequest): Observable<AdvancedOptimizationResult> {
    return this.http.post<AdvancedOptimizationResult>(`${this.baseUrl}/optimize-advanced`, request);
  }
}
