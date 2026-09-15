import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Logo } from '../shared/ui/logo';

/** Never rendered on /admin/* routes — see app.html/app.ts. */
@Component({
  selector: 'app-footer',
  imports: [RouterLink, Logo],
  template: `
    <footer class="site-footer">
      <div class="grid">
        <div class="brand-col">
          <app-logo variant="full" tone="white" class="brand-mark" />
          <p class="tagline">Airguns, pellets and accessories — matched so you know it'll work together.</p>
        </div>

        <div>
          <p class="eyebrow">Shop</p>
          <ul>
            <li><a routerLink="/airguns">Airguns</a></li>
            <li><a routerLink="/pellets">Pellets</a></li>
            <li><a routerLink="/accessories">Accessories</a></li>
            <li><a routerLink="/bundles">Bundles</a></li>
          </ul>
        </div>

        <div>
          <p class="eyebrow">Guidance</p>
          <ul>
            <li><a routerLink="/learn">Learning Centre</a></li>
            <li><a routerLink="/webinar">Live Webinar</a></li>
          </ul>
        </div>
      </div>

      <div class="legal">
        <span>© {{ year }} ONE77 Sports</span>
        <span>E-commerce powered by Shopify</span>
      </div>
    </footer>
  `,
  styles: [
    `
            .site-footer {
        border-top: 1px solid #2a2a2a;
        background: #0a0a0a;
margin-top: 0;
      }
      .grid {
        max-width: var(--container-width);
        margin-inline: auto;
        padding: var(--space-8) var(--container-padding-inline) var(--space-6);
        display: grid;
        grid-template-columns: 1fr;
        gap: var(--space-7);
      }
      @media (min-width: 720px) {
        .grid {
          grid-template-columns: 1.4fr 1fr 1fr;
        }
      }
      .brand-mark {
        height: 24px;
      }
      .tagline {
        margin: var(--space-4) 0 0;
        color: #999999;
        font-size: var(--font-size-sm);
        max-width: 32ch;
      }
      .eyebrow {
        color: #ffffff;
        font-weight: var(--font-weight-semibold);
        font-size: var(--font-size-sm);
      }
      ul {
        list-style: none;
        margin: var(--space-4) 0 0;
        padding: 0;
        display: flex;
        flex-direction: column;
        gap: var(--space-2);
      }
      a {
        text-decoration: none;
        color: #aaaaaa;
        font-size: var(--font-size-sm);
      }
      a:hover {
        color: #ffffff;
      }
      .legal {
        max-width: var(--container-width);
        margin-inline: auto;
        padding: var(--space-5) var(--container-padding-inline) var(--space-6);
        border-top: 1px solid #2a2a2a;
        display: flex;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: var(--space-2);
        color: #777777;
        font-size: var(--font-size-xs);
      }
    `,
  ],
})
export class Footer {
  protected readonly year = new Date().getFullYear();
}
