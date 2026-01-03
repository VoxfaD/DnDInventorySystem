
document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("history-sidebar");
    const toggle = document.getElementById("history-toggle");
    const closeBtn = document.querySelector(".history-close");
    const prevBtn = document.querySelector(".history-prev");
    const nextBtn = document.querySelector(".history-next");
    const pageLabel = document.querySelector(".history-page-label");
    const historyEntries = Array.from(document.querySelectorAll(".history-list .history-entry"));

    if (sidebar && toggle) {
        toggle.addEventListener("click", () => {
            sidebar.classList.toggle("open");
            toggle.classList.toggle("open");
        });
    }

    if (sidebar && closeBtn) {
        closeBtn.addEventListener("click", () => {
            sidebar.classList.remove("open");
            if (toggle) toggle.classList.remove("open");
        });
    }

    historyEntries.forEach((entry) => (entry.style.display = ""));

    document.querySelectorAll(".paged-table").forEach((table) => {
        const pageSize = parseInt(table.dataset.pageSize || "10", 10);
        const rows = Array.from(table.querySelectorAll("tbody tr"));
        const pager = document.querySelector(`.table-pager[data-table-id="${table.id}"]`);
        const prev = pager?.querySelector(".table-prev");
        const next = pager?.querySelector(".table-next");
        const label = pager?.querySelector(".table-page-label");
        let page = 1;

        const renderTablePage = () => {
            const totalPages = Math.max(1, Math.ceil(rows.length / pageSize));
            page = Math.min(page, totalPages);
            rows.forEach((row, idx) => {
                const start = (page - 1) * pageSize;
                const end = page * pageSize;
                row.style.display = idx >= start && idx < end ? "" : "none";
            });
            if (label) label.textContent = `Page ${page} of ${totalPages}`;
            if (prev) prev.disabled = page <= 1;
            if (next) next.disabled = page >= totalPages;
        };

        prev?.addEventListener("click", () => {
            page = Math.max(1, page - 1);
            renderTablePage();
        });

        next?.addEventListener("click", () => {
            page = Math.min(Math.ceil(rows.length / pageSize), page + 1);
            renderTablePage();
        });

        renderTablePage();
    });

    document.querySelectorAll(".assign-table .assign-row").forEach((row) => {
        row.addEventListener("click", (e) => {
            if (e.target.closest("input, button, select, textarea, label, a")) {
                return;
            }
            const idx = row.dataset.rowIndex;
            const hidden = document.querySelector(`.assignment-selected[data-row-index="${idx}"]`);
            const isSelected = row.classList.toggle("selected");
            if (hidden) {
                hidden.value = isSelected ? "true" : "false";
            }
        });
    });

    document.querySelectorAll(".no-row-toggle").forEach((el) => {
        el.addEventListener("click", (e) => e.stopPropagation());
    });

    document.querySelectorAll("tr.table-row-link").forEach((row) => {
        row.addEventListener("click", (e) => {
            if (e.target.closest("a, button, form, input, label, textarea, select, .no-row-toggle")) {
                return;
            }
            const target = row.dataset.bsTarget || row.dataset.target;
            const href = row.dataset.href;
            if (target && target.startsWith("#")) {
                const collapseEl = document.querySelector(target);
                if (collapseEl) {
                    const instance = bootstrap.Collapse.getOrCreateInstance(collapseEl, { toggle: false });
                    if (collapseEl.classList.contains("show")) {
                        instance.hide();
                    } else {
                        instance.show();
                    }
                }
            } else if (href && href !== "#") {
                window.location = href;
            }
        });
    });

    document.querySelectorAll(".privilege-toggle").forEach((btn) => {
        btn.addEventListener("click", () => {
            const privilege = btn.dataset.privilege;
            const hiddenContainerId = btn.dataset.hiddenContainer;
            if (!privilege || !hiddenContainerId) return;
            const container = document.getElementById(hiddenContainerId);
            if (!container) return;

            btn.classList.toggle("active");
            const existing = container.querySelectorAll(`input[type="hidden"][name="SelectedPrivileges"][value="${privilege}"]`);
            if (btn.classList.contains("active")) {
                if (existing.length === 0) {
                    const input = document.createElement("input");
                    input.type = "hidden";
                    input.name = "SelectedPrivileges";
                    input.value = privilege;
                    container.appendChild(input);
                }
            } else {
                existing.forEach((el) => el.remove());
            }
        });
    });
});
