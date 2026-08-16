import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';

@Component({
  selector: 'app-data-page-card',
  imports: [ButtonModule, CardModule, MessageModule],
  template: `
    <p-card styleClass="data-page-card">
      <ng-template pTemplate="title">{{ title() }}</ng-template>
      <ng-template pTemplate="subtitle">{{ subtitle() }}</ng-template>
      <div class="page-meta">
        <span class="count-pill">{{ count() }} {{ countLabel() }}</span>
        <button pButton type="button" label="Refresh" icon="pi pi-refresh"
          [loading]="loading()" (click)="refresh.emit()"></button>
      </div>
      @if (error()) {
        <p-message severity="error" [text]="error()"></p-message>
      }
      <ng-content />
    </p-card>
  `,
  styles: [`
    .page-meta { display:flex; align-items:center; justify-content:space-between; gap:1rem; margin:1rem 0; }
    .count-pill { padding:.45rem .8rem; border-radius:999px; background:#e0e7ff; color:#3730a3; font-weight:600; }
    @media (max-width:48rem) { .page-meta { align-items:stretch; flex-direction:column; } }
  `]
})
export class DataPageCardComponent {
  readonly title = input.required<string>();
  readonly subtitle = input('');
  readonly count = input(0);
  readonly countLabel = input('items');
  readonly loading = input(false);
  readonly error = input('');
  readonly refresh = output<void>();
}
