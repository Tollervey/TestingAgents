import { LitElement, html, css } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { LightningApiClient } from '../shared/lightning-api-client.js';
import type { WalletBalanceResponse, WalletLimitsResponse } from '../shared/types.js';

/**
 * Connection Status Component
 * Displays SDK connection status, wallet balance, and transaction limits
 */
@customElement('lightning-connection-status')
export class ConnectionStatusElement extends LitElement {
  static styles = css`
    :host {
      display: block;
    }

    .status-container {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-4);
    }

    .status-header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
    }

    .status-indicator {
      width: 12px;
      height: 12px;
      border-radius: 50%;
      background-color: var(--uui-color-danger);
    }

    .status-indicator.connected {
      background-color: var(--uui-color-positive);
    }

    .balance-section {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: var(--uui-size-space-4);
    }

    .balance-item {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-1);
    }

    .balance-label {
      font-size: 0.875rem;
      color: var(--uui-color-text-alt);
    }

    .balance-value {
      font-size: 1.25rem;
      font-weight: 600;
    }

    .limits-section {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: var(--uui-size-space-4);
    }

    .limit-item {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-2);
    }

    .limit-title {
      font-weight: 600;
      color: var(--uui-color-text);
    }

    .limit-range {
      font-size: 0.875rem;
      color: var(--uui-color-text-alt);
    }

    .error {
      color: var(--uui-color-danger);
      padding: var(--uui-size-space-4);
    }
  `;

  @state() private _balance: WalletBalanceResponse | null = null;
  @state() private _limits: WalletLimitsResponse | null = null;
  @state() private _loading = true;
  @state() private _error: string | null = null;
  @state() private _connected = false;

  async connectedCallback() {
    super.connectedCallback();
    await this._loadData();
  }

  private async _loadData() {
    this._loading = true;
    this._error = null;

    try {
      const [balanceResult, limitsResult] = await Promise.all([
        LightningApiClient.getWalletBalance(),
        LightningApiClient.getWalletLimits(),
      ]);

      if (balanceResult.ok) {
        this._balance = balanceResult.data;
        this._connected = true;
      } else {
        this._error = balanceResult.error.detail || balanceResult.error.title || 'Failed to load balance';
        this._connected = false;
      }

      if (limitsResult.ok) {
        this._limits = limitsResult.data;
      } else if (!this._error) {
        this._error = limitsResult.error.detail || limitsResult.error.title || 'Failed to load limits';
      }
    } catch (err) {
      this._error = err instanceof Error ? err.message : 'Unknown error';
      this._connected = false;
    } finally {
      this._loading = false;
    }
  }

  private _formatSats(sats: number): string {
    return `${sats.toLocaleString()} sats`;
  }

  render() {
    if (this._loading) {
      return html`<uui-loader></uui-loader>`;
    }

    if (this._error) {
      return html`<div class="error">${this._error}</div>`;
    }

    return html`
      <uui-box>
        <div slot="headline" class="status-header">
          <span class="status-indicator ${this._connected ? 'connected' : ''}"></span>
          <span>${this._connected ? 'Connected' : 'Disconnected'}</span>
        </div>

        <div class="status-container">
          ${this._balance ? this._renderBalance() : ''}
          ${this._limits ? this._renderLimits() : ''}
        </div>
      </uui-box>
    `;
  }

  private _renderBalance() {
    if (!this._balance) return '';

    return html`
      <div class="balance-section">
        <div class="balance-item">
          <span class="balance-label">Balance</span>
          <span class="balance-value">${this._formatSats(this._balance.balanceSat)}</span>
        </div>
        <div class="balance-item">
          <span class="balance-label">Pending Receive</span>
          <span class="balance-value">${this._formatSats(this._balance.pendingReceiveSat)}</span>
        </div>
        <div class="balance-item">
          <span class="balance-label">Pending Send</span>
          <span class="balance-value">${this._formatSats(this._balance.pendingSendSat)}</span>
        </div>
      </div>
    `;
  }

  private _renderLimits() {
    if (!this._limits) return '';

    return html`
      <div class="limits-section">
        <div class="limit-item">
          <span class="limit-title">Receive Limits</span>
          <span class="limit-range">
            ${this._formatSats(this._limits.receive.minSat)} - ${this._formatSats(this._limits.receive.maxSat)}
          </span>
        </div>
        <div class="limit-item">
          <span class="limit-title">Send Limits</span>
          <span class="limit-range">
            ${this._formatSats(this._limits.send.minSat)} - ${this._formatSats(this._limits.send.maxSat)}
          </span>
        </div>
      </div>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    'lightning-connection-status': ConnectionStatusElement;
  }
}
