/**
 * SR ENERGIA - Solar Energy Website
 * Main JavaScript File
 * ============================================
 */

'use strict';

/* ============================================
   LOADING SCREEN
   ============================================ */
window.addEventListener('load', () => {
  const loader = document.querySelector('.loading-screen');
  if (!loader) return;
  setTimeout(() => {
    loader.classList.add('hidden');
    setTimeout(() => loader.remove(), 500);
  }, 1400);
});


/* ============================================
   NAVBAR — scroll behavior & active links
   ============================================ */
const navbar = document.getElementById('navbar');

function updateNavbar() {
  if (!navbar) return;
  if (window.scrollY > 60) {
    navbar.classList.add('scrolled');
  } else {
    navbar.classList.remove('scrolled');
  }
}

// Active section highlight
const sections = document.querySelectorAll('section[id]');
const navLinks = document.querySelectorAll('.nav-menu a, .mobile-nav a');

function setActiveLink() {
  let current = '';
  const scrollY = window.scrollY + 100;

  sections.forEach(section => {
    if (section.offsetTop <= scrollY) {
      current = section.id;
    }
  });

  navLinks.forEach(link => {
    link.classList.remove('active');
    const href = link.getAttribute('href');
    if (href === `#${current}`) {
      link.classList.add('active');
    }
  });
}

window.addEventListener('scroll', () => {
  updateNavbar();
  setActiveLink();
  updateScrollTopBtn();
});

updateNavbar();
setActiveLink();


/* ============================================
   MOBILE MENU
   ============================================ */
const hamburger      = document.querySelector('.hamburger');
const mobileMenu     = document.querySelector('.mobile-menu');
const mobileOverlay  = document.querySelector('.mobile-menu-overlay');
const mobileClose    = document.querySelector('.mobile-menu-close');

function openMobileMenu() {
  hamburger.classList.add('active');
  mobileMenu.classList.add('active');
  mobileOverlay.classList.add('active');
  document.body.style.overflow = 'hidden';
}

function closeMobileMenu() {
  hamburger.classList.remove('active');
  mobileMenu.classList.remove('active');
  mobileOverlay.classList.remove('active');
  document.body.style.overflow = '';
}

if (hamburger) {
  hamburger.addEventListener('click', () => {
    if (mobileMenu.classList.contains('active')) {
      closeMobileMenu();
    } else {
      openMobileMenu();
    }
  });
}

if (mobileClose) mobileClose.addEventListener('click', closeMobileMenu);
if (mobileOverlay) mobileOverlay.addEventListener('click', closeMobileMenu);

// Close mobile menu when a link is clicked
document.querySelectorAll('.mobile-nav a').forEach(link => {
  link.addEventListener('click', closeMobileMenu);
});


/* ============================================
   SMOOTH SCROLL for anchor links
   ============================================ */
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
  anchor.addEventListener('click', function (e) {
    const target = document.querySelector(this.getAttribute('href'));
    if (!target) return;
    e.preventDefault();
    const offset = navbar ? navbar.offsetHeight : 70;
    const top = target.getBoundingClientRect().top + window.scrollY - offset;
    window.scrollTo({ top, behavior: 'smooth' });
  });
});


/* ============================================
   INTERSECTION OBSERVER — fade-in animations
   ============================================ */
const fadeElements = document.querySelectorAll('.fade-in, .fade-in-left, .fade-in-right');

const fadeObserver = new IntersectionObserver(
  (entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible');
        fadeObserver.unobserve(entry.target);
      }
    });
  },
  { threshold: 0.12, rootMargin: '0px 0px -40px 0px' }
);

fadeElements.forEach(el => fadeObserver.observe(el));


/* ============================================
   COUNTER ANIMATION
   ============================================ */
function animateCounter(el) {
  const target    = parseFloat(el.dataset.target);
  const suffix    = el.dataset.suffix  || '';
  const prefix    = el.dataset.prefix  || '';
  const isDecimal = el.dataset.decimal === 'true';
  const duration  = 2000;
  const steps     = 60;
  const increment = target / steps;
  let current     = 0;
  let step        = 0;

  const timer = setInterval(() => {
    step++;
    current = Math.min(current + increment, target);

    if (step >= steps) {
      current = target;
      clearInterval(timer);
    }

    const display = isDecimal
      ? current.toFixed(1)
      : Math.floor(current).toLocaleString('pt-BR');

    el.textContent = prefix + display + suffix;
  }, duration / steps);
}

const counterObserver = new IntersectionObserver(
  (entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting && !entry.target.dataset.animated) {
        entry.target.dataset.animated = 'true';
        animateCounter(entry.target);
        counterObserver.unobserve(entry.target);
      }
    });
  },
  { threshold: 0.5 }
);

