import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  template: `
    <div class="container">
      <header class="card">
        <h1>DMN Tester UI</h1>
        <p>Test your DMN files with ease</p>
      </header>
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [`
    header {
      text-align: center;
      margin-bottom: 30px;
    }
    
    h1 {
      color: #007bff;
      margin-bottom: 10px;
    }
    
    p {
      color: #666;
      font-size: 16px;
    }
  `]
})
export class AppComponent {
  title = 'dmn-tester-ui';
} 