import { LitElement, html, css, PropertyValues } from 'lit';
import { customElement, property, state } from 'lit/decorators.js';
import { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/extension-registry';
import './tier-config.element.js';

/**
 * Paywall configuration stored in the property editor
 */
export interface PaywallConfig {
  enabled: boolean;
  tierPrices: Record<string, number>;
  description?: string;
}

/**
 * Paywall Property Editor Element
 * Allows content editors to configure paywall settings with tiered pricing
 */
@customElement('paywall-editor')
export class PaywallEditorElement extends LitElement implements UmbPropertyEditorUiElement {
  static styles = css`
    :host {
      display: block;
    }

    .paywall-container {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-4);
    }

    .toggle-row {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
    }

    .toggle-label {
      font-weight: 500;
    }

    .section {
      padding: var(--uui-size-space-4);
      background-color: var(--uui-color-surface);
      border: 1px solid var(--uui-color-border);
      border-radius: var(--uui-border-radius);
    }

    .section-header {
      font-size: 0.875rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      color: var(--uui-color-text-alt);
      margin-bottom: var(--uui-size-space-3);
    }

    .description-field {
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-2);
    }

    .description-label {
      font-size: 0.875rem;
      color: var(--uui-color-text-alt);
    }

    .help-text {
      font-size: 0.75rem;
      color: var(--uui-color-text-alt);
      font-style: italic;
    }

    .disabled-overlay {
      opacity: 0.5;
      pointer-events: none;
    }

    uui-textarea {
      width: 100%;
    }
  `;

  /**
   * The current value of the property editor
   */
  @property({ type: Object })
  value: PaywallConfig = {
    enabled: false,
    tierPrices: {},
    description: ''
  };

  @state()
  private _config: PaywallConfig = {
    enabled: false,
    tierPrices: {},
    description: ''
  };

  protected firstUpdated(_changedProperties: PropertyValues) {
    super.firstUpdated(_changedProperties);
    // Initialize internal state from value
    if (this.value) {
      this._config = { ...this.value };
    }
  }

  protected updated(changedProperties: PropertyValues) {
    super.updated(changedProperties);
    // Sync external value changes to internal state
    if (changedProperties.has('value') && this.value) {
      this._config = { ...this.value };
    }
  }

  private _handleEnabledChange(event: Event) {
    const toggle = event.target as HTMLInputElement;
    this._updateConfig({ enabled: toggle.checked });
  }

  private _handleDescriptionChange(event: Event) {
    const textarea = event.target as HTMLTextAreaElement;
    this._updateConfig({ description: textarea.value });
  }

  private _handleTierPricesChange(event: CustomEvent<{ tierPrices: Record<string, number> }>) {
    this._updateConfig({ tierPrices: event.detail.tierPrices });
  }

  private _updateConfig(updates: Partial<PaywallConfig>) {
    this._config = { ...this._config, ...updates };

    // Dispatch the change event for Umbraco property editor
    this.dispatchEvent(
      new CustomEvent('property-value-change', {
        detail: { value: this._config },
        bubbles: true,
        composed: true
      })
    );
  }

  render() {
    return html`
      <div class="paywall-container">
        <div class="toggle-row">
          <uui-toggle
            .checked=${this._config.enabled}
            @change=${this._handleEnabledChange}
            label="Enable paywall">
          </uui-toggle>
          <span class="toggle-label">Enable Paywall</span>
        </div>

        <div class="section ${!this._config.enabled ? 'disabled-overlay' : ''}">
          <div class="section-header">Tier Pricing</div>
          <p class="help-text">
            Set prices in satoshis for each access tier. Leave blank to disable a tier.
          </p>
          <tier-config
            .tierPrices=${this._config.tierPrices}
            ?disabled=${!this._config.enabled}
            @tier-prices-change=${this._handleTierPricesChange}>
          </tier-config>
        </div>

        <div class="section ${!this._config.enabled ? 'disabled-overlay' : ''}">
          <div class="description-field">
            <label class="description-label" for="paywall-description">
              Paywall Message (optional)
            </label>
            <uui-textarea
              id="paywall-description"
              placeholder="Enter a message to display to users before payment..."
              .value=${this._config.description ?? ''}
              ?disabled=${!this._config.enabled}
              @input=${this._handleDescriptionChange}
              rows="3">
            </uui-textarea>
            <span class="help-text">
              This message will be shown to users when they encounter the paywall.
            </span>
          </div>
        </div>

        ${this._renderSummary()}
      </div>
    `;
  }

  private _renderSummary() {
    if (!this._config.enabled) {
      return html`
        <div class="section">
          <p class="help-text">Paywall is disabled. Content will be freely accessible.</p>
        </div>
      `;
    }

    const configuredTiers = Object.entries(this._config.tierPrices)
      .filter(([_, amount]) => amount > 0)
      .map(([tier, amount]) => `${tier}: ${amount.toLocaleString()} sats`);

    if (configuredTiers.length === 0) {
      return html`
        <div class="section">
          <p class="help-text" style="color: var(--uui-color-warning);">
            No tiers configured. Please set at least one tier price to enable the paywall.
          </p>
        </div>
      `;
    }

    return html`
      <div class="section">
        <div class="section-header">Summary</div>
        <p>Configured tiers: ${configuredTiers.join(', ')}</p>
      </div>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    'paywall-editor': PaywallEditorElement;
  }
}
