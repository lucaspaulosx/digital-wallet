import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TransferComponent } from './transfer.component';

describe('TransferComponent', () => {
  let fixture: ComponentFixture<TransferComponent>;
  let component: TransferComponent;
  let http: HttpTestingController;

  const fill = (amount: number) =>
    component.form.setValue({ toEmail: 'bia@teste.com', amount, description: 'Teste' });

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [TransferComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(TransferComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  it('does not submit an invalid form', () => {
    component.submit();

    expect(component.form.invalid).toBeTrue();
    http.expectNone('/api/transfers');
  });

  it('sends an Idempotency-Key header', () => {
    fill(50);
    component.submit();

    const request = http.expectOne('/api/transfers');
    expect(request.request.headers.get('Idempotency-Key')).toBeTruthy();
    expect(request.request.body.amount).toBe(50);
  });

  it('reuses the same key when retrying after a network error', () => {
    fill(50);
    component.submit();
    const first = http.expectOne('/api/transfers');
    const key = first.request.headers.get('Idempotency-Key');
    first.flush({ title: 'Erro' }, { status: 500, statusText: 'Server Error' });

    component.submit();
    const retry = http.expectOne('/api/transfers');

    expect(retry.request.headers.get('Idempotency-Key')).toBe(key);
  });

  it('uses a new key after a successful transfer', () => {
    fill(50);
    component.submit();
    const first = http.expectOne('/api/transfers');
    const key = first.request.headers.get('Idempotency-Key');
    first.flush({ id: '1', amount: 50, toEmail: 'bia@teste.com', description: 'Teste', createdAt: new Date().toISOString() });

    fill(50);
    component.submit();
    const second = http.expectOne('/api/transfers');

    expect(second.request.headers.get('Idempotency-Key')).not.toBe(key);
  });

  it('shows the server message when the transfer is rejected', () => {
    fill(5000);
    component.submit();
    http.expectOne('/api/transfers').flush({ detail: 'O saldo não cobre o valor da transferência.' }, { status: 422, statusText: 'Unprocessable' });

    expect(component.error()).toBe('O saldo não cobre o valor da transferência.');
  });
});
