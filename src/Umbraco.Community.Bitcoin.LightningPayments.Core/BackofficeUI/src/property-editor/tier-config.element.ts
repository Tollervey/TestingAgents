import { LitElement, html, css } from 'lit';
import { customElement, property } from 'lit/decorators.js';

/**
 * Tier pricing configuration item
 */
export interface TierPrice {
  tier: string;
  label: string;
  amount: number;
}

/**
 * Tier Configuration Component
 * Allows editing of individual tier prices for paywall access
 */
@customElement('tier-config')
export class TierConfigElement extends LitElement {
  static styles = css`
    :host {
      display: block;
    }

    .tier-row {
      display: grid;
      grid-template-columns: 80px 1fr 80px;
      gap: var(--uui-size-space-3);
      align-items: center;
      padding: var(--uui-size-space-2) 0;
      border-bottom: 1px solid var(--uui-color-border);
    }

    .tier-row:last-child {
      border-bottom: none;
    }

    .tier-label {
      font-weight: 500;
      color: var(--uui-color-text);
    }

    .tier-description {
      font-size: 0.875rem;
      color: var(--uui-color-text-alt);
    }

    .tier-input {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-2);
    }

    .sats-label {
      font-size: 0.75rem;
      color: var(--uui-color-text-alt);
      white-space: nowrap;
    }

    .error {
      color: var(--uui-color-danger);
      font-size: 0.75rem;
      margin-top: var(--uui-size-space-1);
    }

    uui-input {
      width: 100%;
      max-width: 150px;
    }
  `;

  /**
   * Map of tier keys to satoshi amounts
   */
  @property({ type: Object })
  tierPrices: Record<string, number> = {};

  /**
   * Whether the inputs should be disabled
   */
  @property({ type: Boolean })
  disabled = false;

  private readonly _tiers: Array<{ tier: string; label: string; description: string }> = [
    { tier: '1h', label: '1 Hour', description: 'Short-term access' },
    { tier: '8h', label: '8 Hours', description: 'Half-day access' },
    { tier: '24h', label: '24 Hours', description: 'Full day access' },
    { tier: '7d', label: '7 Days', description: 'Weekly access' },
  ];

  private _handleAmountChange(tier: string, event: Event) {
    const input = event.target as HTMLInputElement;
    const value = parseInt(input.value, 10);

    // Validate
    if (isNaN(value) || value < 0) {
      return;
    }

    // Create new prices object with updated value
    const newPrices = { ...this.tierPrices };
    if (value === 0) {
      delete newPrices[tier];
    } else {
      newPrices[tier] = value;
    }

    // Dispatch change event
    this.dispatchEvent(
      new CustomEvent('tier-prices-change', {
        detail: { tierPrices: newPrices },
        bubbles: true,
        composed: true,
      })
    );
  }

  render() {
    return html`
      <div class="tier-container">
        ${this._tiers.map(
          ({ tier, label, description }) => html`
            <div class="tier-row">
              <div>
                <div class="tier-label">${label}</div>
                <div class="tier-description">${description}</div>
              </div>
              <div class="tier-input">
                <uui-input
                  type="number"
                  min="0"
                  step="1"
                  placeholder="0"
                  .value=${String(this.tierPrices[tier] ?? '')}
                  ?disabled=${this.disabled}
                  @input=${(e: Event) => this._handleAmountChange(tier, e)}
                  label="${label} amount">
                </uui-input>
                <span class="sats-label">sats</span>
              </div>
              <div>
                ${this.tierPrices[tier] ? this._formatAsBTC(this.tierPrices[tier]) : ''}
              </div>
            </div>
          `
        )}
      </div>
    `;
  }

  private _formatAsBTC(sats: number): string {
    if (sats === 0) return '';
    const btc = sats / 100_000_000;
    if (btc < 0.00001) {
      return `${sats} sats`;
    }
    return `${btc.toFixed(8)} BTC`;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    'tier-config': TierConfigElement;
  }
}
