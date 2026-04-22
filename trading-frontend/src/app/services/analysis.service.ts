import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HistoricalBar } from '../models/historical-bar.model';
import { ChartRange } from '../models/enums/chart-range.enum';
import { ChartTimeframe } from '../models/enums/chart-timeframe.enum';
import { AnalysisResponse } from '../models/analysis.model';

@Injectable({
  providedIn: 'root',
})
export class AnalysisService {
  private apiUrl = 'https://localhost:7051/api/Analysis';

  constructor (private http: HttpClient){}

  public getAIAnalysis(ticker: string): Observable<AnalysisResponse> {
    return this.http.get<AnalysisResponse>(`${this.apiUrl}/${ticker}`);
  }
}