document.querySelectorAll('.counter').forEach(el => counterObserver.observe(el));


/* ============================================
   TESTIMONIALS SLIDER
   ============================================ */
(function initSlider() {
  const track      = document.querySelector('.testimonials-track');
  const dots       = document.querySelectorAll('.dot');
  const btnPrev    = document.querySelector('.slider-btn.prev');
  const btnNext    = document.querySelector('.slider-btn.next');
  const cards      = document.querySelectorAll('.testimonial-card');

  if (!track || cards.length === 0) return;

  let current   = 0;
  let autoTimer = null;
  const perView = getPerView();

  function getPerView() {
    if (window.innerWidth <= 768)  return 1;
    if (window.innerWidth <= 1024) return 2;
    return 3;
  }

  const totalSlides = Math.ceil(cards.length / getPerView());

  function goTo(index) {
    const pv    = getPerView();
    const max   = Math.max(0, cards.length - pv);
    current     = Math.max(0, Math.min(index, max));

    // Percentage shift per card
    const cardW = 100 / pv;
    const shift = current * cardW;
    track.style.transform = `translateX(-${shift}%)`;

    // Update dots
    const dotIndex = Math.round(current / pv);
    dots.forEach((d, i) => d.classList.toggle('active', i === dotIndex));
  }

  function next() { goTo(current + getPerView()); }
  function prev() { goTo(current - getPerView()); }

  function startAuto() {
    stopAuto();
    autoTimer = setInterval(() => {
      const pv = getPerView();
      if (current + pv >= cards.length) {
        goTo(0);
      } else {
        next();
      }
    }, 4500);
  }

  function stopAuto() {
    if (autoTimer) clearInterval(autoTimer);
  }

  if (btnNext) btnNext.addEventListener('click', () => { stopAuto(); next(); startAuto(); });
  if (btnPrev) btnPrev.addEventListener('click', () => { stopAuto(); prev(); startAuto(); });

  dots.forEach((dot, i) => {
    dot.addEventListener('click', () => {
      stopAuto();
      goTo(i * getPerView());
      startAuto();
    });
  });

  // Touch / swipe support
  let touchStartX = 0;
  track.addEventListener('touchstart', e => { touchStartX = e.touches[0].clientX; }, { passive: true });
  track.addEventListener('touchend', e => {
    const diff = touchStartX - e.changedTouches[0].clientX;
    if (Math.abs(diff) > 50) {
      stopAuto();
      diff > 0 ? next() : prev();
      startAuto();
    }
  });

  window.addEventListener('resize', () => goTo(0));

  goTo(0);
  startAuto();
})();


/* ============================================
   FAQ ACCORDION
   ============================================ */
document.querySelectorAll('.faq-item').forEach(item => {
  const question = item.querySelector('.faq-question');
  const answer   = item.querySelector('.faq-answer');

  if (!question) return;

  question.addEventListener('click', () => {
    const isOpen = item.classList.contains('open');

    // Close all others
    document.querySelectorAll('.faq-item.open').forEach(openItem => {
      if (openItem !== item) {
        openItem.classList.remove('open');
        openItem.querySelector('.faq-answer').style.maxHeight = null;
      }
    });

    if (isOpen) {
      item.classList.remove('open');
      answer.style.maxHeight = null;
    } else {
      item.classList.add('open');
      answer.style.maxHeight = answer.scrollHeight + 'px';
    }
  });

  // keyboard support
  question.setAttribute('role', 'button');
  question.setAttribute('tabindex', '0');
  question.addEventListener('keydown', e => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      question.click();
    }
  });
});


/* ============================================
   CONTACT FORM
   ============================================ */
