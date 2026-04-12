import { defineConfig } from 'astro/config';
import react from '@astrojs/react';

export default defineConfig({
  integrations: [react()],
  output: 'static',
  vite: {
    // Evita que Vite rompa las URLs de los íconos de Leaflet
    optimizeDeps: {
      include: ['leaflet'],
    },
  },
});
