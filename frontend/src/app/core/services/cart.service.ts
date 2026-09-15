import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, firstValueFrom, of } from 'rxjs';
import { environment } from '../../../environments/environment';

/** A single line item as rendered in the cart drawer. */
export interface CartLine {
  id: string; // opaque CartLine GID
  variantId: string; // opaque ProductVariant GID
  productTitle: string;
  variantTitle: string;
  imageUrl: string | null;
  quantity: number;
  lineTotalLabel: string | null;
}

export interface CartState {
  id: string; // opaque Cart GID
  checkoutUrl: string;
  lines: CartLine[];
  totalQuantity: number;
  subtotalLabel: string | null;
}

const CART_ID_STORAGE_KEY = 'one77_shopify_cart_id';

/**
 * Cart/checkout via Shopify's Storefront Cart API. Per
 * SHOPIFY_V1_CONTRACT.md: "Shopify owns... cart, checkout, orders, the
 * commerce lifecycle" — this service never builds a custom checkout UI or
 * handles payment details itself. Adding to cart returns a `checkoutUrl`
 * that points at Shopify's own hosted checkout; "checking out" from ONE77
 * means redirecting the browser there, nothing more.
 *
 * The cart id is persisted in localStorage (not sessionStorage) so a
 * customer's cart survives closing the tab, matching standard storefront
 * behaviour. If the stored cart id is stale/expired, Shopify's response
 * simply comes back empty/erroring and a new cart is created transparently.
 *
 * If Shopify isn't configured (see ShopifyProductService's isConfigured
 * flag), every method resolves to a harmless no-op so the storefront still
 * renders without crashing in fixture-only dev mode.
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);

  private readonly endpoint = environment.shopifyStoreDomain
    ? `https://${environment.shopifyStoreDomain}/api/${environment.shopifyStorefrontApiVersion}/graphql.json`
    : null;

  private readonly isConfigured = !!(environment.shopifyStoreDomain && environment.shopifyStorefrontToken);

  private readonly _cart = signal<CartState | null>(null);
  private readonly _isOpen = signal(false);
  private readonly _isLoading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly cart = this._cart.asReadonly();
  readonly isOpen = this._isOpen.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly itemCount = computed(() => this._cart()?.totalQuantity ?? 0);

  constructor() {
    if (this.isConfigured) {
      const storedId = localStorage.getItem(CART_ID_STORAGE_KEY);
      if (storedId) {
        this.fetchCart(storedId);
      }
    }
  }

  open(): void {
    this._isOpen.set(true);
  }

  close(): void {
    this._isOpen.set(false);
  }

  /** Adds `quantity` of a variant to the cart, creating the cart if none exists yet. Opens the drawer on success. */
  async addToCart(variantId: string, quantity: number): Promise<void> {
    if (!this.isConfigured) {
      this._error.set('Shopify is not configured yet.');
      return;
    }
    this._isLoading.set(true);
    this._error.set(null);
    try {
      const existingId = this._cart()?.id ?? localStorage.getItem(CART_ID_STORAGE_KEY);
      const cart = existingId
        ? await this.linesAdd(existingId, variantId, quantity)
        : await this.createCart(variantId, quantity);
      this.applyCart(cart);
      this.open();
    } catch {
      this._error.set("Couldn't update the cart. Please try again.");
    } finally {
      this._isLoading.set(false);
    }
  }

  async updateQuantity(lineId: string, quantity: number): Promise<void> {
    const cartId = this._cart()?.id;
    if (!cartId) return;
    this._isLoading.set(true);
    this._error.set(null);
    try {
      const cart = await this.graphql<{ cartLinesUpdate: { cart: RawCart } }>(CART_LINES_UPDATE_MUTATION, {
        cartId,
        lines: [{ id: lineId, quantity }],
      }).then((d) => d.cartLinesUpdate.cart);
      this.applyCart(cart);
    } catch {
      this._error.set("Couldn't update the cart. Please try again.");
    } finally {
      this._isLoading.set(false);
    }
  }

  async removeLine(lineId: string): Promise<void> {
    const cartId = this._cart()?.id;
    if (!cartId) return;
    this._isLoading.set(true);
    this._error.set(null);
    try {
      const cart = await this.graphql<{ cartLinesRemove: { cart: RawCart } }>(CART_LINES_REMOVE_MUTATION, {
        cartId,
        lineIds: [lineId],
      }).then((d) => d.cartLinesRemove.cart);
      this.applyCart(cart);
    } catch {
      this._error.set("Couldn't update the cart. Please try again.");
    } finally {
      this._isLoading.set(false);
    }
  }

  /** Sends the browser to Shopify's own hosted checkout — payment is never handled by ONE77 code. */
  goToCheckout(): void {
    const url = this._cart()?.checkoutUrl;
    if (url) {
      window.location.href = url;
    }
  }

  private async createCart(variantId: string, quantity: number): Promise<RawCart> {
    const data = await this.graphql<{ cartCreate: { cart: RawCart } }>(CART_CREATE_MUTATION, {
      input: { lines: [{ merchandiseId: variantId, quantity }] },
    });
    return data.cartCreate.cart;
  }

  private async linesAdd(cartId: string, variantId: string, quantity: number): Promise<RawCart> {
    const data = await this.graphql<{ cartLinesAdd: { cart: RawCart | null; userErrors: { message: string }[] } }>(
      CART_LINES_ADD_MUTATION,
      { cartId, lines: [{ merchandiseId: variantId, quantity }] },
    );
    if (!data.cartLinesAdd.cart || data.cartLinesAdd.userErrors.length) {
      // Stored cart id was likely stale/expired on Shopify's side — start a fresh cart instead of failing silently.
      return this.createCart(variantId, quantity);
    }
    return data.cartLinesAdd.cart;
  }

  private fetchCart(cartId: string): void {
    this.graphql<{ cart: RawCart | null }>(CART_QUERY, { cartId })
      .then((data) => {
        if (data.cart) this.applyCart(data.cart);
        else localStorage.removeItem(CART_ID_STORAGE_KEY);
      })
      .catch(() => {
        /* stale cart id on load — ignore, a new cart is created on next Add to Cart */
      });
  }

  private applyCart(raw: RawCart): void {
    localStorage.setItem(CART_ID_STORAGE_KEY, raw.id);
    this._cart.set({
      id: raw.id,
      checkoutUrl: raw.checkoutUrl,
      totalQuantity: raw.totalQuantity,
      subtotalLabel: raw.cost?.subtotalAmount
        ? formatPrice(raw.cost.subtotalAmount.amount, raw.cost.subtotalAmount.currencyCode)
        : null,
      lines: raw.lines.edges.map((e) => ({
        id: e.node.id,
        variantId: e.node.merchandise.id,
        productTitle: e.node.merchandise.product.title,
        variantTitle: e.node.merchandise.title,
        imageUrl: e.node.merchandise.image?.url ?? null,
        quantity: e.node.quantity,
        lineTotalLabel: e.node.cost?.totalAmount
          ? formatPrice(e.node.cost.totalAmount.amount, e.node.cost.totalAmount.currencyCode)
          : null,
      })),
    });
  }

  private async graphql<T>(query: string, variables: Record<string, unknown>): Promise<T> {
    const result = await firstValueFrom(
      this.http
        .post<{ data: T; errors?: { message: string }[] }>(
          this.endpoint!,
          { query, variables },
          {
            headers: {
              'Content-Type': 'application/json',
              'X-Shopify-Storefront-Access-Token': environment.shopifyStorefrontToken,
            },
          },
        )
        .pipe(catchError(() => of(null))),
    );
    if (!result || result.errors?.length) {
      throw new Error(result?.errors?.map((e) => e.message).join('; ') ?? 'Cart request failed');
    }
    return result.data;
  }
}

