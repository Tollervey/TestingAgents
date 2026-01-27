import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  build: {
    lib: {
      entry: {
        'lightning-payments': resolve(__dirname, 'src/index.ts'),
        'dashboard/lightning-dashboard.element': resolve(__dirname, 'src/dashboard/lightning-dashboard.element.ts'),
        'dashboard/connection-status.element': resolve(__dirname, 'src/dashboard/connection-status.element.ts'),
        'dashboard/payment-chart.element': resolve(__dirname, 'src/dashboard/payment-chart.element.ts'),
        'dashboard/payment-history-table.element': resolve(__dirname, 'src/dashboard/payment-history-table.element.ts'),
      },
      formats: ['es'],
    },
    outDir: '../wwwroot/App_Plugins/LightningPayments',
    emptyOutDir: true,
    rollupOptions: {
      external: [/^@umbraco-cms/, 'lit', /^lit\//],
      output: {
        entryFileNames: '[name].js',
        chunkFileNames: 'chunks/[name]-[hash].js',
      },
    },
  },
});
