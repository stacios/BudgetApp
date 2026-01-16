// BudgetManager JavaScript - Enhanced UI

// ============================================
// Dark Mode Toggle
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    const themeToggle = document.getElementById('themeToggle');
    
    if (themeToggle) {
        themeToggle.addEventListener('click', function() {
            const currentTheme = document.documentElement.getAttribute('data-theme');
            const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
            
            document.documentElement.setAttribute('data-theme', newTheme);
            localStorage.setItem('theme', newTheme);
            
            // Update Chart.js colors if charts exist
            updateChartColors(newTheme);
        });
    }
});

// Update Chart.js colors based on theme
function updateChartColors(theme) {
    const textColor = theme === 'dark' ? '#f1f5f9' : '#64748b';
    const gridColor = theme === 'dark' ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.1)';
    
    if (typeof Chart !== 'undefined') {
        Chart.defaults.color = textColor;
        
        // Update all existing charts
        Chart.helpers.each(Chart.instances, function(chart) {
            if (chart.options.scales) {
                Object.keys(chart.options.scales).forEach(key => {
                    if (chart.options.scales[key].ticks) {
                        chart.options.scales[key].ticks.color = textColor;
                    }
                    if (chart.options.scales[key].grid) {
                        chart.options.scales[key].grid.color = gridColor;
                    }
                });
            }
            chart.update();
        });
    }
}

// ============================================
// Animated Number Counter
// ============================================
function animateCounter(element, target, duration = 1500, prefix = '', suffix = '') {
    const start = 0;
    const startTime = performance.now();
    const isNegative = target < 0;
    const absTarget = Math.abs(target);
    
    function easeOutExpo(t) {
        return t === 1 ? 1 : 1 - Math.pow(2, -10 * t);
    }
    
    function update(currentTime) {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);
        const easedProgress = easeOutExpo(progress);
        const current = start + (absTarget * easedProgress);
        
        const displayValue = isNegative ? -current : current;
        element.textContent = prefix + formatNumber(displayValue) + suffix;
        
        if (progress < 1) {
            requestAnimationFrame(update);
        } else {
            element.textContent = prefix + formatNumber(target) + suffix;
        }
    }
    
    requestAnimationFrame(update);
}

function formatNumber(num) {
    return new Intl.NumberFormat('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(num);
}

// Initialize counters on page load
function initializeCounters() {
    const counters = document.querySelectorAll('[data-counter]');
    
    counters.forEach(counter => {
        const target = parseFloat(counter.getAttribute('data-counter'));
        const prefix = counter.getAttribute('data-prefix') || '';
        const suffix = counter.getAttribute('data-suffix') || '';
        const duration = parseInt(counter.getAttribute('data-duration')) || 1500;
        
        // Use Intersection Observer for animation on scroll into view
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    animateCounter(counter, target, duration, prefix, suffix);
                    observer.unobserve(counter);
                }
            });
        }, { threshold: 0.1 });
        
        observer.observe(counter);
    });
}

document.addEventListener('DOMContentLoaded', initializeCounters);

// ============================================
// Initialize Bootstrap Components
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    // Tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    // Popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
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

// ============================================
// Utility Functions
// ============================================

// Format currency
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

// ============================================
// Chart.js Configuration
// ============================================
if (typeof Chart !== 'undefined') {
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
    const textColor = currentTheme === 'dark' ? '#f1f5f9' : '#64748b';
    
    Chart.defaults.font.family = "'Inter', 'Segoe UI', system-ui, -apple-system, sans-serif";
    Chart.defaults.color = textColor;
    Chart.defaults.plugins.legend.position = 'bottom';
    Chart.defaults.plugins.legend.labels.usePointStyle = true;
    Chart.defaults.plugins.legend.labels.padding = 15;
    
    // Add animation defaults
    Chart.defaults.animation = {
        duration: 1000,
        easing: 'easeOutQuart'
    };
}

// Create gradient for charts
function createGradient(ctx, colorStart, colorEnd) {
    const gradient = ctx.createLinearGradient(0, 0, 0, 400);
    gradient.addColorStop(0, colorStart);
    gradient.addColorStop(1, colorEnd);
    return gradient;
}

// ============================================
// Donut Chart Helper
// ============================================
function createDonutChart(canvasId, data, centerText) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return null;
    
    const ctx = canvas.getContext('2d');
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
    
    const chart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: data.labels,
            datasets: [{
                data: data.values,
                backgroundColor: data.colors || [
                    '#3b82f6', '#10b981', '#f59e0b', '#ef4444', 
                    '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16'
                ],
                borderWidth: 0,
                hoverOffset: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '70%',
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 15,
                        color: currentTheme === 'dark' ? '#f1f5f9' : '#64748b'
                    }
                },
                tooltip: {
                    backgroundColor: currentTheme === 'dark' ? '#1e293b' : '#ffffff',
                    titleColor: currentTheme === 'dark' ? '#f1f5f9' : '#1e293b',
                    bodyColor: currentTheme === 'dark' ? '#cbd5e1' : '#64748b',
                    borderColor: currentTheme === 'dark' ? '#334155' : '#e2e8f0',
                    borderWidth: 1,
                    padding: 12,
                    boxPadding: 6,
                    callbacks: {
                        label: function(context) {
                            const value = context.raw;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((value / total) * 100).toFixed(1);
                            return `${context.label}: $${value.toLocaleString('en-US', {minimumFractionDigits: 2})} (${percentage}%)`;
                        }
                    }
                }
            },
            animation: {
                animateRotate: true,
                animateScale: true,
                duration: 1500,
                easing: 'easeOutQuart'
            }
        }
    });
    
    return chart;
}

// ============================================
// Table Functions
// ============================================

// Sort table
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

// Export table to CSV
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

// ============================================
// Budget Calculations
// ============================================
function calculateDailyBudget(budget, daysInMonth, currentDay) {
    const remaining = budget.budgetAmount - budget.spentAmount;
    const remainingDays = daysInMonth - currentDay + 1;
    return remainingDays > 0 ? remaining / remainingDays : 0;
}

// Budget status colors
const statusColors = {
    ok: 'rgba(16, 185, 129, 0.5)',
    watch: 'rgba(245, 158, 11, 0.5)',
    over: 'rgba(239, 68, 68, 0.5)'
};

// ============================================
// Form Validation
// ============================================
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

// ============================================
// Progress Bar Animation
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    const progressBars = document.querySelectorAll('.progress-bar[data-width]');
    
    progressBars.forEach(bar => {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const width = bar.getAttribute('data-width');
                    bar.style.width = width;
                    observer.unobserve(bar);
                }
            });
        }, { threshold: 0.1 });
        
        bar.style.width = '0';
        observer.observe(bar);
    });
});
