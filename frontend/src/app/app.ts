import { Component, signal } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { Header } from './layout/header';
import { Footer } from './layout/footer';
import { CartDrawer } from './core/services/cart-drawer';
/**
 * The Admin has its own shell (AdminShell: sidebar nav, its own header) —
 * the public storefront's Header/Footer must not wrap it, or Admin ends up
 * with two navigation bars stacked on top of each other. Tracked by URL
 * prefix rather than route data since Header/Footer sit outside the routed
 * component tree in app.html.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, Footer, CartDrawer],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected isAdminRoute = signal(false);

  constructor(router: Router) {
    this.isAdminRoute.set(router.url.startsWith('/admin'));
    router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe((event) => {
      this.isAdminRoute.set(event.urlAfterRedirects.startsWith('/admin'));
    });
  }
}