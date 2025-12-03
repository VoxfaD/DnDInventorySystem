// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("history-sidebar");
    const toggle = document.getElementById("history-toggle");
    const closeBtn = document.querySelector(".history-close");

    if (sidebar && toggle) {
        toggle.addEventListener("click", () => {
            sidebar.classList.toggle("open");
        });
    }

    if (sidebar && closeBtn) {
        closeBtn.addEventListener("click", () => sidebar.classList.remove("open"));
    }
});
