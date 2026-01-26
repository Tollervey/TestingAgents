/**
 * Lightning Payments shared TypeScript types
 */

/** Payment status enum matching C# PaymentStatus */
export type PaymentStatus = 'Pending' | 'Paid' | 'Failed' | 'Expired' | 'RefundPending' | 'Refunded';

/** Payment kind enum matching C# PaymentKind */
export type PaymentKind = 'Paywall' | 'Tip';

/** Notification type matching C# NotificationType */
export type NotificationType = 'Email' | 'Webhook';

/** Notification event matching C# NotificationEvent */
export type NotificationEvent = 'PaymentConfirmed' | 'PaymentFailed' | 'PaymentExpired' | 'RefundInitiated' | 'RefundCompleted';

/** Notification status matching C# NotificationStatus */
export type NotificationStatus = 'Pending' | 'Sent' | 'Retrying' | 'Failed';

/** Refund status matching C# RefundStatus */
export type RefundStatus = 'Pending' | 'Succeeded' | 'Failed';

/** Payment summary for dashboard display */
export interface PaymentSummary {
  paymentHash: string;
  contentId: number;
  status: PaymentStatus;
  amountSat: number;
  kind: PaymentKind;
  createdAt: string;
}

/** Dashboard statistics */
export interface DashboardStats {
  totalPayments: number;
  totalAmountSat: number;
  pendingPayments: number;
  confirmedPayments: number;
  failedPayments: number;
  last24Hours: {
    payments: number;
    amountSat: number;
  };
}

/** Wallet balance information */
export interface WalletBalance {
  balanceSat: number;
  pendingReceiveSat: number;
  pendingSendSat: number;
}

/** Fiat amount representation */
export interface FiatAmount {
  amount: number;
  currency: string;
  formattedAmount: string;
}

/** API error response */
export interface ApiError {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
}

/** Bolt12 offer */
export interface Bolt12Offer {
  offerId: string;
  offerString: string;
  description: string;
  amountSat?: number;
  isActive: boolean;
  createdAt: string;
  deactivatedAt?: string;
  contentId?: number;
}

/** Refund transaction */
export interface RefundTransaction {
  refundId: string;
  originalPaymentHash: string;
  amountSat: number;
  status: RefundStatus;
  reason?: string;
  initiatedByUserId: string;
  initiatedAt: string;
  completedAt?: string;
  errorMessage?: string;
}

/** Payment notification */
export interface PaymentNotification {
  notificationId: string;
  paymentHash: string;
  type: NotificationType;
  event: NotificationEvent;
  destination: string;
  status: NotificationStatus;
  attemptCount: number;
  maxAttempts: number;
  createdAt: string;
  sentAt?: string;
  failedAt?: string;
  lastError?: string;
}

/** Exchange rate */
export interface ExchangeRate {
  currency: string;
  ratePerBtc: number;
  ratePerSat: number;
  fetchedAt: string;
  source: string;
  isStale: boolean;
}
