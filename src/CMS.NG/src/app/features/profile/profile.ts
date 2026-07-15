import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';

import { AuthService } from '@core/services/auth.service';

/**
 * "My Profile" page for the signed-in user. Shows the read-only UserId and roles (decoded from the
 * JWT) and lets the user edit only their own UserName. Saving calls the auth service, which updates
 * session storage and the shell's user-name signal on success.
 */
@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, InputTextModule, ButtonModule],
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

  protected readonly form = this.fb.group({
    userName: [this.auth.userName() ?? '', [Validators.required]],
  });

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

  protected invalid(): boolean {
    const c = this.form.get('userName');
    return !!c && c.invalid && (c.dirty || c.touched);
  }
}
