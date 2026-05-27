(() => {
    const page = document.querySelector("[data-topic-selection-page]");

    if (!page) {
        return;
    }

    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    const requestToken = tokenInput ? tokenInput.value : "";
    const selectionMessage = document.querySelector("[data-topic-selection-message]");

    const findTopicCard = topicId => {
        const topicIdValue = String(topicId);
        return Array.from(document.querySelectorAll("[data-topic-card]"))
            .find(card => card.dataset.topicId === topicIdValue);
    };

    const getBatch = element => element ? element.closest("[data-topic-batch-id]") : null;
    const getBatchCards = batch => batch ? Array.from(batch.querySelectorAll("[data-topic-card]")) : [];
    const getSelectedTopicId = batch => batch ? (batch.dataset.selectedTopicId || "") : "";

    const setSelectedTopicId = (batch, topicId) => {
        if (batch) {
            batch.dataset.selectedTopicId = topicId ? String(topicId) : "";
        }
    };

    const showSelectionMessage = message => {
        if (!selectionMessage || !message) {
            return;
        }

        selectionMessage.textContent = message;
        selectionMessage.classList.add("is-visible");
    };

    const setSelectButtonReady = button => {
        if (!button) {
            return;
        }

        button.disabled = false;
        button.textContent = "Select Topic";
    };

    const updateBatchSelectionText = (batch, title) => {
        if (!batch) {
            return;
        }

        const text = batch.querySelector(".topics-card__text");

        if (!text) {
            return;
        }

        let selection = text.querySelector(".topic-batch__selection");

        if (!title) {
            if (selection) {
                selection.remove();
            }

            return;
        }

        if (!selection) {
            selection = document.createElement("span");
            selection.className = "topic-batch__selection";
            text.appendChild(selection);
        }

        selection.textContent = `Selected: ${title}`;
    };

    const updateBatchCounts = batch => {
        if (!batch) {
            return;
        }

        const cards = getBatchCards(batch);

        if (cards.length === 0) {
            return;
        }

        const takenCount = cards.filter(card => card.classList.contains("is-taken")).length;
        const availableCount = cards.length - takenCount;
        const availableTarget = batch.querySelector('[data-topic-count="available"]');
        const takenTarget = batch.querySelector('[data-topic-count="taken"]');

        if (availableTarget) {
            availableTarget.textContent = String(availableCount);
        }

        if (takenTarget) {
            takenTarget.textContent = String(takenCount);
        }
    };

    const refreshBatchButtons = batch => {
        const selectedTopicId = getSelectedTopicId(batch);

        getBatchCards(batch).forEach(card => {
            const isSelectedCard = card.dataset.topicId === selectedTopicId;
            const selectButton = card.querySelector("[data-select-topic]");
            const releaseButton = card.querySelector("[data-release-topic]");
            const isTaken = card.classList.contains("is-taken");

            if (releaseButton) {
                releaseButton.classList.toggle("is-hidden", !isSelectedCard);
                releaseButton.disabled = false;
                releaseButton.textContent = "Release Topic";
            }

            if (!selectButton) {
                return;
            }

            if (isSelectedCard) {
                selectButton.disabled = true;
                selectButton.textContent = "Selected";
                selectButton.classList.add("is-hidden");
                return;
            }

            selectButton.classList.remove("is-hidden");

            if (isTaken) {
                selectButton.disabled = true;
                selectButton.textContent = "Select Topic";
                return;
            }

            if (selectedTopicId) {
                selectButton.disabled = true;
                selectButton.textContent = "Already selected";
                return;
            }

            setSelectButtonReady(selectButton);
        });
    };

    const markTopicTaken = topicId => {
        const card = findTopicCard(topicId);

        if (!card) {
            return;
        }

        const badge = card.querySelector("[data-topic-status]");
        const batch = getBatch(card);

        card.classList.add("is-taken");

        if (badge && !card.classList.contains("is-selected")) {
            badge.textContent = "Filled";
            badge.classList.remove("topics-badge--live", "topics-badge--selected");
            badge.classList.add("topics-badge--taken");
        }

        updateBatchCounts(batch);
        refreshBatchButtons(batch);
    };

    const markTopicSelected = (topicId, message) => {
        const card = findTopicCard(topicId);

        if (!card) {
            showSelectionMessage(message);
            return;
        }

        const batch = getBatch(card);
        const badge = card.querySelector("[data-topic-status]");
        const title = card.querySelector("h4")?.textContent?.trim() || "Selected topic";

        setSelectedTopicId(batch, topicId);
        card.classList.add("is-taken", "is-selected");

        if (badge) {
            badge.textContent = "Selected";
            badge.classList.remove("topics-badge--live", "topics-badge--taken");
            badge.classList.add("topics-badge--selected");
        }

        updateBatchSelectionText(batch, title);
        updateBatchCounts(batch);
        refreshBatchButtons(batch);
        showSelectionMessage(message || "Topic selected successfully.");
    };

    const markTopicAvailable = (topicId, message) => {
        const topicIdValue = String(topicId);
        const card = findTopicCard(topicIdValue);

        if (!card) {
            return;
        }

        const batch = getBatch(card);
        const badge = card.querySelector("[data-topic-status]");
        const wasBatchSelection = getSelectedTopicId(batch) === topicIdValue;

        card.classList.remove("is-taken", "is-selected");

        if (badge) {
            badge.textContent = "Available";
            badge.classList.remove("topics-badge--taken", "topics-badge--selected");
            badge.classList.add("topics-badge--live");
        }

        if (wasBatchSelection) {
            setSelectedTopicId(batch, "");
            updateBatchSelectionText(batch, "");
        }

        refreshBatchButtons(batch);
        updateBatchCounts(batch);

        if (wasBatchSelection) {
            showSelectionMessage(message || "Topic released. You can select another topic in this group.");
        }
    };

    const setButtonBusy = (button, isBusy) => {
        button.disabled = isBusy;
        button.textContent = isBusy ? "Selecting..." : "Select Topic";
    };

    const setReleaseButtonBusy = (button, isBusy) => {
        button.disabled = isBusy;
        button.textContent = isBusy ? "Releasing..." : "Release Topic";
    };

    document.addEventListener("click", async event => {
        const button = event.target.closest("[data-select-topic]");

        if (!button || button.disabled) {
            return;
        }

        const card = button.closest("[data-topic-card]");
        const batch = getBatch(card);

        if (getSelectedTopicId(batch)) {
            return;
        }

        const topicId = card ? card.dataset.topicId : "";
        const body = new URLSearchParams();

        body.append("id", topicId);
        body.append("__RequestVerificationToken", requestToken);

        setButtonBusy(button, true);

        try {
            const response = await fetch(button.dataset.selectUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": requestToken
                },
                body
            });

            let result = {};

            try {
                result = await response.json();
            } catch {
                result = {};
            }

            if (response.ok) {
                markTopicSelected(result.topicId || topicId, result.message);
                return;
            }

            if (response.status === 409 && result.code === "TopicTaken") {
                markTopicTaken(topicId);
                showSelectionMessage(result.message);
                return;
            }

            if (response.status === 409 && result.code === "AlreadySelectedTopic") {
                markTopicSelected(result.topicId, result.message);
                return;
            }

            setButtonBusy(button, false);
            showSelectionMessage(result.message || "Unable to select this topic. Please try again.");
        } catch {
            setButtonBusy(button, false);
            showSelectionMessage("Unable to select this topic. Please try again.");
        }
    });

    document.addEventListener("click", async event => {
        const button = event.target.closest("[data-release-topic]");

        if (!button || button.disabled) {
            return;
        }

        const card = button.closest("[data-topic-card]");
        const topicId = card ? card.dataset.topicId : "";
        const body = new URLSearchParams();

        body.append("id", topicId);
        body.append("__RequestVerificationToken", requestToken);

        setReleaseButtonBusy(button, true);

        try {
            const response = await fetch(button.dataset.releaseUrl, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": requestToken
                },
                body
            });

            let result = {};

            try {
                result = await response.json();
            } catch {
                result = {};
            }

            if (response.ok || (response.status === 409 && result.code === "TopicAvailable")) {
                markTopicAvailable(result.topicId || topicId, result.message || "Topic released. You can select another topic in this group.");
                return;
            }

            setReleaseButtonBusy(button, false);
            showSelectionMessage(result.message || "Unable to release this topic. Please try again.");
        } catch {
            setReleaseButtonBusy(button, false);
            showSelectionMessage("Unable to release this topic. Please try again.");
        }
    });

    const handleTopicTaken = topicId => {
        if (topicId) {
            markTopicTaken(topicId);
        }
    };

    const handleTopicAvailable = topicId => {
        if (topicId) {
            markTopicAvailable(topicId);
        }
    };

    const startNativeSignalR = async () => {
        const recordSeparator = String.fromCharCode(0x1e);

        try {
            const negotiateResponse = await fetch("/collaborationHub/negotiate?negotiateVersion=1", {
                method: "POST",
                credentials: "same-origin"
            });

            if (!negotiateResponse.ok) {
                return;
            }

            const negotiate = await negotiateResponse.json();
            const connectionToken = negotiate.connectionToken || negotiate.connectionId;

            if (!connectionToken) {
                return;
            }

            const socketProtocol = window.location.protocol === "https:" ? "wss:" : "ws:";
            const socketUrl = `${socketProtocol}//${window.location.host}/collaborationHub?id=${encodeURIComponent(connectionToken)}`;
            const socket = new WebSocket(socketUrl);
            let pingTimer = null;

            socket.addEventListener("open", () => {
                socket.send(JSON.stringify({ protocol: "json", version: 1 }) + recordSeparator);
                pingTimer = window.setInterval(() => {
                    if (socket.readyState === WebSocket.OPEN) {
                        socket.send(JSON.stringify({ type: 6 }) + recordSeparator);
                    }
                }, 15000);
            });

            socket.addEventListener("message", event => {
                String(event.data).split(recordSeparator).filter(Boolean).forEach(frame => {
                    try {
                        const message = JSON.parse(frame);

                        if (message.type === 1 && message.target === "TopicTaken") {
                            handleTopicTaken(message.arguments && message.arguments[0]);
                        }

                        if (message.type === 1 && message.target === "TopicAvailable") {
                            handleTopicAvailable(message.arguments && message.arguments[0]);
                        }
                    } catch {
                        // Ignore keep-alive and handshake frames.
                    }
                });
            });

            socket.addEventListener("close", () => {
                if (pingTimer) {
                    window.clearInterval(pingTimer);
                }

                window.setTimeout(startNativeSignalR, 3000);
            });
        } catch {
            window.setTimeout(startNativeSignalR, 3000);
        }
    };

    const startRealtime = () => {
        if (window.signalR && window.signalR.HubConnectionBuilder) {
            const connection = new window.signalR.HubConnectionBuilder()
                .withUrl("/collaborationHub")
                .withAutomaticReconnect()
                .build();

            connection.on("TopicTaken", handleTopicTaken);
            connection.on("TopicAvailable", handleTopicAvailable);
            connection.start().catch(() => startNativeSignalR());
            return;
        }

        startNativeSignalR();
    };

    document.querySelectorAll("[data-topic-batch-id]").forEach(batch => {
        updateBatchCounts(batch);
        refreshBatchButtons(batch);
    });
    startRealtime();
})();
