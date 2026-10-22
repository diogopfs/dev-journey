import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { User, UserRole } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { errorMessage } from '../../../core/http/problem-details';
import { strongPassword } from '../password.validator';
import { UserService } from '../user.service';

/** Formulário reaproveitado para criação (/users/new) e edição (/users/:id/edit). */
@Component({
  selector: 'app-user-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './user-form.html',
})
export class UserForm implements OnInit {
  private readonly users = inject(UserService);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  /** Preenchido automaticamente a partir do parâmetro de rota (withComponentInputBinding). */
  readonly id = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, strongPassword]],
    role: ['User' as UserRole],
    isActive: [true],
  });

  ngOnInit(): void {
    const id = this.id();
    if (!id) return;

    // Na edição a senha não é alterada por aqui (existe uma tela própria para isso).
    this.form.controls.password.disable();
    this.loading.set(true);
    this.users.getById(id).subscribe({
      next: (user) => {
        this.form.patchValue(user);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.loading.set(false);
      },
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    const id = this.id();

    const request$: Observable<User> = id
      ? this.users.update(id, {
          name: value.name,
          email: value.email,
          // Apenas admins enviam perfil/status; a API rejeitaria caso um usuário comum tentasse.
          ...(this.auth.isAdmin() ? { role: value.role, isActive: value.isActive } : {}),
        })
      : this.users.create(value);

    request$.subscribe({
      next: (user) => {
        this.auth.updateCurrentUser(user);
        void this.router.navigate(['/users']);
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
