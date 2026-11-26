// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', () => {
    const toggle = document.getElementById('searchToggle');
    const search = document.getElementById('quickSearch');
    if (!toggle || !search) return;

    toggle.addEventListener('click', () => {
        search.classList.toggle('is-open');
        if (search.classList.contains('is-open')) search.querySelector('input')?.focus();
    });
});
