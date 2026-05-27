(() => {
    const feedback = document.getElementById('settings-feedback');
    const toggles = document.querySelectorAll('[data-password-toggle]');

    if (feedback) {
        window.requestAnimationFrame(() => {
            feedback.scrollIntoView({ behavior: 'smooth', block: 'center' });
        });
    }

    toggles.forEach(toggle => {
        toggle.addEventListener('click', () => {
            const targetId = toggle.getAttribute('data-target');
            const input = document.getElementById(targetId);

            if (!input) {
                return;
            }

            const showPassword = input.type === 'password';
            input.type = showPassword ? 'text' : 'password';
            toggle.classList.toggle('is-active', showPassword);
            toggle.setAttribute('aria-label', showPassword ? 'Hide password' : 'Show password');
        });
    });
})();
