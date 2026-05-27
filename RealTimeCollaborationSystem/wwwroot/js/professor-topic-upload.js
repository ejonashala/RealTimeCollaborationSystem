(() => {
    const form = document.querySelector("[data-topic-upload-form]");

    if (!form) {
        return;
    }

    const titleInput = form.querySelector("[data-topic-batch-title]");
    const topicsInput = form.querySelector("[data-topic-lines]");
    const previewPanel = document.querySelector("[data-topic-preview-panel]");
    const previewList = previewPanel?.querySelector("[data-topic-preview-list]");
    const previewEmpty = previewPanel?.querySelector("[data-topic-preview-empty]");
    const previewCount = previewPanel?.querySelector("[data-topic-preview-count]");
    const topicsError = form.querySelector("[data-topic-lines-error]");
    const modal = form.closest(".topic-create-modal");

    const getTopics = () => {
        return topicsInput.value
            .split(/\r?\n/)
            .map(topic => topic.trim())
            .filter(Boolean);
    };

    const renderPreview = () => {
        const topics = getTopics();

        if (!previewList || !previewEmpty || !previewCount) {
            return;
        }

        const shouldShowPreview = topics.length > 0;

        previewList.innerHTML = "";
        previewEmpty.hidden = topics.length > 0;
        previewCount.textContent = topics.length === 0
            ? "No topics yet"
            : `${topics.length} topic${topics.length === 1 ? "" : "s"} ready`;

        topics.forEach(topic => {
            const item = document.createElement("li");
            item.className = "topics-preview__item";
            item.textContent = topic;
            previewList.appendChild(item);
        });

        if (modal) {
            modal.classList.toggle("topic-create-modal--preview", shouldShowPreview);
        }

        if (previewPanel) {
            previewPanel.classList.toggle("is-visible", shouldShowPreview);
            previewPanel.setAttribute("aria-hidden", shouldShowPreview ? "false" : "true");
        }

        if (topics.length > 0 && topicsError) {
            topicsError.textContent = "";
        }
    };

    topicsInput.addEventListener("input", renderPreview);

    if (titleInput) {
        titleInput.addEventListener("input", () => {
            titleInput.setCustomValidity("");
        });
    }

    form.addEventListener("submit", event => {
        const topics = getTopics();

        if (titleInput && titleInput.value.trim().length === 0) {
            titleInput.setCustomValidity("Batch title is required.");
        }

        if (topics.length === 0) {
            event.preventDefault();
            if (topicsError) {
                topicsError.textContent = "At least one topic is required.";
            }
            topicsInput.focus();
        }
    });

    if (modal) {
        modal.addEventListener("hidden.bs.modal", () => {
            modal.classList.remove("topic-create-modal--preview");

            if (previewPanel) {
                previewPanel.classList.remove("is-visible");
                previewPanel.setAttribute("aria-hidden", "true");
            }
        });
    }

    renderPreview();
})();
