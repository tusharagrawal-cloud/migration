import { Component } from '@angular/core';
import { PageContainer } from '../../shared/ui/page-container';

@Component({
  selector: 'app-terms-page',
  imports: [PageContainer],
  template: `
    <app-page-container>
      <div class="policy-page">
        <p class="eyebrow">Legal</p>
        <h1>Terms &amp; Conditions</h1>
        <p class="updated">Last updated: [DATE]</p>

        <h2>1. Introduction</h2>
        <p>
          These Terms &amp; Conditions govern your use of the ONE77 Sports website and any purchase made through it.
          By accessing or using this site, you agree to be bound by these terms. ONE77 Sports is operated by
          [COMPANY LEGAL NAME].
        </p>

        <h2>2. Products &amp; Orders</h2>
        <p>
          All products listed on this site are subject to availability. We reserve the right to limit quantities,
          refuse or cancel any order at our discretion, including in cases of pricing errors, suspected fraud, or
          stock unavailability.
        </p>

        <h2>3. Pricing &amp; Payment</h2>
        <p>
          All prices are listed in INR and are inclusive of applicable taxes unless stated otherwise. Payment must be
          completed at the time of order through the available payment methods on the site.
        </p>

        <h2>4. Shipping</h2>
        <p>
          Delivery timelines provided at checkout are estimates and may vary due to courier delays, regional
          restrictions, or product availability. Certain products may be subject to regulatory shipping restrictions.
        </p>

        <h2>5. Age &amp; Eligibility</h2>
        <p>
          Airguns and related accessories are age-restricted products. By purchasing from this site, you confirm you
          meet the minimum legal age requirement applicable in your jurisdiction.
        </p>

        <h2>6. Limitation of Liability</h2>
        <p>
          ONE77 Sports is not liable for any indirect, incidental, or consequential damages arising from the use or
          misuse of products purchased through this site.
        </p>

        <h2>7. Changes to Terms</h2>
        <p>
          We reserve the right to update these Terms &amp; Conditions at any time. Continued use of the site after
          changes are posted constitutes acceptance of the revised terms.
        </p>

        <h2>8. Contact</h2>
        <p>For any questions regarding these terms, contact us at [PHONE NUMBER] or via the Free Q&amp;A on this site.</p>
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
      .updated {
        color: var(--color-text-faint);
        font-size: var(--font-size-sm);
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
    `,
  ],
})
export class TermsPage {}