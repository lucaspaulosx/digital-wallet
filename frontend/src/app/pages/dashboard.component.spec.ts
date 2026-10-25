import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  let fixture: ComponentFixture<DashboardComponent>;
  let http: HttpTestingController;

  const render = (phoneVerified: boolean) => {
    http.expectOne('/api/accounts/me').flush({
      accountId: '1', name: 'Ana', email: 'ana@teste.com', balance: 100, phoneVerified,
    });
    http.expectOne((r) => r.url === '/api/accounts/me/transactions').flush([]);
    fixture.detectChanges();
  };

  const hrefs = (): string[] =>
    Array.from<HTMLAnchorElement>(fixture.nativeElement.querySelectorAll('.actions a'))
      .map((a) => a.getAttribute('href') ?? '');

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  it('shows the transfer and credit links', () => {
    render(true);

    expect(hrefs()).toContain('/transferir');
    expect(hrefs()).toContain('/credito');
  });

  it('hides the activation link when the phone is verified', () => {
    render(true);

    expect(hrefs()).not.toContain('/ativar');
  });

  it('shows the activation link when the phone is not verified', () => {
    render(false);

    expect(hrefs()).toContain('/ativar');
  });
});
