import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CardWidget } from './card-widget';

describe('CardWidget', () => {
  let component: CardWidget;
  let fixture: ComponentFixture<CardWidget>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardWidget],
    }).compileComponents();

    fixture = TestBed.createComponent(CardWidget);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
