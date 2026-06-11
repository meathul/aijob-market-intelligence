import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgxDarkVeilComponent } from '@omnedia/ngx-dark-veil';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NgxDarkVeilComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'aijob-ui';
}
