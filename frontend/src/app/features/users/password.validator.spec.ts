import { FormControl } from '@angular/forms';
import { strongPassword } from './password.validator';

describe('strongPassword', () => {
  it.each(['curta1!', 'semnumero!A', 'SEMMINUSCULA1!', 'SemEspecial1'])('deve rejeitar "%s"', (value) => {
    expect(strongPassword(new FormControl(value, { nonNullable: true }))).toEqual({ strongPassword: true });
  });

  it('deve aceitar senha forte', () => {
    expect(strongPassword(new FormControl('Senha@123', { nonNullable: true }))).toBeNull();
  });
});
