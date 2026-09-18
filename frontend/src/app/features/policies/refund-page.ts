import { Component } from '@angular/core';
import { PageContainer } from '../../shared/ui/page-container';

@Component({
  selector: 'app-refund-page',
  imports: [PageContainer],
  template: `
    <app-page-container>
      <div class="policy-page">
        <p class="eyebrow">Legal</p>
        <h1>Refund &amp; Return Policy</h1>
        <p class="updated">Last updated: [DATE]</p>

        <h2>1. Overview</h2>
        <p>
          We want you to be happy with your purchase from ONE77 Sports. This policy explains when and how you can
          request a return, exchange, or refund.
        </p>

        <h2>2. Eligibility for Returns</h2>
        <p>
          Products may be eligible for return within [X] days of delivery if they are unused, in their original
          packaging, and accompanied by proof of purchase. Certain items — including used or damaged pellets,
          consumables, and any item explicitly marked as non-returnable — are not eligible for return.
        </p>

        <h2>3. Damaged or Defective Items</h2>
        <p>
          If you receive a damaged or defective product, please contact us within [X] days of delivery with photos
          of the issue so we can arrange a replacement or refund.
        </p>

        <h2>4. Return Process</h2>
        <p>
          To initiate a return, contact us via the Free Q&amp;A on this site or at [PHONE NUMBER] with your order
          number and reason for return. We will guide you through the pickup or shipping process.
        </p>

        <h2>5. Refunds</h2>
        <p>
          Once a returned item is received and inspected, we will notify you of the approval or rejection of your
          refund. Approved refunds are processed to your original payment method within [X] business days.
        </p>

        <h2>6. Shipping Costs</h2>
        <p>
          Original shipping charges are non-refundable unless the return is due to our error (e.g. wrong or
          defective item). Return shipping costs, where applicable, are the customer's responsibility.
        </p>

        <h2>7. Cancellations</h2>
        <p>
          Orders can be cancelled before they are shipped by contacting us as soon as possible. Once an order has
          shipped, it must go through the standard return process instead.
        </p>

        <h2>8. Contact</h2>
        <p>For any questions regarding refunds or returns, contact us at [PHONE NUMBER] or via the Free Q&amp;A on this site.</p>
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
export class RefundPage {}