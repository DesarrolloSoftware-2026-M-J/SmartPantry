import { HttpErrorResponse } from '@angular/common/http';
import { Pipe, PipeTransform } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LIST_QUERY_DEBOUNCE_TIME, LocalizationPipe } from '@abp/ng.core';
import { Confirmation, ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import { Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { PantryItemDto, PantryItemService, PantryItemStatus } from '../proxy/pantry-items';
import { PantryComponent } from './pantry.component';

// Reemplaza al pipe de localizacion: devuelve la clave tal cual, asi no depende del backend.
@Pipe({ name: 'abpLocalization', standalone: true })
class FakeLocalizationPipe implements PipeTransform {
  transform(value: unknown): string {
    return String(value);
  }
}

const makeItem = (overrides: Partial<PantryItemDto> = {}): PantryItemDto =>
  ({
    id: 'item-1',
    productName: 'Arroz',
    productBrand: 'Gallo',
    quantity: 2,
    unit: 'kg',
    expirationDate: '2026-12-15',
    status: PantryItemStatus.Active,
    ...overrides,
  }) as PantryItemDto;

describe('PantryComponent', () => {
  let fixture: ComponentFixture<PantryComponent>;
  let component: PantryComponent;

  const service = {
    getList: vi.fn(),
    update: vi.fn(),
    markAsConsumed: vi.fn(),
    delete: vi.fn(),
  };
  const confirmation = { info: vi.fn(), warn: vi.fn() };
  const toaster = { success: vi.fn(), warn: vi.fn(), error: vi.fn() };

  beforeEach(() => {
    vi.useFakeTimers();
    vi.resetAllMocks();
    confirmation.info.mockReturnValue(of(Confirmation.Status.confirm));
    confirmation.warn.mockReturnValue(of(Confirmation.Status.confirm));

    TestBed.configureTestingModule({
      imports: [PantryComponent],
      providers: [
        { provide: PantryItemService, useValue: service },
        { provide: ConfirmationService, useValue: confirmation },
        { provide: ToasterService, useValue: toaster },
        { provide: LIST_QUERY_DEBOUNCE_TIME, useValue: 0 },
      ],
    });
    TestBed.overrideComponent(PantryComponent, {
      remove: { imports: [LocalizationPipe] },
      add: { imports: [FakeLocalizationPipe] },
    });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  async function refresh(): Promise<void> {
    fixture.detectChanges();
    await vi.advanceTimersByTimeAsync(10);
    fixture.detectChanges();
  }

  async function render(): Promise<void> {
    fixture = TestBed.createComponent(PantryComponent);
    component = fixture.componentInstance;
    await refresh();
  }

  const html = () => fixture.nativeElement as HTMLElement;

  const buttonByKey = (key: string): HTMLButtonElement | undefined =>
    Array.from(html().querySelectorAll('button')).find(b => b.textContent?.includes(key));

  it('muestra el estado de carga mientras espera al backend', async () => {
    service.getList.mockReturnValue(new Subject());
    await render();

    expect(html().querySelector('.spinner-border')).not.toBeNull();
  });

  it('muestra el error y permite reintentar', async () => {
    service.getList.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    await render();
    expect(html().querySelector('.alert-danger')).not.toBeNull();

    service.getList.mockReturnValue(of({ items: [makeItem()], totalCount: 1 }));
    buttonByKey('::Pantry:Retry')!.click();
    await refresh();

    expect(html().querySelector('.alert-danger')).toBeNull();
    expect(html().querySelectorAll('tbody tr').length).toBe(1);
  });

  it('muestra el mensaje de despensa vacia', async () => {
    service.getList.mockReturnValue(of({ items: [], totalCount: 0 }));
    await render();

    expect(html().querySelector('.alert-info')).not.toBeNull();
    expect(html().querySelector('table')).toBeNull();
  });

  it('lista los items y marca los de datos incompletos', async () => {
    service.getList.mockReturnValue(
      of({
        items: [makeItem(), makeItem({ id: 'item-2', quantity: undefined })],
        totalCount: 2,
      }),
    );
    await render();

    expect(html().querySelectorAll('tbody tr').length).toBe(2);
    expect(html().querySelectorAll('.badge.bg-warning').length).toBe(1);
  });

  it('no permite editar ni consumir un item consumido, pero si eliminarlo', async () => {
    service.getList.mockReturnValue(
      of({ items: [makeItem({ status: PantryItemStatus.Consumed })], totalCount: 1 }),
    );
    await render();

    expect(buttonByKey('::Pantry:Edit')).toBeUndefined();
    expect(buttonByKey('::Pantry:MarkAsConsumed')).toBeUndefined();
    expect(buttonByKey('::Pantry:Delete')).toBeDefined();
  });

  it('marca como consumido tras confirmar y recarga la lista', async () => {
    service.getList.mockReturnValue(of({ items: [makeItem()], totalCount: 1 }));
    service.markAsConsumed.mockReturnValue(of(makeItem({ status: PantryItemStatus.Consumed })));
    await render();

    buttonByKey('::Pantry:MarkAsConsumed')!.click();
    await refresh();

    expect(service.markAsConsumed).toHaveBeenCalledWith('item-1', { skipHandleError: true });
    expect(toaster.success).toHaveBeenCalled();
    expect(service.getList.mock.calls.length).toBeGreaterThanOrEqual(2);
  });

  it('elimina el item tras confirmar', async () => {
    service.getList.mockReturnValue(of({ items: [makeItem()], totalCount: 1 }));
    service.delete.mockReturnValue(of(undefined));
    await render();

    buttonByKey('::Pantry:Delete')!.click();
    await refresh();

    expect(service.delete).toHaveBeenCalledWith('item-1', { skipHandleError: true });
    expect(toaster.success).toHaveBeenCalled();
  });

  it('no elimina si se cancela la confirmacion', async () => {
    confirmation.warn.mockReturnValue(of(Confirmation.Status.reject));
    service.getList.mockReturnValue(of({ items: [makeItem()], totalCount: 1 }));
    await render();

    buttonByKey('::Pantry:Delete')!.click();
    await refresh();

    expect(service.delete).not.toHaveBeenCalled();
  });

  it('avisa y recarga si el item ya no existe (404)', async () => {
    service.getList.mockReturnValue(of({ items: [makeItem()], totalCount: 1 }));
    service.delete.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    await render();

    buttonByKey('::Pantry:Delete')!.click();
    await refresh();

    expect(toaster.warn).toHaveBeenCalledWith('::Pantry:ItemNotFound');
    expect(service.getList.mock.calls.length).toBeGreaterThanOrEqual(2);
  });

  it('valida la cantidad y envia los cambios al guardar', async () => {
    service.getList.mockReturnValue(of({ items: [makeItem()], totalCount: 1 }));
    service.update.mockReturnValue(of(makeItem({ quantity: 5 })));
    await render();

    buttonByKey('::Pantry:Edit')!.click();
    await refresh();
    expect(component.form.value.quantity).toBe(2);

    component.form.controls.quantity.setValue(0);
    component.save();
    expect(service.update).not.toHaveBeenCalled();

    component.form.controls.quantity.setValue(5);
    component.save();
    expect(service.update).toHaveBeenCalledWith(
      'item-1',
      { quantity: 5, unit: 'kg', expirationDate: '2026-12-15' },
      { skipHandleError: true },
    );
  });
});