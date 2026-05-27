(() => {
    const root = document.querySelector("[data-notification-dropdown]");

    if (!root) {
        return;
    }

    const list = root.querySelector("[data-notification-list]");
    const countBadge = root.querySelector("[data-notification-count]");
    const stateLabel = root.querySelector("[data-notification-state]");
    const token = document.querySelector('.notification-token input[name="__RequestVerificationToken"]')?.value || "";
    const recordSeparator = String.fromCharCode(0x1e);
    let isMarkingRead = false;

    const escapeHtml = value => String(value || "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");

    const getTone = type => {
        const normalized = String(type || "").toLowerCase();

        if (normalized.includes("task") || normalized.includes("deadline")) {
            return "task";
        }

        if (normalized.includes("topic")) {
            return "topic";
        }

        if (normalized.includes("invitation") || normalized.includes("group")) {
            return "group";
        }

        if (normalized.includes("feedback")) {
            return "feedback";
        }

        return "system";
    };

    const setState = text => {
        if (stateLabel) {
            stateLabel.textContent = text;
        }
    };

    const renderCount = count => {
        if (!countBadge) {
            return;
        }

        const value = Number(count || 0);
        countBadge.hidden = value <= 0;
        countBadge.textContent = String(value);
    };

    const renderEmpty = message => {
        if (list) {
            list.innerHTML = `<div class="notification-menu__empty">${escapeHtml(message)}</div>`;
        }
    };

    const normalizeNavigationUrl = value => {
        const fallback = "/Notifications";
        const raw = String(value || "").trim();

        if (!raw) {
            return fallback;
        }

        try {
            const resolved = new URL(raw, window.location.origin);

            if (resolved.origin !== window.location.origin) {
                return fallback;
            }

            return `${resolved.pathname}${resolved.search}${resolved.hash}` || fallback;
        } catch {
            return fallback;
        }
    };

    const renderItems = data => {
        const items = Array.isArray(data.items) ? data.items : [];

        renderCount(data.unreadCount);

        if (!items.length) {
            renderEmpty("No notifications yet.");
            setState("Live");
            return;
        }

        list.innerHTML = items.map(item => {
            const tone = getTone(item.type);
            const status = String(item.status || "").toLowerCase();
            const readStateClass = status === "unread"
                ? "notification-menu__item--unread"
                : "notification-menu__item--read";
            const invitationStatus = item.invitationStatus
                ? `<span class="notification-menu__status">${escapeHtml(item.invitationStatus)}</span>`
                : "";
            const actions = item.requiresResponse && item.id
                ? `<div class="notification-menu__actions">
                        <button type="button" class="dashboard-card__button dashboard-card__button--primary" data-notification-action="accept" data-notification-id="${escapeHtml(item.id)}">Accept</button>
                        <button type="button" class="dashboard-card__button" data-notification-action="decline" data-notification-id="${escapeHtml(item.id)}">Decline</button>
                   </div>`
                : "";
            const cardRole = actions ? "group" : "button";

            return `<article class="notification-menu__item notification-menu__item--${tone} ${readStateClass}" data-notification-id="${escapeHtml(item.id || "")}" data-notification-url="${escapeHtml(item.url || "/Notifications")}" data-notification-status="${escapeHtml(status || "read")}" tabindex="0" role="${cardRole}">
                        <span class="notification-menu__dot" aria-hidden="true"></span>
                        <div>
                            <div class="notification-menu__title">
                                <strong>${escapeHtml(item.title)}</strong>
                                ${invitationStatus}
                            </div>
                            <p>${escapeHtml(item.message)}</p>
                            <time>${escapeHtml(item.createdAt)}</time>
                            ${actions}
                        </div>
                    </article>`;
        }).join("");

        setState("Live");
    };

    const markNotificationsRead = async () => {
        const currentUnreadCount = Number(countBadge?.textContent || 0);

        if (isMarkingRead || currentUnreadCount <= 0) {
            return;
        }

        isMarkingRead = true;

        try {
            const body = new URLSearchParams();
            body.append("__RequestVerificationToken", token);

            const response = await fetch("/Notifications/MarkSeen", {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": token
                },
                body
            });

            if (response.ok) {
                renderCount(0);
                list?.querySelectorAll(".notification-menu__item--unread").forEach(item => {
                    item.classList.remove("notification-menu__item--unread");
                    item.classList.add("notification-menu__item--read");
                    item.dataset.notificationStatus = "read";
                });
                setState("Live");
            }
        } catch {
            setState("Reconnecting");
        } finally {
            isMarkingRead = false;
        }
    };

    const refreshNotifications = async () => {
        try {
            const response = await fetch("/Notifications/Latest", {
                credentials: "same-origin",
                headers: { "Accept": "application/json" }
            });

            if (!response.ok) {
                renderCount(0);
                renderEmpty("Sign in to see updates.");
                setState("Offline");
                return;
            }

            renderItems(await response.json());
        } catch {
            setState("Reconnecting");
        }
    };

    const postInvitationResponse = async (action, id, button) => {
        const urlAction = action === "accept" ? "AcceptInvitation" : "DeclineInvitation";
        const body = new URLSearchParams();

        body.append("__RequestVerificationToken", token);

        button.disabled = true;

        try {
            await fetch(`/Notifications/${urlAction}/${encodeURIComponent(id)}`, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": token
                },
                body
            });

            await refreshNotifications();
        } catch {
            button.disabled = false;
            setState("Action failed");
        }
    };

    const navigateToNotification = async item => {
        if (!item) {
            window.location.assign("/Notifications");
            return;
        }

        const id = item.dataset.notificationId;
        let url = normalizeNavigationUrl(item.dataset.notificationUrl);

        if (id) {
            try {
                const response = await fetch(url, {
                    credentials: "same-origin",
                    headers: {
                        "Accept": "application/json",
                        "X-Requested-With": "XMLHttpRequest"
                    }
                });

                if (response.ok) {
                    const result = await response.json();
                    url = normalizeNavigationUrl(result.url);
                } else {
                    url = "/Notifications";
                }
            } catch {
                url = "/Notifications";
            }
        }

        window.location.assign(normalizeNavigationUrl(url));
    };

    root.addEventListener("click", event => {
        const button = event.target.closest("[data-notification-action]");

        if (!button || button.disabled) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        postInvitationResponse(button.dataset.notificationAction, button.dataset.notificationId, button);
    });

    list?.addEventListener("click", event => {
        if (event.target.closest("[data-notification-action]")) {
            return;
        }

        const item = event.target.closest("[data-notification-url]");

        if (!item) {
            return;
        }

        event.preventDefault();
        navigateToNotification(item);
    });

    list?.addEventListener("keydown", event => {
        if (event.key !== "Enter" && event.key !== " ") {
            return;
        }

        if (event.target.closest("[data-notification-action]")) {
            return;
        }

        const item = event.target.closest("[data-notification-url]");

        if (!item) {
            return;
        }

        event.preventDefault();
        navigateToNotification(item);
    });

    root.addEventListener("shown.bs.dropdown", () => {
        markNotificationsRead();
    });

    const startNativeSignalR = async () => {
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
                setState("Live");
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
                        const realtimeTargets = ["NotificationsUpdated", "TopicTaken", "TopicAvailable"];

                        if (message.type === 1 && realtimeTargets.includes(message.target)) {
                            refreshNotifications();
                        }
                    } catch {
                        // SignalR keep-alive and handshake frames do not need UI work.
                    }
                });
            });

            socket.addEventListener("close", () => {
                if (pingTimer) {
                    window.clearInterval(pingTimer);
                }

                setState("Reconnecting");
                window.setTimeout(startNativeSignalR, 3000);
            });
        } catch {
            setState("Reconnecting");
            window.setTimeout(startNativeSignalR, 3000);
        }
    };

    const startRealtime = () => {
        if (window.signalR && window.signalR.HubConnectionBuilder) {
            const connection = new window.signalR.HubConnectionBuilder()
                .withUrl("/collaborationHub")
                .withAutomaticReconnect()
                .build();

            connection.on("NotificationsUpdated", refreshNotifications);
            connection.on("TopicTaken", refreshNotifications);
            connection.on("TopicAvailable", refreshNotifications);
            connection.start().then(() => setState("Live")).catch(() => startNativeSignalR());
            return;
        }

        startNativeSignalR();
    };

    refreshNotifications();
    startRealtime();
})();