// ---- Storefront API cart response shape (subset actually used) -----------

interface RawCart {
  id: string;
  checkoutUrl: string;
  totalQuantity: number;
  cost: { subtotalAmount: { amount: string; currencyCode: string } | null } | null;
  lines: {
    edges: {
      node: {
        id: string;
        quantity: number;
        cost: { totalAmount: { amount: string; currencyCode: string } | null } | null;
        merchandise: {
          id: string;
          title: string;
          image: { url: string } | null;
          product: { title: string };
        };
      };
    }[];
  };
}

function formatPrice(amount: string, currencyCode: string): string {
  const value = Number(amount);
  if (Number.isNaN(value)) return amount;
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency: currencyCode }).format(value);
  } catch {
    return `${amount} ${currencyCode}`;
  }
}

const CART_FIELDS = `
  id
  checkoutUrl
  totalQuantity
  cost { subtotalAmount { amount currencyCode } }
  lines(first: 50) {
    edges {
      node {
        id
        quantity
        cost { totalAmount { amount currencyCode } }
        merchandise {
          ... on ProductVariant {
            id
            title
            image { url }
            product { title }
          }
        }
      }
    }
  }
`;

const CART_CREATE_MUTATION = `
  mutation CartCreate($input: CartInput!) {
    cartCreate(input: $input) {
      cart { ${CART_FIELDS} }
      userErrors { message }
    }
  }
`;

const CART_LINES_ADD_MUTATION = `
  mutation CartLinesAdd($cartId: ID!, $lines: [CartLineInput!]!) {
    cartLinesAdd(cartId: $cartId, lines: $lines) {
      cart { ${CART_FIELDS} }
      userErrors { message }
    }
  }
`;

const CART_LINES_UPDATE_MUTATION = `
  mutation CartLinesUpdate($cartId: ID!, $lines: [CartLineUpdateInput!]!) {
    cartLinesUpdate(cartId: $cartId, lines: $lines) {
      cart { ${CART_FIELDS} }
      userErrors { message }
    }
  }
`;

const CART_LINES_REMOVE_MUTATION = `
  mutation CartLinesRemove($cartId: ID!, $lineIds: [ID!]!) {
    cartLinesRemove(cartId: $cartId, lineIds: $lineIds) {
      cart { ${CART_FIELDS} }
      userErrors { message }
    }
  }
`;

const CART_QUERY = `
  query CartQuery($cartId: ID!) {
    cart(id: $cartId) { ${CART_FIELDS} }
  }
`;