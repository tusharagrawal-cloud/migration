// Production/default config. No hardcoded production URL, no credentials —
// deployment sets `apiBaseUrl` to wherever the real ONE77 API host is served
// from. Empty string means "same origin" (e.g. reverse-proxied under /api).
export const environment = {
  production: true,
  apiBaseUrl: '',
  shopifyStoreDomain: 'h6sw6j-md.myshopify.com',
  shopifyStorefrontToken: '8ecaf5407717ccc5801637d6ed59b51d',
  shopifyStorefrontApiVersion: '2024-10',
};
