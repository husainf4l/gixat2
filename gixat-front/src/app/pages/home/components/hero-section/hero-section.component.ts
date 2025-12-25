import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-hero-section',
  standalone: true,
  imports: [CommonModule, RouterLink, NgOptimizedImage],
  templateUrl: './hero-section.component.html'
})
export class HeroSectionComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  
  isSignedIn = signal(false);
  currentIndex = 2;
  images = [
    { id: 0, src: '/hero/heroo6.webp', alt: 'Car maintenance service' },
    { id: 1, src: '/hero/heroo7.webp', alt: 'Professional auto repair' },
    { id: 2, src: '/hero/heroo8.webp', alt: 'Modern garage workshop' },
    { id: 3, src: '/hero/heroo9.webp', alt: 'Auto technician at work' },
    { id: 4, src: '/hero/heroo10.webp', alt: 'Vehicle inspection service' }
  ];
  intervalId: any;

  ngOnInit() {
    // Check if user is signed in
    this.authService.me().subscribe({
      next: (userData) => {
        this.isSignedIn.set(!!userData.me?.id);
      },
      error: () => {
        this.isSignedIn.set(false);
      }
    });

    this.startAutoRotate();
  }

  ngOnDestroy() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }

  startAutoRotate() {
    this.intervalId = setInterval(() => {
      this.next();
    }, 4000);
  }

  next() {
    this.currentIndex = (this.currentIndex + 1) % this.images.length;
  }

  prev() {
    this.currentIndex = (this.currentIndex - 1 + this.images.length) % this.images.length;
  }

  getItemStyles(index: number) {
    const offset = index - this.currentIndex;
    const total = this.images.length;
    let pos = (offset + total) % total;
    if (pos > Math.floor(total / 2)) {
      pos = pos - total;
    }

    const isCenter = pos === 0;
    const isAdjacent = Math.abs(pos) === 1;

    const translateX = pos * 45;
    const scale = isCenter ? 1 : isAdjacent ? 0.85 : 0.7;
    const rotateY = pos * -10;
    const zIndex = isCenter ? 10 : isAdjacent ? 5 : 1;
    const opacity = isCenter ? 1 : isAdjacent ? 0.4 : 0;
    const blur = isCenter ? 0 : 4;
    const visibility = Math.abs(pos) > 1 ? 'hidden' : 'visible';

    return {
      transform: `translateX(${translateX}%) scale(${scale}) rotateY(${rotateY}deg)`,
      zIndex: zIndex,
      opacity: opacity,
      filter: `blur(${blur}px)`,
      visibility: visibility
    };
  }
}
