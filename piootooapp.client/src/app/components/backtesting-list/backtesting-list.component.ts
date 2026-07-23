import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BacktestingService } from '../../services/backtesting.service';
import { BacktestingResult } from '../../models/backtesting-new.models';

@Component({
  selector: 'app-backtesting-list',
  templateUrl: './backtesting-list.component.html',
  styleUrls: ['./backtesting-list.component.css'],
  standalone: false
})
export class BacktestingListComponent implements OnInit {
  backtestingList: BacktestingResult[] = [];
  selectedBacktestingIds: Set<string> = new Set();
  isLoading = false;
  error: string | null = null;

  constructor(
    private backtestingService: BacktestingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadBacktestingList();
  }

  loadBacktestingList(): void {
    this.isLoading = true;
    this.error = null;
    this.backtestingService.getCompletedBacktestings().subscribe({
      next: (backtestings) => {
        this.backtestingList = backtestings || [];
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Errore caricamento lista backtesting:', err);
        this.error = 'Errore durante il caricamento della lista: ' + (err.error?.error || err.message || 'Errore sconosciuto');
        this.isLoading = false;
      }
    });
  }

  navigateToNew(): void {
    this.router.navigate(['/backtesting/request']);
  }

  navigateToDetail(backtesting: BacktestingResult): void {
    this.router.navigate(['/backtesting', backtesting.jobId]);
  }

  toggleBacktestingSelection(backtesting: BacktestingResult): void {
    if (this.selectedBacktestingIds.has(backtesting.jobId)) {
      this.selectedBacktestingIds.delete(backtesting.jobId);
    } else {
      this.selectedBacktestingIds.add(backtesting.jobId);
    }
  }

  isBacktestingSelected(backtesting: BacktestingResult): boolean {
    return this.selectedBacktestingIds.has(backtesting.jobId);
  }

  selectAllBacktestings(): void {
    this.backtestingList.forEach(bt => this.selectedBacktestingIds.add(bt.jobId));
  }

  deselectAllBacktestings(): void {
    this.selectedBacktestingIds.clear();
  }

  deleteSelectedBacktestings(): void {
    if (this.selectedBacktestingIds.size === 0) {
      this.error = 'Seleziona almeno un backtesting da eliminare';
      return;
    }

    if (!confirm(`Sei sicuro di voler eliminare ${this.selectedBacktestingIds.size} backtesting?`)) {
      return;
    }

    const deletePromises = Array.from(this.selectedBacktestingIds).map(jobId =>
      this.backtestingService.deleteBacktesting(jobId).toPromise()
    );

    Promise.all(deletePromises).then(() => {
      this.selectedBacktestingIds.clear();
      this.loadBacktestingList();
    }).catch((err) => {
      console.error('Errore durante l\'eliminazione:', err);
      this.error = 'Errore durante l\'eliminazione dei backtesting';
      this.loadBacktestingList();
    });
  }

  deleteBacktesting(backtesting: BacktestingResult, event: Event): void {
    event.stopPropagation();
    
    if (!confirm(`Sei sicuro di voler eliminare il backtesting "${backtesting.setupName}"?`)) {
      return;
    }

    this.backtestingService.deleteBacktesting(backtesting.jobId).subscribe({
      next: () => {
        this.loadBacktestingList();
      },
      error: (err) => {
        console.error('Errore durante l\'eliminazione:', err);
        this.error = 'Errore durante l\'eliminazione: ' + err.message;
      }
    });
  }

  formatPercent(value: number): string {
    return (value * 100).toFixed(2) + '%';
  }

  formatCurrency(value: number): string {
    return '$' + value.toFixed(2);
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('it-IT', { 
      year: 'numeric', 
      month: '2-digit', 
      day: '2-digit' 
    });
  }
}
