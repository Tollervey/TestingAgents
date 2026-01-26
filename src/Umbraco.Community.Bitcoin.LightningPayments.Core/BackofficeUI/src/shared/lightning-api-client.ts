/**
 * Lightning Payments Management API Client
 */

import type { DashboardStats, WalletBalance, PaymentSummary, ApiError, Bolt12Offer, RefundTransaction, PaymentNotification, ExchangeRate } from './types.js';

const BASE_URL = '/umbraco/management/api/v1/lightning-payments';

/** Generic API response type */
export type ApiResponse<T> =
  | { ok: true; data: T }
  | { ok: false; error: ApiError };

/** Fetch wrapper with error handling */
async function fetchApi<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  try {
    const response = await fetch(`${BASE_URL}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
    });

    if (!response.ok) {
      const error: ApiError = await response.json().catch(() => ({
        status: response.status,
        title: response.statusText,
      }));
      return { ok: false, error };
    }

    const data = await response.json() as T;
    return { ok: true, data };
  } catch (err) {
    return {
      ok: false,
      error: {
        title: 'Network Error',
        detail: err instanceof Error ? err.message : 'Unknown error',
        status: 0,
      },
    };
  }
}

/** Lightning Payments API client */
export const LightningApiClient = {
  // Dashboard endpoints
  getDashboardStats: () => fetchApi<DashboardStats>('/dashboard/stats'),
  getWalletBalance: () => fetchApi<WalletBalance>('/dashboard/balance'),

  // Payment endpoints
  getRecentPayments: (limit = 10) => fetchApi<PaymentSummary[]>(`/payments?limit=${limit}`),
  getPayment: (paymentHash: string) => fetchApi<PaymentSummary>(`/payments/${paymentHash}`),

  // Bolt12 offer endpoints
  getOffers: () => fetchApi<Bolt12Offer[]>('/offers'),
  getOffer: (offerId: string) => fetchApi<Bolt12Offer>(`/offers/${offerId}`),
  createOffer: (data: { description: string; amountSat?: number }) =>
    fetchApi<Bolt12Offer>('/offers', { method: 'POST', body: JSON.stringify(data) }),
  deactivateOffer: (offerId: string) =>
    fetchApi<void>(`/offers/${offerId}/deactivate`, { method: 'POST' }),

  // Refund endpoints
  getRefunds: (paymentHash?: string) =>
    fetchApi<RefundTransaction[]>(paymentHash ? `/refunds?paymentHash=${paymentHash}` : '/refunds'),
  getRefund: (refundId: string) => fetchApi<RefundTransaction>(`/refunds/${refundId}`),
  initiateRefund: (data: { originalPaymentHash: string; amountSat: number; destinationInvoice: string; reason?: string }) =>
    fetchApi<RefundTransaction>('/refunds', { method: 'POST', body: JSON.stringify(data) }),

  // Notification endpoints
  getNotifications: (paymentHash?: string) =>
    fetchApi<PaymentNotification[]>(paymentHash ? `/notifications?paymentHash=${paymentHash}` : '/notifications'),
  retryNotification: (notificationId: string) =>
    fetchApi<void>(`/notifications/${notificationId}/retry`, { method: 'POST' }),

  // Exchange rate endpoints
  getExchangeRates: () => fetchApi<ExchangeRate[]>('/exchange-rates'),
  convertToFiat: (amountSat: number, currency: string) =>
    fetchApi<{ amount: number; formattedAmount: string }>(`/exchange-rates/convert?amountSat=${amountSat}&currency=${currency}`),
};
