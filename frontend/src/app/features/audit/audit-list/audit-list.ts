import { DatePipe, JsonPipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { errorMessage } from '../../../core/http/problem-details';
import { AuditLog, AuditService } from '../audit.service';

@Component({
  selector: 'app-audit-list',
  imports: [DatePipe, JsonPipe],
  templateUrl: './audit-list.html',
})
export class AuditList {
  private readonly audit = inject(AuditService);

  protected readonly logs = rxResource({ stream: () => this.audit.latest() });
  protected readonly loadError = computed(() => (this.logs.error() ? errorMessage(this.logs.error()) : null));

  protected payload(log: AuditLog): unknown {
    return JSON.parse(log.payload);
  }

  /** Tempo entre o evento acontecer na API e ser processado pelo Worker (processamento assíncrono). */
  protected latencyMs(log: AuditLog): number {
    return new Date(log.processedAt).getTime() - new Date(log.occurredAt).getTime();
  }
}
