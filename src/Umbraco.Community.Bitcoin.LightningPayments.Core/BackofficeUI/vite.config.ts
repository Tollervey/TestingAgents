import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  build: {
    lib: {
      entry: resolve(__dirname, 'src/index.ts'),
      formats: ['es'],
    },
    outDir: '../wwwroot/App_Plugins/LightningPayments',
    emptyOutDir: true,
    rollupOptions: {
      external: [/^@umbraco-cms/],
      output: {
        entryFileNames: 'lightning-payments.js',
      },
    },
  },
});
