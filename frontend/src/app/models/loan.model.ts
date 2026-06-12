export type LoanStatus = 'active' | 'paid';

export interface Loan {
  id: number;
  amount: number;
  currentBalance: number;
  applicantName: string;
  status: LoanStatus;
}
