import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PiootooSetup } from '../models/settings.models';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private readonly baseUrl = '/api/settings';

  constructor(private http: HttpClient) {}

  getAvailableSymbols(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/symbols`);
  }

  getAllSetups(): Observable<PiootooSetup[]> {
    return this.http.get<PiootooSetup[]>(`${this.baseUrl}/setups`);
  }

  getSetupById(id: string): Observable<PiootooSetup> {
    return this.http.get<PiootooSetup>(`${this.baseUrl}/setups/${id}`);
  }

  createSetup(setup: PiootooSetup): Observable<PiootooSetup> {
    return this.http.post<PiootooSetup>(`${this.baseUrl}/setups`, setup);
  }

  updateSetup(id: string, setup: PiootooSetup): Observable<PiootooSetup> {
    return this.http.put<PiootooSetup>(`${this.baseUrl}/setups/${id}`, setup);
  }

  deleteSetup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/setups/${id}`);
  }
}
