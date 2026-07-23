import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, of } from 'rxjs';
import { switchMap, takeWhile, catchError } from 'rxjs/operators';
import {
  SapiooRequest,
  SapiooJob,
  SapiooResult,
  SapiooJobStatus
} from '../models/sapioo.models';

@Injectable({
  providedIn: 'root'
})
export class SapiooService {
  private readonly baseUrl = '/api/sapioo';

  constructor(private http: HttpClient) {}

  startOptimization(request: SapiooRequest): Observable<{ jobId: string }> {
    return this.http.post<{ jobId: string }>(`${this.baseUrl}/start`, request);
  }

  getStatus(jobId: string): Observable<SapiooJob> {
    return this.http.get<SapiooJob>(`${this.baseUrl}/status/${jobId}`);
  }

  getResult(jobId: string): Observable<SapiooResult> {
    return this.http.get<SapiooResult>(`${this.baseUrl}/result/${jobId}`);
  }

  getAvailableBacktestings(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/backtestings`);
  }

  getCompletedOptimizations(): Observable<SapiooResult[]> {
    return this.http.get<SapiooResult[]>(`${this.baseUrl}/list`);
  }

  deleteOptimization(jobId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${jobId}`);
  }

  /**
   * Polling per lo stato del job fino al completamento
   */
  pollJobStatus(jobId: string, intervalMs: number = 2000): Observable<SapiooJob> {
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
                status: SapiooJobStatus.Completed,
                progressPercent: 100,
                startedAt: new Date().toISOString(),
                completedAt: new Date().toISOString()
              } as SapiooJob);
            }
            // Per altri errori, rilancia
            throw err;
          })
        );
      }),
      takeWhile((job): job is SapiooJob => {
        if (!job) return false;
        const shouldContinue = job.status === SapiooJobStatus.Pending || 
                               job.status === SapiooJobStatus.Running;
        // Se completato o fallito, ferma il polling
        return shouldContinue;
      }, true) // Include l'ultimo valore (Completed o Failed)
    );
  }
}
