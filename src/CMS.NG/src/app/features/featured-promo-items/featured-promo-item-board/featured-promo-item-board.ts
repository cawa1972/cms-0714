import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';

import {
  FeaturedPromoItem,
  FeaturedPromoItemRequest,
  PromoCodeLookup,
} from '@core/models/featured-promo-item.model';
import { FeaturedPromoItemService } from '@core/services/featured-promo-item.service';
import { LookupService } from '@core/services/lookup.service';
import { toIso } from '@core/utils/date.util';

const STATE_KEY = 'featured-promo-item-board-state';
const SLOTS = [1, 2, 3];
const WEEKDAYS = ['日', '一', '二', '三', '四', '五', '六'];

interface CenterOption {
  value: number;
  label: string;
}

interface SlotCell {
  slot: number;
  item: FeaturedPromoItem | null;
}

interface DayRow {
  iso: string;
  label: string;
  cells: SlotCell[];
}

/** Position of the inline editor: a day+slot; pkid null = creating in an empty slot. */
interface EditTarget {
  iso: string;
  slot: number;
  pkid: number | null;
}

/**
 * Weekly schedule board for FeaturedPromoItem (首頁上稿作業) — a custom UI instead of the
 * standard list/detail/form. One tab per TrainingCenter, one section per day of the selected
 * Monday–Sunday week, 3 slots per day. Rows edit inline: the user types a Promotion2 PromoCode,
 * which is resolved server-side to Promotion_pkid (Topic/Description default from the promotion).
 * + / − swap an item with its neighbour slot; Copy/Paste duplicate a row into an empty slot.
 */
@Component({
  selector: 'app-featured-promo-item-board',
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, TooltipModule],
  templateUrl: './featured-promo-item-board.html',
  styleUrl: './featured-promo-item-board.css',
})
export class FeaturedPromoItemBoard implements OnInit {
  private readonly service = inject(FeaturedPromoItemService);
  private readonly lookups = inject(LookupService);
  private readonly confirm = inject(ConfirmationService);
  private readonly messages = inject(MessageService);
  private readonly fb = inject(FormBuilder);

  protected readonly centerOptions = signal<CenterOption[]>([]);
  protected readonly selectedCenter = signal<number | null>(null);
  protected readonly weekStart = signal<Date>(FeaturedPromoItemBoard.mondayOf(new Date()));
  protected readonly items = signal<FeaturedPromoItem[]>([]);
  protected readonly loading = signal(false);

