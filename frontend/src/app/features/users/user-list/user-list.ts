import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { User } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { errorMessage } from '../../../core/http/problem-details';
import { UserService } from '../user.service';

@Component({
  selector: 'app-user-list',
  imports: [RouterLink, DatePipe],
  templateUrl: './user-list.html',
})
export class UserList {
  private readonly users = inject(UserService);
  protected readonly auth = inject(AuthService);

  protected readonly search = signal('');
  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected readonly actionError = signal<string | null>(null);

  /**
   * rxResource: recarrega automaticamente sempre que os signals usados em "params" mudam
   * (busca ou página), cancelando a requisição anterior.
   */
  protected readonly result = rxResource({
    params: () => ({ search: this.search(), page: this.page() }),
    stream: ({ params }) => this.users.list(params.search, params.page, this.pageSize),
  });

  protected readonly loadError = computed(() => (this.result.error() ? errorMessage(this.result.error()) : null));

  protected onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  protected goTo(page: number): void {
    this.page.set(page);
  }

  protected canEdit(user: User): boolean {
    return this.auth.isAdmin() || this.auth.user()?.id === user.id;
  }

  protected remove(user: User): void {
    if (!confirm(`Excluir o usuário "${user.name}"?`)) return;

    this.actionError.set(null);
    this.users.delete(user.id).subscribe({
      next: () => this.result.reload(),
      error: (err) => this.actionError.set(errorMessage(err)),
    });
  }
}
