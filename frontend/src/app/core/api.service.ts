import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Account,
  AdminAccount,
  SimulationRequest,
  SimulationResult,
  StatementItem,
  TransferRequest,
  TransferResponse,
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  getAccount(): Observable<Account> {
    return this.http.get<Account>('/api/accounts/me');
  }

  getStatement(take = 20): Observable<StatementItem[]> {
    return this.http.get<StatementItem[]>('/api/accounts/me/transactions', { params: { take } });
  }

  transfer(body: TransferRequest, idempotencyKey: string): Observable<TransferResponse> {
    return this.http.post<TransferResponse>('/api/transfers', body, {
      headers: { 'Idempotency-Key': idempotencyKey },
    });
  }

  simulate(body: SimulationRequest): Observable<SimulationResult> {
    return this.http.post<SimulationResult>('/api/credit/simulations', body);
  }

  adminAccounts(): Observable<AdminAccount[]> {
    return this.http.get<AdminAccount[]>('/api/admin/accounts');
  }
}
