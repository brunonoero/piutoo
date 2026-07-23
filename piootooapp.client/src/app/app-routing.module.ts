import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { OptimizationComponent } from './components/optimization/optimization.component';
import { OptimizationListComponent } from './components/optimization-list/optimization-list.component';
import { OptimizationRequestComponent } from './components/optimization-request/optimization-request.component';
import { OptimizationDetailComponent } from './components/optimization-detail/optimization-detail.component';
import { BacktestingComponent } from './components/backtesting/backtesting.component';
import { BacktestingNewComponent } from './components/backtesting-new/backtesting-new.component';
import { BacktestingListComponent } from './components/backtesting-list/backtesting-list.component';
import { BacktestingRequestComponent } from './components/backtesting-request/backtesting-request.component';
import { BacktestingDetailComponent } from './components/backtesting-detail/backtesting-detail.component';
import { SapiooComponent } from './components/sapioo/sapioo.component';

const routes: Routes = [
  { path: '', redirectTo: '/backtesting', pathMatch: 'full' },
  
  // Backtesting routes
  { path: 'backtesting', component: BacktestingListComponent },
  { path: 'backtesting/request', component: BacktestingRequestComponent },
  { path: 'backtesting/:jobId', component: BacktestingDetailComponent },
  
  // Optimization routes
  { path: 'optimization', component: OptimizationListComponent },
  { path: 'optimization/request', component: OptimizationRequestComponent },
  { path: 'optimization/:setupId', component: OptimizationDetailComponent },
  
  // Legacy routes
  { path: 'backtesting-old', component: BacktestingComponent },
  { path: 'backtesting-legacy', component: BacktestingNewComponent },
  { path: 'optimization-legacy', component: OptimizationComponent },
  
  { path: 'sapioo', component: SapiooComponent },
  { path: '**', redirectTo: '/backtesting' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
