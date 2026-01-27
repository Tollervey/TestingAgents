import { LitElement, html, css } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { LightningApiClient } from '../shared/lightning-api-client.js';
import type { ChartDataResponse, ChartDataPoint } from '../shared/types.js';

/**
 * Payment Chart Component
 * Displays payment volume over time with period selection
 */
@customElement('lightning-payment-chart')
export class PaymentChartElement extends LitElement {
  static styles = css`
    :host {
      display: block;
    }

    .chart-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: var(--uui-size-space-4);
    }

    .chart-container {
      min-height: 300px;
      padding: var(--uui-size-space-4);
    }

    .chart-bars {
      display: flex;
      align-items: flex-end;
      gap: var(--uui-size-space-2);
      height: 250px;
      padding: var(--uui-size-space-4) 0;
    }

    .chart-bar-container {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--uui-size-space-2);
      height: 100%;
      justify-content: flex-end;
    }

    .chart-bar {
      width: 100%;
      background: linear-gradient(180deg, var(--uui-color-selected) 0%, var(--uui-color-selected-emphasis) 100%);
      border-radius: var(--uui-border-radius) var(--uui-border-radius) 0 0;
      transition: opacity 0.2s;
      min-height: 4px;
      cursor: pointer;
      position: relative;
    }

    .chart-bar:hover {
      opacity: 0.8;
    }

    .chart-bar-label {
      font-size: 0.75rem;
      color: var(--uui-color-text-alt);
      text-align: center;
      white-space: nowrap;
    }

    .chart-bar-tooltip {
      position: absolute;
      bottom: 100%;
      left: 50%;
      transform: translateX(-50%);
      background: var(--uui-color-surface);
      padding: var(--uui-size-space-2);
      border-radius: var(--uui-border-radius);
      box-shadow: var(--uui-shadow-depth-3);
      white-space: nowrap;
      font-size: 0.875rem;
      display: none;
    }

    .chart-bar:hover .chart-bar-tooltip {
      display: block;
    }

    .empty-state {
      display: flex;
      align-items: center;
      justify-content: center;
      height: 250px;
      color: var(--uui-color-text-alt);
    }

    .error {
      color: var(--uui-color-danger);
      padding: var(--uui-size-space-4);
    }
  `;

  @state() private _chartData: ChartDataResponse | null = null;
  @state() private _period: 'day' | 'week' | 'month' = 'week';
  @state() private _loading = true;
  @state() private _error: string | null = null;

  async connectedCallback() {
    super.connectedCallback();
    await this._loadData();
  }

  private async _loadData() {
    this._loading = true;
    this._error = null;

    const result = await LightningApiClient.getChartData(this._period);

    if (result.ok) {
      this._chartData = result.data;
    } else {
      this._error = result.error.detail || result.error.title || 'Failed to load chart data';
    }

    this._loading = false;
  }

  private async _onPeriodChange(event: Event) {
    const select = event.target as HTMLSelectElement;
    this._period = select.value as 'day' | 'week' | 'month';
    await this._loadData();
  }

  private _formatDate(timestamp: string): string {
    const date = new Date(timestamp);
    switch (this._period) {
      case 'day':
        return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
      case 'week':
        return date.toLocaleDateString('en-US', { weekday: 'short' });
      case 'month':
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
      default:
        return date.toLocaleDateString();
    }
  }

  private _formatSats(sats: number): string {
    return `${sats.toLocaleString()} sats`;
  }

  private _getBarHeight(point: ChartDataPoint, maxAmount: number): number {
    if (maxAmount === 0) return 0;
    return Math.max(4, (point.amountSat / maxAmount) * 100);
  }

  render() {
    if (this._loading) {
      return html`<uui-loader></uui-loader>`;
    }

    if (this._error) {
      return html`<div class="error">${this._error}</div>`;
    }

    return html`
      <uui-box headline="Payment Volume">
        <div class="chart-container">
          <div class="chart-header">
            <uui-select
              .value=${this._period}
              @change=${this._onPeriodChange}
              label="Time Period">
              <uui-select-option value="day">Last 24 Hours</uui-select-option>
              <uui-select-option value="week">Last Week</uui-select-option>
              <uui-select-option value="month">Last Month</uui-select-option>
            </uui-select>
          </div>
          ${this._renderChart()}
        </div>
      </uui-box>
    `;
  }

  private _renderChart() {
    if (!this._chartData || this._chartData.dataPoints.length === 0) {
      return html`<div class="empty-state">No payment data available</div>`;
    }

    const maxAmount = Math.max(...this._chartData.dataPoints.map(p => p.amountSat));

    return html`
      <div class="chart-bars">
        ${this._chartData.dataPoints.map(point => html`
          <div class="chart-bar-container">
            <div
              class="chart-bar"
              style="height: ${this._getBarHeight(point, maxAmount)}%">
              <div class="chart-bar-tooltip">
                ${this._formatSats(point.amountSat)}<br>
                ${point.count} payment${point.count !== 1 ? 's' : ''}
              </div>
            </div>
            <span class="chart-bar-label">${this._formatDate(point.timestamp)}</span>
          </div>
        `)}
      </div>
    `;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    'lightning-payment-chart': PaymentChartElement;
  }
}
