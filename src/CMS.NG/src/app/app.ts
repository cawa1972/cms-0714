import { Component, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

interface NavItem {
  label: string;
  icon: string;
  route?: string;
  children?: NavItem[];
  expanded?: boolean;
}

interface NavSection {
  header: string;
  items: NavItem[];
}

@Component({
  selector: 'app-root',
  imports: [NgClass, RouterOutlet, RouterLink, RouterLinkActive, ToastModule, ConfirmDialogModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly title = signal('CMS');
  protected readonly collapsed = signal(false);

  protected readonly navSections = signal<NavSection[]>([
    {
      header: '選單 MENU',
      items: [
        { label: '首頁管理 Home', icon: 'pi pi-home' },
        {
          label: '課程管理 Course',
          icon: 'pi pi-book',
          expanded: true,
          children: [{ label: '課程分類 CourseGroup', icon: 'pi pi-sitemap', route: '/course-groups' }],
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
          children: [
            { label: '角色 AppRole', icon: 'pi pi-id-card', route: '/app-roles' },
            { label: '發布狀態 PublishStatus', icon: 'pi pi-flag', route: '/publish-statuses' },
            { label: '使用者 AppUser', icon: 'pi pi-users' },
          ],
        },
      ],
    },
  ]);

  toggleSidebar(): void {
    this.collapsed.update((v) => !v);
  }

  toggleItem(item: NavItem): void {
    if (this.collapsed() || !item.children?.length) {
      return;
    }
    item.expanded = !item.expanded;
    this.navSections.update((s) => [...s]);
  }
}
