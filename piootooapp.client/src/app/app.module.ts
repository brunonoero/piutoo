import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
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
import { ApiService } from './services/api.service';
import { BacktestingService } from './services/backtesting.service';
import { SapiooService } from './services/sapioo.service';
import { SettingsService } from './services/settings.service';

@NgModule({
  declarations: [
    AppComponent,
    OptimizationComponent,
    OptimizationListComponent,
    OptimizationRequestComponent,
    OptimizationDetailComponent,
    BacktestingComponent,
    BacktestingNewComponent,
    BacktestingListComponent,
    BacktestingRequestComponent,
    BacktestingDetailComponent,
    SapiooComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    AppRoutingModule,
    BaseChartDirective
  ],
  providers: [
    ApiService,
    BacktestingService,
    SapiooService,
    SettingsService,
    provideCharts(withDefaultRegisterables())
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
