import { graniteEvent } from '@apps-in-toss/web-framework';

// The native navigation bar sends a Toss event rather than browser popstate.
if (window.ReactNativeWebView) {
  graniteEvent.addEventListener('backEvent', {
    onEvent: () => window.paperShotUnity?.SendMessage('WebGLPlatform', 'OnBrowserBack', ''),
    onError: (error) => console.warn('Toss back event unavailable', error),
  });
}
