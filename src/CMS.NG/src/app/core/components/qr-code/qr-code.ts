import { Component, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { toDataURL } from 'qrcode';
import { from, switchMap } from 'rxjs';
import { ButtonModule } from 'primeng/button';

/**
 * Renders a QR code for `value` with `title` shown above it, plus a download
 * link for the generated PNG. Reusable across any detail page that needs a
 * scannable link to a record's public page.
 */
@Component({
  selector: 'app-qr-code',
  imports: [ButtonModule],
  templateUrl: './qr-code.html',
  styleUrl: './qr-code.css',
})
export class QrCode {
  readonly value = input.required<string>();
  readonly title = input<string | null>(null);
  readonly downloadFileName = input('qrcode.png');

  protected readonly dataUrl = toSignal(
    toObservable(this.value).pipe(switchMap((value) => from(toDataURL(value)))),
    { initialValue: null },
  );

  download(): void {
    const url = this.dataUrl();
    if (!url) {
      return;
    }
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = this.downloadFileName();
    anchor.click();
  }
}
