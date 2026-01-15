// BudgetManager JavaScript

// Initialize Bootstrap tooltips
document.addEventListener('DOMContentLoaded', function() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
});

// Auto-dismiss alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function() {
    setTimeout(function() {
        var alerts = document.querySelectorAll('.alert-dismissible');
        alerts.forEach(function(alert) {
            var bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        });
    }, 5000);
});

// Format currency inputs
function formatCurrency(value) {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD'
    }).format(value);
}

// Confirm delete actions
function confirmDelete(message) {
    return confirm(message || 'Are you sure you want to delete this item?');
}

// Month navigation helper
function navigateToMonth(year, month) {
    const url = new URL(window.location.href);
    url.searchParams.set('year', year);
    url.searchParams.set('month', month);
    window.location.href = url.toString();
}

// Budget status colors
const statusColors = {
    ok: 'rgba(22, 163, 74, 0.5)',
    watch: 'rgba(217, 119, 6, 0.5)',
    over: 'rgba(220, 38, 38, 0.5)'
};

// Chart.js default configuration
if (typeof Chart !== 'undefined') {
    Chart.defaults.font.family = "'Segoe UI', system-ui, -apple-system, sans-serif";
    Chart.defaults.color = '#6b7280';
    Chart.defaults.plugins.legend.position = 'bottom';
}

// Table sorting functionality
function sortTable(table, column, type) {
    const tbody = table.querySelector('tbody');
    const rows = Array.from(tbody.querySelectorAll('tr'));
    
    const sortedRows = rows.sort((a, b) => {
        const aValue = a.cells[column].textContent.trim();
        const bValue = b.cells[column].textContent.trim();
        
        if (type === 'number') {
            return parseFloat(aValue.replace(/[^0-9.-]+/g, '')) - parseFloat(bValue.replace(/[^0-9.-]+/g, ''));
        } else if (type === 'date') {
            return new Date(aValue) - new Date(bValue);
        } else {
            return aValue.localeCompare(bValue);
        }
    });
    
    sortedRows.forEach(row => tbody.appendChild(row));
}

// Export data to CSV
function exportTableToCSV(table, filename) {
    const rows = Array.from(table.querySelectorAll('tr'));
    const csv = rows.map(row => {
        const cells = Array.from(row.querySelectorAll('th, td'));
        return cells.map(cell => `"${cell.textContent.trim().replace(/"/g, '""')}"`).join(',');
    }).join('\n');
    
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = filename || 'export.csv';
    link.click();
}

// Filter table rows
function filterTable(table, searchTerm) {
    const rows = table.querySelectorAll('tbody tr');
    const term = searchTerm.toLowerCase();
    
    rows.forEach(row => {
        const text = row.textContent.toLowerCase();
        row.style.display = text.includes(term) ? '' : 'none';
    });
}

// Budget pacing calculation
function calculateDailyBudget(budget, daysInMonth, currentDay) {
    const remaining = budget.budgetAmount - budget.spentAmount;
    const remainingDays = daysInMonth - currentDay + 1;
    return remainingDays > 0 ? remaining / remainingDays : 0;
}

// Form validation enhancement
document.addEventListener('DOMContentLoaded', function() {
    const forms = document.querySelectorAll('form[data-validate]');
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!form.checkValidity()) {
                e.preventDefault();
                e.stopPropagation();
            }
            form.classList.add('was-validated');
        });
    });
});
