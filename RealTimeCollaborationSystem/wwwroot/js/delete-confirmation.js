(() => {
    const modalElement = document.getElementById("deleteConfirmationModal");

    if (!modalElement || !window.bootstrap) {
        return;
    }

    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const titleElement = modalElement.querySelector("[data-delete-confirm-title]");
    const itemElement = modalElement.querySelector("[data-delete-confirm-item]");
    const messageElement = modalElement.querySelector("[data-delete-confirm-message]");
    const confirmButton = modalElement.querySelector("[data-delete-confirm-submit]");
    const defaultConfirmText = confirmButton?.textContent || "Delete";
    let pendingForm = null;
    let pendingBusyText = "Deleting...";

    const setText = (element, text, fallback = "") => {
        if (!element) {
            return;
        }

        const value = (text || fallback).trim();
        element.textContent = value;
        element.hidden = !value;
    };

    document.addEventListener("submit", event => {
        const form = event.target.closest("[data-delete-confirm]");

        if (!form || form.dataset.deleteConfirmed === "true") {
            return;
        }

        event.preventDefault();
        pendingForm = form;

        setText(titleElement, form.dataset.deleteTitle, "Delete item?");
        setText(itemElement, form.dataset.deleteItem);
        setText(
            messageElement,
            form.dataset.deleteMessage,
            "This item will be permanently deleted. This action cannot be undone."
        );

        if (confirmButton) {
            confirmButton.disabled = false;
            confirmButton.textContent = form.dataset.deleteConfirmText || defaultConfirmText;
        }

        pendingBusyText = form.dataset.deleteConfirmBusyText || "Deleting...";
        modal.show();
    });

    confirmButton?.addEventListener("click", () => {
        if (!pendingForm) {
            return;
        }

        pendingForm.dataset.deleteConfirmed = "true";
        confirmButton.disabled = true;
        confirmButton.textContent = pendingBusyText;
        modal.hide();

        if (typeof pendingForm.requestSubmit === "function") {
            pendingForm.requestSubmit();
        } else {
            pendingForm.submit();
        }
    });

    modalElement.addEventListener("hidden.bs.modal", () => {
        if (pendingForm?.dataset.deleteConfirmed === "true") {
            return;
        }

        pendingForm = null;
        pendingBusyText = "Deleting...";
        if (confirmButton) {
            confirmButton.disabled = false;
            confirmButton.textContent = defaultConfirmText;
        }
    });
})();
