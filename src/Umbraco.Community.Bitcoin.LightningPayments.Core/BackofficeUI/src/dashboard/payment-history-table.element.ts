import { LitElement, html, css } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { LightningApiClient } from '../shared/lightning-api-client.js';
import type { PaymentListResponse, PaymentSummaryDto, PaymentStatus } from '../shared/types.js';

/**
 * Payment History Table Component
 * Displays paginated list of payments with status badges
 */
@customElement('lightning-payment-history-table')
export class PaymentHistoryTableElement extends LitElement {
  static styles = css`
    :host {
      display: block;
    }

    .table-container {
      overflow-x: auto;
    }

    .pagination-controls {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: var(--uui-size-space-4);
      border-top: 1px solid var(--uui-color-border);
    }

    .pagination-info {
      font-size: 0.875rem;
      color: var(--uui-color-text-alt);
    }

    .payment-hash {
      font-family: monospace;
      font-size: 0.875rem;
    }

    .status-badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: var(--uui-border-radius);
      font-size: 0.75rem;
      font-weight: 600;
    }

    .status-paid {
      background-color: var(--uui-color-positive);
      color: white;
    }

    .status-pending {
      background-color: var(--uui-color-warning);
      color: black;
    }

    .status-failed {
      background-color: var(--uui-color-danger);
      color: white;
    }

    .status-expired {
      background-color: var(--uui-color-disabled);
      color: white;
    }

    .status-refunded,
    .status-refundpending {
      background-color: var(--uui-color-focus);
      color: white;
    }

    .clickable-row {
      cursor: pointer;
    }

    .clickable-row:hover {
      background-color: var(--uui-color-surface-emphasis);
    }

    .empty-state {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: var(--uui-size-space-6);
      color: var(--uui-color-text-alt);
    }

    .error {
      color: var(--uui-color-danger);
      padding: var(--uui-size-space-4);
    }
  `;

  @state() private _payments: PaymentListResponse | null = null;
  @state() private _currentPage = 1;
  @state() private _pageSize = 20;
  @state() private _loading = true;
  @state() private _error: string | null = null;

  async connectedCallback() {
    super.connectedCallback();
    await this._loadData();
  }

  private async _loadData() {
    this._loading = true;
    this._error = null;

    const skip = (this._currentPage - 1) * this._pageSize;
    const result = await LightningApiClient.getPayments(skip, this._pageSize);

    if (result.ok) {
      this._payments = result.data;
    } else {
      this._error = result.error.detail || result.error.title || 'Failed to load payments';
    }

    this._loading = false;
  }

  private async _onPageChange(page: number) {
    this._currentPage = page;
    await this._loadData();
  }

  private _onRowClick(payment: PaymentSummaryDto) {
    this.dispatchEvent(new CustomEvent('payment-selected', {
      detail: { paymentHash: payment.paymentHash },
      bubbles: true,
      composed: true,
    }));
  }

  private _truncateHash(hash: string): string {
    return hash.length > 16 ? `${hash.substring(0, 8)}...${hash.substring(hash.length - 8)}` : hash;
  }

  private _formatSats(sats: number): string {
    return sats.toLocaleString();
  }

  private _formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  private _getStatusBadgeClass(status: PaymentStatus): string {
    return `status-badge status-${status.toLowerCase()}`;
  }

  private _getTotalPages(): number {
    if (!this._payments) return 0;
    return Math.ceil(this._payments.total / this._pageSize);
  }

  render() {
    if (this._loading) {
      return html`<uui-loader></uui-loader>`;
    }

    if (this._error) {
      return html`<div class="error">${this._error}</div>`;
    }

    return html`
      <uui-box headline="Recent Payments">
        ${this._renderTable()}
        ${this._renderPagination()}
      </uui-box>
    `;
  }

  private _renderTable() {
    if (!this._payments || this._payments.items.length === 0) {
      return html`<div class="empty-state">No payments found</div>`;
    }

    return html`
      <div class="table-container">
        <uui-table>
          <uui-table-head>
            <uui-table-head-cell>Payment Hash</uui-table-head-cell>
            <uui-table-head-cell>Amount</uui-table-head-cell>
            <uui-table-head-cell>Status</uui-table-head-cell>
            <uui-table-head-cell>Kind</uui-table-head-cell>
            <uui-table-head-cell>Content</uui-table-head-cell>
            <uui-table-head-cell>Created</uui-table-head-cell>
          </uui-table-head>
          ${this._payments.items.map(payment => html`
            <uui-table-row
              class="clickable-row"
              @click=${() => this._onRowClick(payment)}>
              <uui-table-cell>
                <span class="payment-hash" title=${payment.paymentHash}>
                  ${this._truncateHash(payment.paymentHash)}
                </span>
              </uui-table-cell>
              <uui-table-cell>${this._formatSats(payment.amountSat)} sats</uui-table-cell>
              <uui-table-cell>
                <span class=${this._getStatusBadgeClass(payment.status)}>
                  ${payment.status}
                </span>
              </uui-table-cell>
              <uui-table-cell>${payment.kind}</uui-table-cell>
              <uui-table-cell>
                ${payment.contentName || `Content #${payment.contentId}`}
              </uui-table-cell>
              <uui-table-cell>${this._formatDate(payment.createdAt)}</uui-table-cell>
            </uui-table-row>
          `)}
        </uui-table>
      </div>
    `;
  }

  private _renderPagination() {
    if (!this._payments || this._payments.total === 0) {
      return '';
    }

    const totalPages = this._getTotalPages();
    const startItem = (this._currentPage - 1) * this._pageSize + 1;
    const endItem = Math.min(this._currentPage * this._pageSize, this._payments.total);

    return html`
      <div class="pagination-controls">
        <span class="pagination-info">
          Showing ${startItem}-${endItem} of ${this._payments.total} payments
        </span>
        <uui-pagination
          .total=${totalPages}
          .current=${this._currentPage}
          @change=${(e: CustomEvent) => this._onPageChange(e.detail.current)}>
        </uui-pagination>
      </div>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    'lightning-payment-history-table': PaymentHistoryTableElement;
  }
}
