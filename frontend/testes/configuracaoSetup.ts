import '@testing-library/jest-dom/vitest';

// Polyfill necessário para o React Flow renderizar em jsdom (medição de
// dimensões dos nodos via ResizeObserver).
class ResizeObserverFalso implements ResizeObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
}

if (typeof globalThis.ResizeObserver === 'undefined') {
  Object.defineProperty(globalThis, 'ResizeObserver', {
    value: ResizeObserverFalso,
    writable: true,
    configurable: true
  });
}