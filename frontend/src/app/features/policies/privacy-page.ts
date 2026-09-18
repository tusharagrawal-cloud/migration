import { Component } from '@angular/core';
import { PageContainer } from '../../shared/ui/page-container';

@Component({
  selector: 'app-privacy-page',
  imports: [PageContainer],
  template: `
    <app-page-container>
      <div class="policy-page">
        <p class="eyebrow">Legal</p>
        <h1>Privacy Policy</h1>
        <p class="updated">Last updated: [DATE]</p>

        <h2>1. Introduction</h2>
        <p>
          This Privacy Policy explains how ONE77 Sports collects, uses, and protects your personal information when
          you visit or make a purchase on this website.
        </p>

        <h2>2. Information We Collect</h2>
        <p>
          We collect information you provide directly, such as your name, email address, phone number, shipping
          address, and payment details, as well as information collected automatically, such as browsing behaviour
          and device information.
        </p>

        <h2>3. How We Use Your Information</h2>
        <p>
          We use your information to process orders, communicate order and shipping updates, provide customer
          support, improve our website, and, where you have opted in, send you marketing communications.
        </p>

        <h2>4. Sharing of Information</h2>
        <p>
          We share information with service providers who help us operate the site — including our e-commerce and
          payment platform (Shopify), shipping partners, and communication providers — only to the extent necessary
          to provide these services. We do not sell your personal information to third parties.
        </p>

        <h2>5. Data Security</h2>
        <p>
          We use reasonable technical and organisational measures to protect your information. However, no method of
          transmission over the internet is completely secure, and we cannot guarantee absolute security.
        </p>

        <h2>6. Cookies</h2>
        <p>
          This site uses cookies and similar technologies to remember your preferences, keep your cart working
          correctly, and understand how the site is used.
        </p>

        <h2>7. Your Rights</h2>
        <p>
          You may request access to, correction of, or deletion of your personal information by contacting us using
          the details below.
        </p>

        <h2>8. Changes to This Policy</h2>
        <p>
          We may update this Privacy Policy from time to time. Continued use of the site after changes are posted
          constitutes acceptance of the revised policy.
        </p>

        <h2>9. Contact</h2>
        <p>For any questions regarding this policy, contact us at [PHONE NUMBER] or via the Free Q&amp;A on this site.</p>
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
export class PrivacyPage {}