(function initContactForm() {
  const form    = document.getElementById('contact-form');
  const success = document.querySelector('.form-success');
  const fields  = form ? form.querySelectorAll('[required]') : [];

  if (!form) return;

  function validateEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

  function validatePhone(phone) {
    return /^[\d\s()\-+]{8,}$/.test(phone);
  }

  function showError(field, msg) {
    field.classList.add('error');
    let err = field.parentElement.querySelector('.field-error');
    if (!err) {
      err = document.createElement('span');
      err.className = 'field-error';
      err.style.cssText = 'display:block;font-size:0.75rem;color:#ef4444;margin-top:4px;';
      field.parentElement.appendChild(err);
    }
    err.textContent = msg;
  }

  function clearError(field) {
    field.classList.remove('error');
    const err = field.parentElement.querySelector('.field-error');
    if (err) err.remove();
  }

  function validateField(field) {
    clearError(field);
    const val = field.value.trim();

    if (field.hasAttribute('required') && !val) {
      showError(field, 'Este campo é obrigatório.');
      return false;
    }

    if (field.type === 'email' && val && !validateEmail(val)) {
      showError(field, 'Por favor, insira um email válido.');
      return false;
    }

    if (field.type === 'tel' && val && !validatePhone(val)) {
      showError(field, 'Por favor, insira um telefone válido.');
      return false;
    }

    return true;
  }

  // Live validation
  fields.forEach(field => {
    field.addEventListener('blur', () => validateField(field));
    field.addEventListener('input', () => {
      if (field.classList.contains('error')) validateField(field);
    });
  });

  form.addEventListener('submit', (e) => {
    e.preventDefault();

    let isValid = true;
    fields.forEach(field => {
      if (!validateField(field)) isValid = false;
    });

    if (!isValid) return;

    const btn = form.querySelector('.form-submit');
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Enviando...';
    btn.disabled = true;

    // Simulate send (replace with real fetch/API call)
    setTimeout(() => {
      btn.innerHTML = originalText;
      btn.disabled = false;
      form.style.display = 'none';
      if (success) success.style.display = 'block';

      // Reset after 8 seconds
      setTimeout(() => {
        if (success) success.style.display = 'none';
        form.style.display = 'block';
        form.reset();
      }, 8000);
    }, 1600);
  });
})();


/* ============================================
   SCROLL TO TOP BUTTON
   ============================================ */
const scrollTopBtn = document.querySelector('.scroll-top');

function updateScrollTopBtn() {
  if (!scrollTopBtn) return;
  scrollTopBtn.classList.toggle('visible', window.scrollY > 400);
}

if (scrollTopBtn) {
  scrollTopBtn.addEventListener('click', () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  });
}


/* ============================================
   NAVBAR — highlight nav items on scroll
   ============================================ */
// Already handled above via setActiveLink()


/* ============================================
   STATS BAR — stagger animation trigger
   ============================================ */
(function observeStats() {
  const statsBar = document.getElementById('stats-bar');
  if (!statsBar) return;

  const obs = new IntersectionObserver(
    ([entry]) => {
      if (entry.isIntersecting) {
        statsBar.querySelectorAll('.stat-item').forEach((item, i) => {
          setTimeout(() => {
            item.style.opacity = '1';
            item.style.transform = 'translateY(0)';
          }, i * 120);
        });
        obs.disconnect();
      }
    },
    { threshold: 0.3 }
  );

  statsBar.querySelectorAll('.stat-item').forEach(item => {
    item.style.opacity    = '0';
    item.style.transform  = 'translateY(20px)';
    item.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
  });

  obs.observe(statsBar);
})();


/* ============================================
   NAVBAR CTA — phone mask & UX
   ============================================ */
(function phoneMask() {
  const phoneInput = document.getElementById('phone');
  if (!phoneInput) return;

  phoneInput.addEventListener('input', function () {
    let v = this.value.replace(/\D/g, '');
    if (v.length > 11) v = v.slice(0, 11);

    if (v.length <= 10) {
      v = v.replace(/^(\d{2})(\d{4})(\d{0,4})/, '($1) $2-$3');
    } else {
      v = v.replace(/^(\d{2})(\d{5})(\d{0,4})/, '($1) $2-$3');
    }
    this.value = v;
  });
})();


/* ============================================
   SERVICE CARDS — tilt effect on hover
   ============================================ */
(function cardTilt() {
  document.querySelectorAll('.service-card').forEach(card => {
    card.addEventListener('mousemove', e => {
      const rect    = card.getBoundingClientRect();
      const x       = e.clientX - rect.left - rect.width  / 2;
      const y       = e.clientY - rect.top  - rect.height / 2;
      const tiltX   = -(y / rect.height) * 5;
      const tiltY   =  (x / rect.width)  * 5;
      card.style.transform = `perspective(800px) rotateX(${tiltX}deg) rotateY(${tiltY}deg) translateY(-8px)`;
    });

    card.addEventListener('mouseleave', () => {
      card.style.transform = '';
    });
  });
})();


/* ============================================
   HERO — particle canvas effect
   ============================================ */
(function heroParticles() {
  const canvas = document.getElementById('hero-canvas');
  if (!canvas) return;

  const ctx  = canvas.getContext('2d');
  let w, h, particles;

  function resize() {
    w = canvas.width  = canvas.offsetWidth;
    h = canvas.height = canvas.offsetHeight;
  }

  function createParticles() {
    particles = Array.from({ length: 60 }, () => ({
      x:    Math.random() * w,
      y:    Math.random() * h,
      r:    Math.random() * 1.8 + 0.4,
      dx:   (Math.random() - 0.5) * 0.4,
      dy:   -Math.random() * 0.5 - 0.2,
      o:    Math.random() * 0.5 + 0.1,
    }));
  }

  function draw() {
    ctx.clearRect(0, 0, w, h);

    particles.forEach(p => {
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
      ctx.fillStyle = `rgba(255,107,0,${p.o})`;
      ctx.fill();

      p.x += p.dx;
      p.y += p.dy;

      if (p.y < -5) { p.y = h + 5; p.x = Math.random() * w; }
      if (p.x < -5) p.x = w + 5;
      if (p.x > w + 5) p.x = -5;
    });

    requestAnimationFrame(draw);
  }

  resize();
  createParticles();
  draw();

  window.addEventListener('resize', () => { resize(); createParticles(); });
})();


