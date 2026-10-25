export interface TokenResponse { accessToken: string; expiresAt: string; }
export interface OtpRequestResponse { expiresAt: string; devCode: string | null; }

export interface Account {
  accountId: string;
  name: string;
  email: string;
  balance: number;
  phoneVerified: boolean;
}

export interface StatementItem {
  transferId: string;
  amount: number;
  description: string;
  createdAt: string;
}

export interface TransferRequest { toEmail: string; amount: number; description: string; }
export interface TransferResponse { id: string; amount: number; toEmail: string; description: string; createdAt: string; }

export interface SimulationRequest { amount: number; months: number; monthlyRatePercent: number; }
export interface SimulationResult {
  principal: number;
  months: number;
  monthlyRatePercent: number;
  installment: number;
  totalPaid: number;
  totalInterest: number;
}

export interface AdminAccount { accountId: string; name: string; email: string; balance: number; phoneVerified: boolean; }

export interface RegisterRequest { name: string; email: string; password: string; phone: string; }
