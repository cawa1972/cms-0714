import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

import { QrCode } from './qr-code';

function setup(value: string, title: string | null = null, downloadFileName?: string) {
  TestBed.configureTestingModule({
    imports: [QrCode],
    providers: [provideNoopAnimations()],
  });

  const fixture: ComponentFixture<QrCode> = TestBed.createComponent(QrCode);
  fixture.componentRef.setInput('value', value);
  fixture.componentRef.setInput('title', title);
  if (downloadFileName !== undefined) {
    fixture.componentRef.setInput('downloadFileName', downloadFileName);
  }
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance };
}

describe('QrCode', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('encodes the given value into a QR code image', async () => {
    const { fixture, component } = setup('https://www.uuu.com.tw/Course/Show/2/AWS-SAA');
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component['dataUrl']()).toMatch(/^data:image\/png;base64,/);
    const img: HTMLImageElement = fixture.nativeElement.querySelector('.qr-code-image');
    expect(img.src).toMatch(/^data:image\/png;base64,/);
  });

  it('shows the title above the QR code', async () => {
    const { fixture } = setup('https://www.uuu.com.tw/Course/Show/2/AWS-SAA', 'AWS-SAA');
    await fixture.whenStable();
    fixture.detectChanges();

    const titleEl: HTMLElement = fixture.nativeElement.querySelector('.qr-code-title');
    expect(titleEl.textContent?.trim()).toBe('AWS-SAA');
  });

  it('download() saves the generated QR code as an image file', async () => {
    const { fixture, component } = setup(
      'https://www.uuu.com.tw/Course/Show/2/AWS-SAA',
      'AWS-SAA',
      'AWS-SAA-qrcode.png',
    );
    await fixture.whenStable();
    fixture.detectChanges();

    const anchor = document.createElement('a');
    spyOn(anchor, 'click');
    spyOn(document, 'createElement').and.returnValue(anchor);

    component.download();

    expect(anchor.href).toMatch(/^data:image\/png;base64,/);
    expect(anchor.download).toBe('AWS-SAA-qrcode.png');
    expect(anchor.click).toHaveBeenCalled();
  });
});
