import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, of } from 'rxjs';
import { switchMap, takeWhile, catchError } from 'rxjs/operators';
import {
  BacktestingRequest,
  BacktestingJob,
  BacktestingResult,
  BacktestingJobStatus
} from '../models/backtesting-new.models';

@Injectable({
  providedIn: 'root'
})
export class BacktestingService {
  private readonly baseUrl = '/api/backtesting';

  constructor(private http: HttpClient) {}

  startBacktesting(request: BacktestingRequest): Observable<{ jobId: string }> {
    return this.http.post<{ jobId: string }>(`${this.baseUrl}/start`, request);
  }

  getStatus(jobId: string): Observable<BacktestingJob> {
    return this.http.get<BacktestingJob>(`${this.baseUrl}/status/${jobId}`);
  }

  getResult(jobId: string): Observable<BacktestingResult> {
    return this.http.get<BacktestingResult>(`${this.baseUrl}/result/${jobId}`);
  }

  getCompletedBacktestings(): Observable<BacktestingResult[]> {
    return this.http.get<BacktestingResult[]>(`${this.baseUrl}/list`);
  }

  deleteBacktesting(jobId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${jobId}`);
  }

  /**
   * Polling per lo stato del job fino al completamento
   */
  pollJobStatus(jobId: string, intervalMs: number = 2000): Observable<BacktestingJob> {
    return interval(intervalMs).pipe(
      switchMap(() => {
        return this.getStatus(jobId).pipe(
          catchError(err => {
            // Se 404, il job potrebbe essere completato e rimosso
            // Restituisci un job completato per fermare il polling
            if (err.status === 404) {
              console.warn('Job non trovato durante polling, potrebbe essere completato');
              return of({
                jobId: jobId,
                status: BacktestingJobStatus.Completed,
                progressPercent: 100,
                startedAt: new Date().toISOString(),
                completedAt: new Date().toISOString()
              } as BacktestingJob);
            }
            // Per altri errori, rilancia
            throw err;
          })
        );
      }),
      takeWhile((job): job is BacktestingJob => {
        if (!job) return false;
        const shouldContinue = job.status === BacktestingJobStatus.Pending || 
                               job.status === BacktestingJobStatus.Running;
        // Se completato o fallito, ferma il polling
        return shouldContinue;
      }, true) // Include l'ultimo valore (Completed o Failed)
    );
  }
}
