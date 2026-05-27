(() => {
    const modal = document.getElementById('profilePhotoModal');
    const openButtons = document.querySelectorAll('[data-photo-modal-open]');
    const closeButtons = document.querySelectorAll('[data-photo-modal-close]');
    const fileInput = document.getElementById('photoFile');
    const fileName = document.getElementById('photoFileName');
    const defaultText = 'JPG, PNG, GIF, or WEBP up to 5 MB.';

    if (fileInput && fileName) {
        fileInput.addEventListener('change', () => {
            fileName.textContent = fileInput.files && fileInput.files.length > 0
                ? fileInput.files[0].name
                : defaultText;
        });
    }

    if (!modal) {
        return;
    }

    const openModal = () => {
        modal.classList.add('is-visible');
        modal.setAttribute('aria-hidden', 'false');
    };

    const closeModal = () => {
        modal.classList.remove('is-visible');
        modal.setAttribute('aria-hidden', 'true');

        if (fileInput) {
            fileInput.value = '';
        }

        if (fileName) {
            fileName.textContent = defaultText;
        }
    };

    openButtons.forEach(button => button.addEventListener('click', openModal));
    closeButtons.forEach(button => button.addEventListener('click', closeModal));

    modal.addEventListener('click', event => {
        if (event.target === modal) {
            closeModal();
        }
    });

    document.addEventListener('keydown', event => {
        if (event.key === 'Escape' && modal.classList.contains('is-visible')) {
            closeModal();
        }
    });
})();
