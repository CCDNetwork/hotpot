import { defineConfig, Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import checker from 'vite-plugin-checker';
import path from 'path';

// Plugin to remove CSP meta tag in development mode
// This allows Vite's HMR scripts to work without manual index.html editing
const removeCSPInDev = (): Plugin => ({
  name: 'remove-csp-in-dev',
  transformIndexHtml(html, ctx) {
    if (ctx.server) {
      // In dev mode, remove the CSP meta tag that blocks inline scripts
      return html.replace(
        /<meta http-equiv="Content-Security-Policy"[^>]*>/,
        '<!-- CSP disabled in development mode -->'
      );
    }
    return html;
  },
});

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    react(),
    checker({
      typescript: true,
    }),
    removeCSPInDev(),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    open: true,
    port: 3000,
    headers: {
      'Strict-Transport-Security': 'max-age=86400; includeSubDomains', // Adds HSTS options to your website, with a expiry time of 1 day
      'X-Content-Type-Options': 'nosniff', // Protects from improper scripts runnings
      'X-Frame-Options': 'DENY', // Stops your site being used as an iframe
      'X-XSS-Protection': '1; mode=block', // Gives XSS protection to legacy browsers
      'Content-Security-Policy': 'upgrade-insecure-requests',
    },
  },
});
