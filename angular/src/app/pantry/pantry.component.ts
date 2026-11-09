import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { LocalizationPipe, ListService, PagedResultDto } from '@abp/ng.core';
import { PantryItemDto, PantryItemService, PantryItemStatus } from '../proxy/pantry-items';
import { catchError, finalize, of } from 'rxjs';

@Component({
  selector: 'app-pantry',
  standalone: true,
  imports: [LocalizationPipe, DatePipe],
  providers: [ListService],
  templateUrl: './pantry.component.html',
})
export class PantryComponent implements OnInit {
  readonly list = inject(ListService);
  private readonly pantryItemService = inject(PantryItemService);

  readonly Status = PantryItemStatus;

  items = signal<PantryItemDto[]>([]);
  totalCount = signal(0);
  loading = signal(true);
  error = signal(false);

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.list.maxResultCount)),
  );

  ngOnInit(): void {
    this.list
      .hookToQuery(query => {
        this.loading.set(true);
        this.error.set(false);
        return this.pantryItemService.getList(query).pipe(
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
}