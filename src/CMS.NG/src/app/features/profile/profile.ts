import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { AuthService } from '@core/services/auth.service';

/** Bilingual complexity rule — the same text the backend returns on a policy violation. */
export const PASSWORD_RULE_MESSAGE =
  '密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號 ' +
  '(Password must be at least 8 characters and contain at least 3 of the 4 classes: ' +
  'uppercase / lowercase / digit / symbol.)';

/** Mirrors the backend PasswordPolicy: length >= 8 and >= 3 of the 4 character classes. */
function passwordComplexity(control: AbstractControl): ValidationErrors | null {
  const value = (control.value as string) ?? '';
  if (!value) {
    return null; // emptiness is the `required` validator's job
  }
  const classes =
    Number(/[A-Z]/.test(value)) +
    Number(/[a-z]/.test(value)) +
    Number(/[0-9]/.test(value)) +
    Number(/[^A-Za-z0-9]/.test(value));
  return value.length >= 8 && classes >= 3 ? null : { complexity: true };
}

/** Group validator: newPassword and confirmPassword must agree (once both are filled). */
function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const newPassword = group.get('newPassword')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return newPassword && confirm && newPassword !== confirm ? { mismatch: true } : null;
}

/**
 * "My Profile" page for the signed-in user. Shows the read-only UserId and roles (decoded from the
 * JWT) and lets the user edit only their own UserName. Saving calls the auth service, which updates
 * session storage and the shell's user-name signal on success. A second, independent form changes
 * the user's own password (current password verified server-side; complexity mirrored client-side).
 */
@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, InputTextModule, PasswordModule, ButtonModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  /** Read-only identity fields, straight from the stored profile / token. */
  protected readonly userId = this.auth.userId;
  protected readonly roles = this.auth.roles;

  protected readonly saving = signal(false);
  protected readonly pwSaving = signal(false);

  protected readonly passwordRule = PASSWORD_RULE_MESSAGE;

  protected readonly form = this.fb.group({
    userName: [this.auth.userName() ?? '', [Validators.required]],
  });

  /** Independent change-password form; passwords are never trimmed or transformed. */
  protected readonly pwForm = this.fb.group(
    {
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, passwordComplexity]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatch },
  );

  save(): void {
    const userName = (this.form.getRawValue().userName ?? '').trim();
    if (!userName) {
      this.form.get('userName')?.markAsTouched();
      this.messages.add({ severity: 'warn', summary: '無法儲存', detail: '請輸入使用者名稱。' });
      return;
    }

    this.saving.set(true);
    this.auth.updateUserName(userName).subscribe({
      next: (res) => {
        this.saving.set(false);
        // Reflect the server-canonical (trimmed) name back into the form.
        this.form.get('userName')?.setValue(res.userName);
        this.messages.add({ severity: 'success', summary: '已儲存', detail: '使用者名稱已更新。' });
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 400 ? '請輸入有效的使用者名稱。' : '更新時發生錯誤，請稍後再試。';
        this.messages.add({ severity: 'error', summary: '更新失敗', detail });
      },
    });
  }

  changePassword(): void {
    if (this.pwForm.invalid) {
      this.pwForm.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmPassword } = this.pwForm.getRawValue();
    this.pwSaving.set(true);
    this.auth
      .changePassword({
        currentPassword: currentPassword!,
        newPassword: newPassword!,
        confirmNewPassword: confirmPassword!,
      })
      .subscribe({
        next: () => {
          this.pwSaving.set(false);
          this.pwForm.reset();
          this.messages.add({ severity: 'success', summary: '已變更', detail: '密碼已更新。' });
        },
        error: (err: HttpErrorResponse) => {
          this.pwSaving.set(false);
          // The backend's bilingual message (wrong current password / policy / mismatch) shows as-is.
          const detail =
            (err.error as { message?: string } | null)?.message ??
            '變更密碼時發生錯誤，請稍後再試。';
          this.messages.add({ severity: 'error', summary: '變更失敗', detail });
        },
      });
  }

  protected invalid(): boolean {
    const c = this.form.get('userName');
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  protected pwInvalid(control: string): boolean {
    const c = this.pwForm.get(control);
    return !!c && c.invalid && (c.dirty || c.touched);
  }

  /** Mismatch is a group-level error, surfaced under the confirm field once it has been touched. */
  protected mismatch(): boolean {
    const confirm = this.pwForm.get('confirmPassword');
    return this.pwForm.hasError('mismatch') && !!confirm && (confirm.dirty || confirm.touched);
  }
}
