import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

/** Log de auditoria lido do MongoDB (gravado pelo Worker ao consumir eventos do RabbitMQ). */
export interface AuditLog {
  eventId: string;
  eventType: string;
  userId: string | null;
  performedBy: string | null;
  payload: string;
  occurredAt: string;
  processedAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);

  latest(limit = 50): Observable<AuditLog[]> {
    return this.http.get<AuditLog[]>('/api/audit-logs', { params: new HttpParams().set('limit', limit) });
  }
}
