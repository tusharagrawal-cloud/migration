import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageContainer } from '../../shared/ui/page-container';

@Component({
  selector: 'app-about-page',
  imports: [PageContainer, RouterLink],
  template: `
    <app-page-container>
      <div class="policy-page">
        <p class="eyebrow">Our Story</p>
        <h1>About ONE77 Sports</h1>
        <p class="lede">Airguns, pellets and accessories — matched by our team so you know it'll work together before you buy.</p>

        <h2>Who We Are</h2>
        <p>
          ONE77 Sports exists because getting an airgun setup right shouldn't require guesswork. We're a curated
          destination for airguns, pellets, and accessories — every listing here is chosen and checked, not just
          dropped into a generic catalogue.
        </p>

        <h2>What We Do</h2>
        <ul class="pillars">
          <li>
            <span class="pillar-title">Expert-led selection</span>
            <span class="pillar-body">Every product we list is reviewed by our team before it goes on the site.</span>
          </li>
          <li>
            <span class="pillar-title">Matched recommendations</span>
            <span class="pillar-body">Right calibre, weight, and use case — so pellets and accessories actually fit what you're shooting.</span>
          </li>
          <li>
            <span class="pillar-title">Curated bundles</span>
            <span class="pillar-body">No noise, no filler — only kit combinations worth owning.</span>
          </li>
          <li>
            <span class="pillar-title">Live human help</span>
            <span class="pillar-body">Real questions get real answers through our Free Q&amp;A.</span>
          </li>
        </ul>

        <h2>Who We Help</h2>
        <p>
          Whether you're picking up your first airgun, chasing tighter groups on the range, plinking in the
          backyard, or upgrading a setup you already own — we build our recommendations around what you're actually
          trying to do, not just what's in stock.
        </p>

        <h2>Our Promise</h2>
        <p>
          If something doesn't fit or work as expected, we want to know. Every product is checked for compatibility
          and quality before it's listed, and our team is a message away if you're ever unsure what will work with
          your setup.
        </p>

        <h2>Get in Touch</h2>
        <p>
          Have a question about a product, an order, or just want advice on what to buy?
          <a routerLink="/webinar">Reserve a free Q&amp;A seat</a> or reach us at [PHONE NUMBER] — we're happy to
          help.
        </p>
      </div>
    </app-page-container>
  `,
  styles: [
    `
      .policy-page {
        max-width: 72ch;
        margin: 0 auto;
        padding-block: var(--space-8);
        color: var(--color-text);
      }
      .eyebrow {
        font-size: 0.75rem;
        letter-spacing: 0.22em;
        text-transform: uppercase;
        font-weight: var(--font-weight-bold);
        color: var(--color-accent);
        margin: 0 0 var(--space-3);
      }
      h1 {
        font-family: var(--font-family-heading);
        font-size: var(--font-size-2xl);
        margin: 0 0 var(--space-2);
      }
      .lede {
        color: var(--color-text-muted);
        font-size: var(--font-size-lg);
        line-height: var(--line-height-normal);
        margin: 0 0 var(--space-7);
      }
      h2 {
        font-family: var(--font-family-heading);
        font-size: var(--font-size-lg);
        margin: var(--space-7) 0 var(--space-3);
      }
      p {
        color: var(--color-text-muted);
        line-height: var(--line-height-normal);
        margin: 0 0 var(--space-4);
      }
      p a {
        color: var(--color-accent);
      }
      .pillars {
        list-style: none;
        margin: 0 0 var(--space-4);
        padding: 0;
        display: grid;
        gap: var(--space-4);
      }
      .pillars li {
        display: flex;
        flex-direction: column;
        gap: 2px;
        padding-left: var(--space-4);
        border-left: 2px solid var(--color-accent);
      }
      .pillar-title {
        font-family: var(--font-family-heading);
        font-weight: var(--font-weight-semibold);
        color: var(--color-text);
      }
      .pillar-body {
        color: var(--color-text-muted);
        font-size: var(--font-size-sm);
        line-height: var(--line-height-normal);
      }
    `,
  ],
})
export class AboutPage {}