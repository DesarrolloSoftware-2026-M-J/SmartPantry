import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-pantry',
  standalone: true,
  imports: [LocalizationPipe],
  templateUrl: './pantry.component.html',
})
export class PantryComponent {}