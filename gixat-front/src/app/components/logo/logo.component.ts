import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-logo',
  standalone: true,
  imports: [RouterLink],
  template: `
    <a routerLink="/" class="flex items-center gap-2 group">
      <img 
        src="/gixat-logo.png" 
        alt="Gixat Logo" 
        [style.height]="height() + 'px'"
        class="object-contain transition-transform duration-300 group-hover:scale-105"
      />
    </a>
  `
})
export class LogoComponent {
  height = input<number>(40);
}
