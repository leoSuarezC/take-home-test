import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Loan } from '../models/loan.model';

@Injectable({ providedIn: 'root' })
export class LoanService {
  private readonly http = inject(HttpClient);
  private readonly loansUrl = `${environment.apiUrl}/loans`;

  getLoans(): Observable<Loan[]> {
    return this.http.get<Loan[]>(this.loansUrl);
  }
}
