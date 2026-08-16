import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-table-caption',
  imports: [ButtonModule],
  template: `
    <div class="caption">
      <div><div class="title">{{ title() }}</div>@if (subtitle()) { <small>{{ subtitle() }}</small> }</div>
      <div class="actions">
        <button pButton type="button" label="Clear filters" icon="pi pi-filter-slash"
          severity="secondary" (click)="clear.emit()"></button>
        <ng-content select="[actions]" />
      </div>
    </div>
  `,
  styles: [`
    .caption,.actions { display:flex; align-items:center; gap:.75rem; }
    .caption { justify-content:space-between; }
    .title { color:#334155; font-weight:700; }
    @media (max-width:48rem) { .caption,.actions { align-items:stretch; flex-direction:column; } }
  `]
})
export class TableCaptionComponent {
  readonly title = input.required<string>();
  readonly subtitle = input('');
  readonly clear = output<void>();
}
