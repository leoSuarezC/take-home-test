import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { Loan } from './models/loan.model';
import { LoanService } from './services/loan.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  private readonly loanService = inject(LoanService);

  displayedColumns: string[] = [
    'amount',
    'currentBalance',
    'applicantName',
    'status',
  ];
  loans: Loan[] = [];
  loading = true;
  error: string | null = null;

  ngOnInit(): void {
    this.loanService.getLoans().subscribe({
      next: (loans) => {
        this.loans = loans;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load loans. Please verify the API is running and try again.';
        this.loading = false;
      },
    });
  }
}
