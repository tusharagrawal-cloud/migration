import { Component, computed, input } from '@angular/core';

type LogoVariant = 'full' | 'symbol' | 'numeric';
type LogoTone = 'black' | 'white';

/**
 * The genuine ONE77 Sports brand mark — official assets carried over from
 * the legacy storefront's branding pass (frontend/src/assets/brand/,
 * frontend/src/components/BrandLogo.jsx), not redrawn. `variant` picks the
 * artwork (full lockup / standalone symbol / brand numeric), `tone` picks
 * the colorway for the surface it sits on. Width is left to the browser so
 * the artwork's own aspect ratio is never distorted — control size via the
 * host element's height.
 */
@Component({
  selector: 'app-logo',
  template: `<img [src]="src()" [alt]="alt()" />`,
  styles: [
    `
      :host {
        display: inline-flex;
        align-items: center;
      }
      img {
        height: 100%;
        width: auto;
        display: block;
      }
    `,
  ],
})
export class Logo {
  variant = input<LogoVariant>('full');
  tone = input<LogoTone>('white');
  alt = input('ONE77 Sports');

protected src = computed(() => `brand/one77-${this.assetName(this.variant())}-${this.tone()}.png`);
  private assetName(variant: LogoVariant): string {
    return variant === 'full' ? 'sports-lockup' : variant;
  }
}
