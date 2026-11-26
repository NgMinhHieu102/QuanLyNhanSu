document.addEventListener('DOMContentLoaded', () => {
    const list = document.getElementById('payroll-list');
    if (!list) return;
    const rows = [...list.querySelectorAll('tbody > tr')];
    const size = document.getElementById('payroll-page-size');
    const previous = document.getElementById('payroll-prev');
    const next = document.getElementById('payroll-next');
    let page = 1;
    const render = () => {
        const count = size.value === 'all' ? Math.max(1, rows.length) : Number(size.value);
        const pages = Math.max(1, Math.ceil(rows.length / count));
        page = Math.max(1, Math.min(page, pages));
        const start = (page - 1) * count;
        rows.forEach((row, index) => { row.hidden = index < start || index >= start + count; });
        document.getElementById('payroll-pagination').hidden = rows.length === 0;
        document.getElementById('payroll-range').textContent = `Hiển thị ${rows.length ? start + 1 : 0}–${Math.min(start + count, rows.length)} / ${rows.length} nhân viên`;
        document.getElementById('payroll-page').textContent = `Trang ${page} / ${pages}`;
        previous.disabled = page === 1;
        next.disabled = page === pages;
    };
    size.addEventListener('change', () => { page = 1; render(); });
    previous.addEventListener('click', () => { page--; render(); });
    next.addEventListener('click', () => { page++; render(); });
    list.querySelectorAll('[data-payroll-employee]').forEach(button => {
        button.addEventListener('click', () => {
            const select = document.querySelector('select[name="employeeId"]');
            select.value = button.dataset.payrollEmployee;
            select.scrollIntoView({ behavior: 'smooth', block: 'center' });
            select.focus({ preventScroll: true });
        });
    });
    render();
});
