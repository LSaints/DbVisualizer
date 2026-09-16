/// <reference types="vitest" />
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// O Vite roda em http://localhost:5173 e encaminha /api para a API ASP.NET
// Core em http://localhost:5000 (ver quickstart.md).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true,
    proxy: {
      '/api': 'http://localhost:5000'
    }
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './testes/configuracaoSetup.ts',
    css: false
  }
});