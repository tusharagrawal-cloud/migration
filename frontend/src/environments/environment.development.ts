// Local development config only — points at the temporary One77.Api.DevHost
// (see migration/backend). Never used in a production build.
export const environment = {
  production: false,
apiBaseUrl: 'http://localhost:5059',
  shopifyStoreDomain: 'h6sw6j-md.myshopify.com',
  shopifyStorefrontToken: '8ecaf5407717ccc5801637d6ed59b51d', // Storefront API access token, NOT an Admin API token — get this from Shopify Admin → Settings → Apps and sales channels → Develop apps, then paste it in here (never commit the real value to git)
  shopifyStorefrontApiVersion: '2024-10',
};
