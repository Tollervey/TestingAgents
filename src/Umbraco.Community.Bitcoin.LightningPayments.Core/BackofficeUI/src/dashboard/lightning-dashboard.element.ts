import { LitElement, html, css } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { LightningApiClient } from '../shared/lightning-api-client.js';
import type { DashboardStatsResponse } from '../shared/types.js';
import './connection-status.element.js';
import './payment-chart.element.js';
import './payment-history-table.element.js';

/**
 * Lightning Dashboard Main Component
 * Orchestrates all dashboard sub-components and stats display
 */
@customElement('lightning-dashboard')
export class LightningDashboardElement extends LitElement {
  static styles = css`
    :host {
      display: block;
      padding: var(--uui-size-layout-1);
    }

    .dashboard-container {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-6);
    }

    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .dashboard-title {
      font-size: 2rem;
      font-weight: 600;
      margin: 0;
    }

    .refresh-button {
      cursor: pointer;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: var(--uui-size-space-4);
    }

    .stat-card {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-2);
    }

    .stat-label {
      font-size: 0.875rem;
      color: var(--uui-color-text-alt);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .stat-value {
      font-size: 2rem;
      font-weight: 600;
      color: var(--uui-color-text);
    }

    .stat-secondary {
      font-size: 0.875rem;
      color: var(--uui-color-text-alt);
    }

    .error {
      color: var(--uui-color-danger);
      padding: var(--uui-size-space-4);
      background-color: var(--uui-color-danger-surface);
      border-radius: var(--uui-border-radius);
    }

    .loading-overlay {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 400px;
    }
  `;

  @state() private _stats: DashboardStatsResponse | null = null;
  @state() private _loading = true;
  @state() private _error: string | null = null;
  @state() private _autoRefreshInterval: number | null = null;

  async connectedCallback() {
    super.connectedCallback();
    await this._loadData();
    this._startAutoRefresh();
  }

  disconnectedCallback() {
    super.disconnectedCallback();
    this._stopAutoRefresh();
  }

  private async _loadData() {
    this._loading = true;
    this._error = null;

    const result = await LightningApiClient.getDashboardStats();

    if (result.ok) {
      this._stats = result.data;
    } else {
      this._error = result.error.detail || result.error.title || 'Failed to load dashboard stats';
    }

    this._loading = false;
  }

  private _startAutoRefresh() {
    // Auto-refresh every 30 seconds
    this._autoRefreshInterval = window.setInterval(() => {
      this._loadData();
    }, 30000);
  }

  private _stopAutoRefresh() {
    if (this._autoRefreshInterval !== null) {
      window.clearInterval(this._autoRefreshInterval);
      this._autoRefreshInterval = null;
    }
  }

  private async _onRefreshClick() {
    await this._loadData();
    // Force refresh of child components by re-rendering
    this.requestUpdate();
  }

  private _formatSats(sats: number): string {
    return sats.toLocaleString();
  }

  private _formatBTC(sats: number): string {
    const btc = sats / 100_000_000;
    return btc.toFixed(8);
  }

  render() {
    return html`
      <div class="dashboard-container">
        <div class="dashboard-header">
          <h1 class="dashboard-title">Lightning Payments Dashboard</h1>
          <uui-button
            class="refresh-button"
            look="outline"
            label="Refresh"
            @click=${this._onRefreshClick}>
            <uui-icon name="refresh"></uui-icon>
            Refresh
          </uui-button>
        </div>

        ${this._renderContent()}
      </div>
    `;
  }

  private _renderContent() {
    if (this._loading && !this._stats) {
      return html`
        <div class="loading-overlay">
          <uui-loader></uui-loader>
        </div>
      `;
    }

    if (this._error) {
      return html`<div class="error">${this._error}</div>`;
    }

    return html`
      ${this._renderStats()}
      <lightning-connection-status></lightning-connection-status>
      <lightning-payment-chart></lightning-payment-chart>
      <lightning-payment-history-table
        @payment-selected=${this._onPaymentSelected}>
      </lightning-payment-history-table>
    `;
  }

  private _renderStats() {
    if (!this._stats) return '';

    return html`
      <div class="stats-grid">
        <uui-box>
          <div class="stat-card">
            <span class="stat-label">Total Received</span>
            <span class="stat-value">${this._formatSats(this._stats.totalReceivedSat)}</span>
            <span class="stat-secondary">${this._formatBTC(this._stats.totalReceivedSat)} BTC</span>
          </div>
        </uui-box>

        <uui-box>
          <div class="stat-card">
            <span class="stat-label">Paid Payments</span>
            <span class="stat-value">${this._stats.paidCount}</span>
            <span class="stat-secondary">Successfully completed</span>
          </div>
        </uui-box>

        <uui-box>
          <div class="stat-card">
            <span class="stat-label">Pending Payments</span>
            <span class="stat-value">${this._stats.pendingCount}</span>
            <span class="stat-secondary">Awaiting confirmation</span>
          </div>
        </uui-box>

        <uui-box>
          <div class="stat-card">
            <span class="stat-label">Failed Payments</span>
            <span class="stat-value">${this._stats.failedCount}</span>
            <span class="stat-secondary">Unsuccessful attempts</span>
          </div>
        </uui-box>

        <uui-box>
          <div class="stat-card">
            <span class="stat-label">Last 24 Hours</span>
            <span class="stat-value">${this._stats.last24Hours.count}</span>
            <span class="stat-secondary">
              ${this._formatSats(this._stats.last24Hours.amountSat)} sats received
            </span>
          </div>
        </uui-box>
      </div>
    `;
  }

  private _onPaymentSelected(event: CustomEvent) {
    const { paymentHash } = event.detail;
    // Navigate to payment details page or show modal
    console.log('Payment selected:', paymentHash);
    // TODO: Implement navigation to payment details
  }
}

declare global {
  interface HTMLElementTagNameMap {
    'lightning-dashboard': LightningDashboardElement;
  }
}
