import { HttpErrorResponse } from '@angular/common/http';

/** Extrai a mensagem do ProblemDetails devolvido pela API, com um texto padrão quando não houver. */
export function errorMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) return 'Sem conexão com o servidor. Tente novamente.';
    const body = err.error as { detail?: string; title?: string } | null;
    return body?.detail ?? body?.title ?? 'Não foi possível concluir a operação.';
  }
  return 'Não foi possível concluir a operação.';
}
