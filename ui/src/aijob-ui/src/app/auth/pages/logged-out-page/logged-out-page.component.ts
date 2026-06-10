import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-logged-out-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './logged-out-page.component.html'
})
export class LoggedOutPageComponent {}
