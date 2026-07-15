import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

/**
 * Thin root component: just the routed outlet plus the global toast/confirm hosts. The authenticated
 * chrome (sidebar, header) lives in the guarded {@link Shell} layout so public routes like the login
 * page render on their own.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastModule, ConfirmDialogModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