  protected readonly editing = signal<EditTarget | null>(null);
  protected readonly clipboard = signal<PromoCodeLookup | null>(null);
  /** Last successful PromoCode lookup; save() reuses it when the code hasn't changed. */
  protected readonly resolvedPromo = signal<PromoCodeLookup | null>(null);
  protected readonly saving = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    promoCode: ['', [Validators.required, Validators.maxLength(30)]],
    topic: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.required, Validators.maxLength(300)]],
  });

  protected readonly weekLabel = computed(() => {
    const start = this.weekStart();
    const end = FeaturedPromoItemBoard.addDays(start, 6);
    return `${start.getMonth() + 1}/${start.getDate()} — ${end.getMonth() + 1}/${end.getDate()}`;
  });

  protected readonly days = computed<DayRow[]>(() => {
    const byKey = new Map<string, FeaturedPromoItem>();
    for (const item of this.items()) {
      byKey.set(`${item.scheduleOn}|${item.slot}`, item);
    }
    return Array.from({ length: 7 }, (_, i) => {
      const date = FeaturedPromoItemBoard.addDays(this.weekStart(), i);
      const iso = toIso(date)!;
      return {
        iso,
        label: `${date.getMonth() + 1}/${date.getDate()} (${WEEKDAYS[date.getDay()]})`,
        cells: SLOTS.map((slot) => ({ slot, item: byKey.get(`${iso}|${slot}`) ?? null })),
      };
    });
  });

  ngOnInit(): void {
    this.restoreState();
    this.lookups.getTrainingCenters().subscribe({
      next: (centers) => {
        const options = centers.map((o) => ({ value: Number(o.value), label: o.label }));
        this.centerOptions.set(options);
        if (this.selectedCenter() === null || !options.some((o) => o.value === this.selectedCenter())) {
          this.selectedCenter.set(options[0]?.value ?? null);
        }
        this.load();
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得訓練中心。' });
      },
    });
  }

  private restoreState(): void {
    const raw = sessionStorage.getItem(STATE_KEY);
    if (!raw) {
      return;
    }
    try {
      const state = JSON.parse(raw) as { trainingCenterPkid?: number; weekStartIso?: string };
      if (typeof state.trainingCenterPkid === 'number') {
        this.selectedCenter.set(state.trainingCenterPkid);
      }
      if (state.weekStartIso) {
        this.weekStart.set(FeaturedPromoItemBoard.mondayOf(new Date(state.weekStartIso + 'T00:00:00')));
      }
    } catch {
      // ignore corrupt state
    }
  }

  private persistState(): void {
    sessionStorage.setItem(
      STATE_KEY,
      JSON.stringify({
        trainingCenterPkid: this.selectedCenter(),
        weekStartIso: toIso(this.weekStart()),
      }),
    );
  }

  load(): void {
    const center = this.selectedCenter();
    if (center === null) {
      this.items.set([]);
      return;
    }
    this.persistState();
    this.loading.set(true);
    this.service
      .query({
        scheduleOnFrom: toIso(this.weekStart()),
        scheduleOnTo: toIso(FeaturedPromoItemBoard.addDays(this.weekStart(), 6)),
        trainingCenterPkid: center,
      })
      .subscribe({
        next: (data) => {
          this.items.set(data);
          this.loading.set(false);
        },
        error: () => {
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法取得上稿資料。' });
          this.loading.set(false);
        },
      });
  }

  selectCenter(pkid: number): void {
    if (this.selectedCenter() === pkid) {
      return;
    }
    this.selectedCenter.set(pkid);
    this.cancelEdit();
    this.load();
  }

  prevWeek(): void {
    this.shiftWeek(-7);
  }

  nextWeek(): void {
    this.shiftWeek(7);
  }

  private shiftWeek(delta: number): void {
    this.weekStart.set(FeaturedPromoItemBoard.addDays(this.weekStart(), delta));
    this.cancelEdit();
    this.load();
  }

  isEditing(iso: string, slot: number): boolean {
    const e = this.editing();
    return e !== null && e.iso === iso && e.slot === slot;
  }

  /** Open the inline editor — prefilled for an existing item, empty for a vacant slot. */
  startEdit(day: DayRow, cell: SlotCell): void {
    this.editing.set({ iso: day.iso, slot: cell.slot, pkid: cell.item?.pkid ?? null });
    if (cell.item) {
      this.form.setValue({
        promoCode: cell.item.promoCode ?? '',
        topic: cell.item.topic,
        description: cell.item.description,
      });
      this.resolvedPromo.set(
        cell.item.promoCode
          ? {
              promotionPkid: cell.item.promotionPkid,
              promoCode: cell.item.promoCode,
              topic: cell.item.topic,
              description: cell.item.description,
            }
          : null,
      );
    } else {
      this.form.reset({ promoCode: '', topic: '', description: '' });
      this.resolvedPromo.set(null);
    }
  }

  /** Remember a row's values so 貼上 can duplicate them into an empty slot. */
  copy(item: FeaturedPromoItem): void {
    this.clipboard.set({
      promotionPkid: item.promotionPkid,
      promoCode: item.promoCode ?? '',
      topic: item.topic,
      description: item.description,
    });
    this.messages.add({ severity: 'info', summary: '已複製', detail: `促銷代碼「${item.promoCode}」已複製。` });
  }

  /** Open the editor on an empty slot, prefilled from the clipboard. */
  paste(day: DayRow, cell: SlotCell): void {
    const clip = this.clipboard();
    if (!clip || cell.item) {
      return;
    }
    this.editing.set({ iso: day.iso, slot: cell.slot, pkid: null });
    this.form.setValue({ promoCode: clip.promoCode, topic: clip.topic, description: clip.description });
    this.resolvedPromo.set(clip);
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.resolvedPromo.set(null);
  }

  /** Explicit PromoCode lookup (查代碼 button); fills empty Topic/Description as defaults. */
  lookupPromo(): void {
    const code = this.form.controls.promoCode.value.trim();
    if (!code) {
      this.form.controls.promoCode.markAsTouched();
      return;
    }
    this.service.lookupPromoCode(code).subscribe({
      next: (promo) => {
        this.resolvedPromo.set(promo);
        if (!this.form.controls.topic.value) {
          this.form.controls.topic.setValue(promo.topic);
        }
        if (!this.form.controls.description.value) {
          this.form.controls.description.setValue(promo.description);
        }
        this.messages.add({ severity: 'success', summary: '代碼有效', detail: `已找到促銷「${promo.promoCode}」。` });
      },
      error: () => {
        this.resolvedPromo.set(null);
        this.messages.add({ severity: 'warn', summary: '查無代碼', detail: `找不到促銷代碼「${code}」。` });
      },
    });
  }

  /** Save the inline editor: resolve the PromoCode if needed, then create/update. */
  save(): void {
    const target = this.editing();
    if (!target) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const code = this.form.controls.promoCode.value.trim();
    const resolved = this.resolvedPromo();
    if (resolved && resolved.promoCode === code) {
      this.persistItem(target, resolved.promotionPkid);
      return;
    }
    this.saving.set(true);
    this.service.lookupPromoCode(code).subscribe({
      next: (promo) => {
        this.resolvedPromo.set(promo);
        this.persistItem(target, promo.promotionPkid);
      },
      error: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'warn', summary: '查無代碼', detail: `找不到促銷代碼「${code}」，無法儲存。` });
      },
    });
  }

  private persistItem(target: EditTarget, promotionPkid: number): void {
    const center = this.selectedCenter();
    if (center === null) {
      return;
    }
    const request: FeaturedPromoItemRequest = {
      pkid: target.pkid ?? 0,
      scheduleOn: target.iso,
      trainingCenterPkid: center,
      slot: target.slot,
      promotionPkid,
      topic: this.form.controls.topic.value.trim(),
      description: this.form.controls.description.value.trim(),
    };
    this.saving.set(true);
    const call = target.pkid === null ? this.service.create(request) : this.service.update(request);
    call.subscribe({
      next: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'success', summary: '已儲存', detail: `${target.iso} 版位 ${target.slot} 已儲存。` });
        this.cancelEdit();
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail = err.status === 409 ? '該日期／版位已有資料。' : '儲存時發生錯誤。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  /** + moves the item one slot down (1→2); − moves it up (2→1). Swaps if occupied. */
  move(item: FeaturedPromoItem, direction: 'up' | 'down'): void {
    this.service.move(item.pkid, direction).subscribe({
      next: () => this.load(),
      error: () => {
        this.messages.add({ severity: 'error', summary: '移動失敗', detail: '無法調整版位。' });
      },
    });
  }

  remove(item: FeaturedPromoItem, event: Event): void {
    this.confirm.confirm({
      target: event.target as EventTarget,
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${item.pkid}</b>「${item.scheduleOn} 版位 ${item.slot}：${item.topic}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.service.delete(item.pkid).subscribe({
          next: () => {
            this.messages.add({ severity: 'success', summary: '已刪除', detail: `「${item.topic}」已刪除。` });
            this.load();
          },
          error: () => {
            this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '刪除上稿資料時發生錯誤。' });
          },
        });
      },
    });
  }

  /** Monday (local midnight) of the week containing the given date. */
  static mondayOf(date: Date): Date {
    const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const offset = (d.getDay() + 6) % 7; // Mon=0 … Sun=6
    d.setDate(d.getDate() - offset);
    return d;
  }

  static addDays(date: Date, days: number): Date {
    const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    d.setDate(d.getDate() + days);
    return d;
  }
}
