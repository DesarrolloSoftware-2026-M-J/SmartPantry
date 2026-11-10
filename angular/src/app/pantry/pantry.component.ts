  import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe, ListService, PagedResultDto } from '@abp/ng.core';
import { Confirmation, ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import { PantryItemDto, PantryItemService, PantryItemStatus } from '../proxy/pantry-items';
import { catchError, finalize, of } from 'rxjs';

@Component({
  selector: 'app-pantry',
  standalone: true,
  imports: [LocalizationPipe, DatePipe, ReactiveFormsModule],
  providers: [ListService],
  templateUrl: './pantry.component.html',
})
export class PantryComponent implements OnInit {
  readonly list = inject(ListService);
  private readonly pantryItemService = inject(PantryItemService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  private readonly fb = inject(FormBuilder);

  readonly Status = PantryItemStatus;

  items = signal<PantryItemDto[]>([]);
  totalCount = signal(0);
  loading = signal(true);
  error = signal(false);

  editing = signal<PantryItemDto | null>(null);
  saving = signal(false);

  form = this.fb.group({
    quantity: this.fb.control<number | null>(null, [Validators.min(0.001)]),
    unit: this.fb.control<string>('', [Validators.maxLength(32)]),
    expirationDate: this.fb.control<string | null>(null),
  });

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.list.maxResultCount)),
  );

  ngOnInit(): void {
    this.list
      .hookToQuery(query => {
        this.loading.set(true);
        this.error.set(false);
        return this.pantryItemService.getList(query, { skipHandleError: true }).pipe(
          catchError(() => {
            this.error.set(true);
            return of({ items: [], totalCount: 0 } as PagedResultDto<PantryItemDto>);
          }),
          finalize(() => this.loading.set(false)),
        );
      })
      .subscribe(result => {
        this.items.set(result.items ?? []);
        this.totalCount.set(result.totalCount ?? 0);
      });
  }

  retry(): void {
    this.list.get();
  }

  isIncomplete(item: PantryItemDto): boolean {
    return item.quantity == null || !item.expirationDate;
  }

  startEdit(item: PantryItemDto): void {
    this.editing.set(item);
    this.form.reset({
      quantity: item.quantity ?? null,
      unit: item.unit ?? '',
      expirationDate: item.expirationDate ?? null,
    });
  }

  cancelEdit(): void {
    this.editing.set(null);
  }

  save(): void {
    const item = this.editing();
    if (!item) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.pantryItemService
      .update(
        item.id!,
        {
          quantity: value.quantity ?? undefined,
          unit: value.unit?.trim() || undefined,
          expirationDate: value.expirationDate || undefined,
        },
        { skipHandleError: true },
      )
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.toaster.success('::Pantry:Saved');
          this.editing.set(null);
          this.list.get();
        },
        error: err => this.onActionError(err),
      });
  }

  consume(item: PantryItemDto): void {
    this.confirmation
      .info('::Pantry:ConfirmConsumeMessage', '::Pantry:ConfirmConsumeTitle')
      .subscribe(status => {
        if (status !== Confirmation.Status.confirm) {
          return;
        }
        this.pantryItemService.markAsConsumed(item.id!, { skipHandleError: true }).subscribe({
          next: () => {
            this.toaster.success('::Pantry:MarkedAsConsumed');
            this.list.get();
          },
          error: err => this.onActionError(err),
        });
      });
  }

  delete(item: PantryItemDto): void {
    this.confirmation
      .warn('::Pantry:ConfirmDeleteMessage', '::Pantry:ConfirmDeleteTitle')
      .subscribe(status => {
        if (status !== Confirmation.Status.confirm) {
          return;
        }
        this.pantryItemService.delete(item.id!, { skipHandleError: true }).subscribe({
          next: () => {
            this.toaster.success('::Pantry:Deleted');
            this.list.get();
          },
          error: err => this.onActionError(err),
        });
      });
  }

  private onActionError(err: HttpErrorResponse): void {
    if (err?.status === 404) {
      this.toaster.warn('::Pantry:ItemNotFound');
      this.editing.set(null);
      this.list.get();
      return;
    }

    if (err?.status === 400) {
      const apiError = err.error?.error;
      const details = (apiError?.validationErrors ?? [])
        .map((e: { message: string }) => e.message)
        .join(' ');
      const message = details || apiError?.message;
      if (message) {
        this.toaster.error(message);
        return;
      }
    }

    this.toaster.error('::Pantry:ActionError');
  }
}