import { Component, computed, inject, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '@core/services/auth.service';
import { AppRoles } from '@core/auth/app-roles';

interface NavItem {
  label: string;
  icon: string;
  route?: string;
  children?: NavItem[];
  expanded?: boolean;
  /** When true the item is only shown to users whose roles include "Admin". */
  adminOnly?: boolean;
}

interface NavSection {
  header: string;
  items: NavItem[];
}

/**
 * Authenticated app shell: sidebar navigation, a header showing the signed-in user with a logout
 * action, and the routed content outlet. Rendered only behind the auth guard.
 */
@Component({
  selector: 'app-shell',
  imports: [NgClass, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly title = signal('CMS');
  protected readonly collapsed = signal(false);
  protected readonly userName = this.auth.userName;
  // Hides the 系統管理 Admin menu; adminGuard enforces the same rule on the routes themselves.
  protected readonly isAdmin = computed(() => this.auth.hasRole(AppRoles.Admin));

  // The full menu; items flagged adminOnly are filtered out in `navSections` for non-admins.
  private readonly allSections = signal<NavSection[]>([
    {
      header: '選單 MENU',
      items: [
        {
          label: '首頁 Home',
          icon: 'pi pi-home',
          expanded: true,
          children: [
            {
              label: '上稿作業 FeaturedPromoItem',
              icon: 'pi pi-calendar',
              route: '/featured-promo-items',
            },
          ],
        },
        {
          label: '課程管理 Course',
          icon: 'pi pi-book',
          expanded: true,
          children: [
            { label: '課程 Course', icon: 'pi pi-book', route: '/courses' },
            { label: '合作廠商 Partner', icon: 'pi pi-building', route: '/partners' },
            { label: '課程分類 CourseGroup', icon: 'pi pi-sitemap', route: '/course-groups' },
          ],
        },
        { label: '說明會 Seminar', icon: 'pi pi-comments' },
        { label: '活動管理 Promotion', icon: 'pi pi-megaphone' },
        { label: '線上報名 Forms', icon: 'pi pi-file-edit' },
        { label: '網站資訊 WebInfo', icon: 'pi pi-globe' },
        { label: '考試中心 TestingCenter', icon: 'pi pi-check-square' },
      ],
    },
    {
      header: '系統 SYSTEM',
      items: [
        {
          label: '系統管理 Admin',
          icon: 'pi pi-cog',
          expanded: true,
          adminOnly: true,
          children: [
            { label: '角色 AppRole', icon: 'pi pi-id-card', route: '/app-roles' },
            { label: '發布狀態 PublishStatus', icon: 'pi pi-flag', route: '/publish-statuses' },
            { label: '使用者 AppUser', icon: 'pi pi-users', route: '/app-users' },
          ],
        },
      ],
    },
  ]);

  // Admin-only items are hidden for non-admins; sections left empty are dropped entirely.
  protected readonly navSections = computed<NavSection[]>(() => {
    const admin = this.isAdmin();
    return this.allSections()
      .map((section) => ({
        ...section,
        items: section.items.filter((item) => !item.adminOnly || admin),
      }))
      .filter((section) => section.items.length > 0);
  });

  toggleSidebar(): void {
    this.collapsed.update((v) => !v);
  }

  toggleItem(item: NavItem): void {
    if (this.collapsed() || !item.children?.length) {
      return;
    }
    item.expanded = !item.expanded;
    this.allSections.update((s) => [...s]);
  }

  logout(): void {
    this.auth.clearSession();
    this.router.navigate(['/login']);
  }
}
