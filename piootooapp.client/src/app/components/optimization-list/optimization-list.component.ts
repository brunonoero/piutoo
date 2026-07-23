import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { FilteredBacktestingResult } from '../../models/optimization.models';

@Component({
  selector: 'app-optimization-list',
  templateUrl: './optimization-list.component.html',
  styleUrls: ['./optimization-list.component.css'],
  standalone: false
})
export class OptimizationListComponent implements OnInit {
  optimizations: FilteredBacktestingResult[] = [];
  selectedIds: Set<string> = new Set();
  isLoading = false;
  error: string | null = null;

  constructor(
    private api: ApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadOptimizations();
  }

  loadOptimizations(): void {
    this.isLoading = true;
    this.error = null;
    this.api.getSavedOptimizations().subscribe({
      next: (results) => {
        this.optimizations = results || [];
        this.isLoading = false;
        console.log('Ottimizzazioni caricate:', this.optimizations.length);
      },
      error: (err) => {
        console.error('Errore caricamento ottimizzazioni:', err);
        this.error = 'Errore durante il caricamento: ' + (err.error?.message || err.message || 'Errore sconosciuto');
        this.isLoading = false;
      }
    });
  }

  navigateToNew(): void {
    this.router.navigate(['/optimization/request']);
  }

  navigateToDetail(optimization: FilteredBacktestingResult): void {
    // Usa l'originalBacktestingId come identificatore
    this.router.navigate(['/optimization', optimization.originalBacktestingId]);
  }

  toggleSelection(optimization: FilteredBacktestingResult): void {
    const id = optimization.originalBacktestingId;
    if (this.selectedIds.has(id)) {
      this.selectedIds.delete(id);
    } else {
      this.selectedIds.add(id);
    }
  }

  isSelected(optimization: FilteredBacktestingResult): boolean {
    return this.selectedIds.has(optimization.originalBacktestingId);
  }

  selectAll(): void {
    this.optimizations.forEach(o => this.selectedIds.add(o.originalBacktestingId));
  }

  deselectAll(): void {
    this.selectedIds.clear();
  }

  deleteSelected(): void {
    if (this.selectedIds.size === 0) return;

    if (!confirm(`Eliminare ${this.selectedIds.size} ottimizzazioni selezionate?`)) return;

    const deletePromises = Array.from(this.selectedIds).map(id =>
      this.api.deleteOptimization(id).toPromise()
    );

    Promise.all(deletePromises).then(() => {
      this.selectedIds.clear();
      this.loadOptimizations();
    }).catch(() => {
      this.error = 'Errore durante l\'eliminazione';
      this.loadOptimizations();
    });
  }

  deleteOptimization(optimization: FilteredBacktestingResult, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Eliminare "${optimization.setupName}"?`)) return;

    this.api.deleteOptimization(optimization.originalBacktestingId).subscribe({
      next: () => this.loadOptimizations(),
      error: () => this.error = 'Errore eliminazione'
    });
  }

  formatNumber(value: number, decimals: number = 2): string {
    return value?.toFixed(decimals) || '0';
  }

  formatPercent(value: number): string {
    if (value === undefined || value === null) return '0%';
    // Se il valore è già in percentuale (es. -15 per -15%), non moltiplicare
    if (Math.abs(value) > 1) {
      return value.toFixed(1) + '%';
    }
    return (value * 100).toFixed(1) + '%';
  }

  formatDate(date: string | Date): string {
    if (!date) return '-';
    const d = new Date(date);
    return d.toLocaleDateString('it-IT', { 
      day: '2-digit', 
      month: '2-digit', 
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatCurrency(value: number): string {
    if (value === undefined || value === null) return '€0';
    return new Intl.NumberFormat('it-IT', { 
      style: 'currency', 
      currency: 'EUR',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value);
  }
}
