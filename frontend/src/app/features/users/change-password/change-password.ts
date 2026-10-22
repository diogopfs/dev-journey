import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { errorMessage } from '../../../core/http/problem-details';
import { strongPassword } from '../password.validator';
import { UserService } from '../user.service';

@Component({
  selector: 'app-change-password',
  imports: [ReactiveFormsModule],
  templateUrl: './change-password.html',
})
export class ChangePassword {
  private readonly users = inject(UserService);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly success = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, strongPassword]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.success.set(false);

    this.users.changePassword(this.form.getRawValue()).subscribe({
      next: () => {
        this.success.set(true);
        this.form.reset();
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(errorMessage(err));
        this.loading.set(false);
      },
    });
  }
}
