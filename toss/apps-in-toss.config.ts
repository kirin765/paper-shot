import { defineConfig } from '@apps-in-toss/web-framework/config';

export default defineConfig({
  appName: 'paper-shot',
  // TODO: set the brand color for the Paper Shot app.
  brand: { primaryColor: '#eae6dd' },
  permissions: [],
  webView: {
    allowsInlineMediaPlayback: true,
    mediaPlaybackRequiresUserAction: true,
    overScrollMode: 'never',
  },
  webBundleDir: 'dist',
});
