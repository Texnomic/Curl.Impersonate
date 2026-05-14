/* ============================================================
   Texnomic.Curl.Impersonate — JavaScript
   Handles: Navigation, mobile menu, scroll animations
   ============================================================ */

document.addEventListener('DOMContentLoaded', () => {

    // --- Scrolled Navigation ---
    const nav = document.getElementById('nav');
    if (nav) {
        const handleScroll = () => {
            nav.classList.toggle('nav--scrolled', window.scrollY > 40);
        };
        window.addEventListener('scroll', handleScroll, { passive: true });
        handleScroll();
    }

    // --- Mobile Menu ---
    const mobileToggle = document.getElementById('mobileToggle');
    const mobileMenu = document.getElementById('mobileMenu');

    const resetHamburger = (spans) => {
        spans[0].style.transform = '';
        spans[1].style.opacity = '';
        spans[2].style.transform = '';
    };

    if (mobileToggle && mobileMenu) {
        mobileToggle.addEventListener('click', () => {
            const isOpen = mobileMenu.classList.toggle('mobile-menu--open');
            mobileToggle.setAttribute('aria-expanded', isOpen);
            const spans = mobileToggle.querySelectorAll('span');
            if (isOpen) {
                spans[0].style.transform = 'rotate(45deg) translate(5px, 5px)';
                spans[1].style.opacity = '0';
                spans[2].style.transform = 'rotate(-45deg) translate(5px, -5px)';
            } else {
                resetHamburger(spans);
            }
        });

        mobileMenu.querySelectorAll('.mobile-menu__link').forEach(link => {
            link.addEventListener('click', () => {
                mobileMenu.classList.remove('mobile-menu--open');
                mobileToggle.setAttribute('aria-expanded', 'false');
                resetHamburger(mobileToggle.querySelectorAll('span'));
            });
        });
    }

    // --- Scroll Reveal Animations ---
    const animatedSelector =
        '.section__header, .protocol-card, .feature-card, .platform-item, .arch-layer, .package-card, .step, .cta';

    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('visible');
                    observer.unobserve(entry.target);
                }
            });
        }, {
            root: null,
            rootMargin: '0px 0px -60px 0px',
            threshold: 0.1,
        });

        document.querySelectorAll(animatedSelector).forEach(el => observer.observe(el));
    } else {
        // Fallback: just show everything on older browsers
        document.querySelectorAll(animatedSelector).forEach(el => el.classList.add('visible'));
    }

    // --- Smooth Scroll for anchor links ---
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        const href = anchor.getAttribute('href');
        if (!href || href === '#') return;

        anchor.addEventListener('click', (e) => {
            const target = document.querySelector(href);
            if (target) {
                e.preventDefault();
                const offset = 80;
                const top = target.getBoundingClientRect().top + window.scrollY - offset;
                window.scrollTo({ top, behavior: 'smooth' });
            }
        });
    });

    // --- Floating particles around hero code window ---
    const heroCodeContainer = document.querySelector('.hero__code');
    if (heroCodeContainer && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        for (let i = 0; i < 20; i++) {
            const dot = document.createElement('div');
            const size = Math.random() * 3 + 1;
            dot.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                background: rgba(0, 212, 255, ${Math.random() * 0.5 + 0.1});
                border-radius: 50%;
                top: ${Math.random() * 100}%;
                left: ${Math.random() * 100}%;
                animation: floatParticle ${Math.random() * 10 + 8}s ease-in-out infinite;
                animation-delay: ${Math.random() * 5}s;
                pointer-events: none;
                z-index: 0;
            `;
            heroCodeContainer.appendChild(dot);
        }

        const style = document.createElement('style');
        style.textContent = `
            @keyframes floatParticle {
                0%, 100% { transform: translate(0, 0) scale(1); opacity: 0.3; }
                25% { transform: translate(${Math.random() * 30 - 15}px, ${Math.random() * 30 - 15}px) scale(1.2); opacity: 0.7; }
                50% { transform: translate(${Math.random() * 30 - 15}px, ${Math.random() * 30 - 15}px) scale(0.8); opacity: 0.5; }
                75% { transform: translate(${Math.random() * 30 - 15}px, ${Math.random() * 30 - 15}px) scale(1.1); opacity: 0.6; }
            }
        `;
        document.head.appendChild(style);
    }

    // --- Copy-to-clipboard for code blocks ---
    document.querySelectorAll('.code-window').forEach(window_ => {
        const body = window_.querySelector('.code-window__body');
        if (!body) return;

        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'code-window__copy';
        button.setAttribute('aria-label', 'Copy code');
        button.textContent = 'Copy';
        button.style.cssText = `
            position: absolute;
            top: 8px;
            right: 12px;
            padding: 4px 10px;
            background: rgba(0, 212, 255, 0.08);
            border: 1px solid rgba(0, 212, 255, 0.18);
            border-radius: 4px;
            color: var(--color-accent);
            font-family: var(--font-mono);
            font-size: 0.72rem;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.06em;
            cursor: pointer;
            opacity: 0;
            transition: opacity 0.2s ease, background 0.2s ease;
        `;

        const tabs = window_.querySelector('.code-window__tabs');
        (tabs || window_).style.position = 'relative';
        (tabs || window_).appendChild(button);

        window_.addEventListener('mouseenter', () => { button.style.opacity = '1'; });
        window_.addEventListener('mouseleave', () => { button.style.opacity = '0'; });

        button.addEventListener('click', async () => {
            const text = body.innerText.trim();
            try {
                await navigator.clipboard.writeText(text);
                const original = button.textContent;
                button.textContent = 'Copied';
                button.style.background = 'rgba(34, 197, 94, 0.15)';
                button.style.borderColor = 'rgba(34, 197, 94, 0.35)';
                button.style.color = 'var(--color-success)';
                setTimeout(() => {
                    button.textContent = original;
                    button.style.background = 'rgba(0, 212, 255, 0.08)';
                    button.style.borderColor = 'rgba(0, 212, 255, 0.18)';
                    button.style.color = 'var(--color-accent)';
                }, 1400);
            } catch {
                button.textContent = 'Failed';
                setTimeout(() => { button.textContent = 'Copy'; }, 1400);
            }
        });
    });
});