/* ============================================
   FEATURE CARDS — stagger on scroll
   ============================================ */
(function featureStagger() {
  const section = document.querySelector('#por-que-nos');
  if (!section) return;

  const cards = section.querySelectorAll('.feature-card');
  cards.forEach(c => {
    c.style.opacity   = '0';
    c.style.transform = 'translateY(30px)';
    c.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
  });

  const obs = new IntersectionObserver(([entry]) => {
    if (entry.isIntersecting) {
      cards.forEach((c, i) => {
        setTimeout(() => {
          c.style.opacity   = '1';
          c.style.transform = 'translateY(0)';
        }, i * 90);
      });
      obs.disconnect();
    }
  }, { threshold: 0.1 });

  obs.observe(section);
})();


/* ============================================
   STEPS — stagger on scroll
   ============================================ */
(function stepStagger() {
  const section = document.querySelector('#como-funciona');
  if (!section) return;

  const steps = section.querySelectorAll('.step-item');
  steps.forEach(s => {
    s.style.opacity   = '0';
    s.style.transform = 'translateY(30px)';
    s.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
  });

  const obs = new IntersectionObserver(([entry]) => {
    if (entry.isIntersecting) {
      steps.forEach((s, i) => {
        setTimeout(() => {
          s.style.opacity   = '1';
          s.style.transform = 'translateY(0)';
        }, i * 150);
      });
      obs.disconnect();
    }
  }, { threshold: 0.15 });

  obs.observe(section);
})();


/* ============================================
   TESTIMONIAL CARDS - stagger on scroll
   ============================================ */
(function testimonialStagger() {
  const section = document.querySelector('#depoimentos');
  if (!section) return;

  const cards = section.querySelectorAll('.testimonial-card');
  cards.forEach(c => {
    c.style.opacity   = '0';
    c.style.transform = 'translateY(30px)';
    c.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
  });

  const obs = new IntersectionObserver(([entry]) => {
    if (entry.isIntersecting) {
      cards.forEach((c, i) => {
        setTimeout(() => {
          c.style.opacity   = '1';
          c.style.transform = 'translateY(0)';
        }, i * 120);
      });
      obs.disconnect();
    }
  }, { threshold: 0.1 });

  obs.observe(section);
})();


/* ============================================
   WHATSAPP BUTTON — show tooltip after delay
   ============================================ */
setTimeout(() => {
  const tooltip = document.querySelector('.whatsapp-tooltip');
  if (!tooltip) return;
  tooltip.style.opacity = '1';
  tooltip.style.transform = 'translateX(0)';
  setTimeout(() => {
    tooltip.style.opacity = '';
    tooltip.style.transform = '';
  }, 4000);
}, 3000);


/* ============================================
   SERVICE CARDS MODAL (placeholder)
   ============================================ */
document.querySelectorAll('.service-learn-more').forEach(btn => {
  btn.addEventListener('click', (e) => {
    e.preventDefault();
    const card    = btn.closest('.service-card');
    const title   = card.querySelector('h3').textContent;
    const waLink  = `https://wa.me/5511999999999?text=Olá!%20Gostaria%20de%20mais%20informações%20sobre%20a%20solução%20${encodeURIComponent(title)}%20da%20SR%20Energia.`;
    window.open(waLink, '_blank');
  });
});


/* ============================================
   COPY PHONE ON CLICK
   ============================================ */
document.querySelectorAll('[data-copy]').forEach(el => {
  el.addEventListener('click', () => {
    const text = el.dataset.copy;
    navigator.clipboard.writeText(text).catch(() => {});
    const original = el.textContent;
    el.textContent = 'Copiado!';
    setTimeout(() => { el.textContent = original; }, 1500);
  });
});


/* ============================================
   INIT complete notification
   ============================================ */
console.log('%c SR ENERGIA %c Website carregado com sucesso! ☀️',
  'background:#FF6B00;color:#fff;padding:4px 8px;border-radius:4px 0 0 4px;font-weight:700;',
  'background:#1a1a2e;color:#FF6B00;padding:4px 8px;border-radius:0 4px 4px 0;font-weight:500;'
);
