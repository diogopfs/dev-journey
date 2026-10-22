import { HttpErrorResponse } from '@angular/common/http';

/** Formato de erro padrão retornado pela API (RFC 9457 - Problem Details). */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

/** Extrai uma mensagem amigável de um erro HTTP. */
export function errorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'Erro inesperado.';
  if (error.status === 0) return 'Não foi possível conectar à API.';
  if (error.status === 429) return 'Muitas tentativas. Aguarde um minuto e tente novamente.';

  const problem = error.error as ProblemDetails | null;
  const fieldErrors = problem?.errors ? Object.values(problem.errors).flat() : [];
  if (fieldErrors.length) return fieldErrors.join(' ');

  return problem?.detail ?? problem?.title ?? `Erro ${error.status}.`;
}
