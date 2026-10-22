import { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * Mesmas regras de senha da API. A validação no front é para UX;
 * a validação que realmente vale é a do back-end.
 */
export function strongPassword(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value ?? '';
  const valid =
    value.length >= 8 && /[A-Z]/.test(value) && /[a-z]/.test(value) && /\d/.test(value) && /[^a-zA-Z0-9]/.test(value);
  return valid ? null : { strongPassword: true };
}
