// Copy analytics data to clipboard in formatted text
function copyAnalyticsToClipboard() {
    let report = '=== PC2 ANALYTICS REPORT ===\n\n';
    report += 'Date Range: ' + analyticsData.startDate + ' to ' + analyticsData.endDate + ' (' + analyticsData.days + ' days)\n';
    report += 'Generated: ' + new Date().toLocaleString() + '\n\n';
    
    // Summary Statistics
    report += '--- SUMMARY STATISTICS ---\n';
    report += 'Total Page Views: ' + analyticsData.totalPageViews + '\n';
    report += 'Total PDF Downloads: ' + analyticsData.totalPdfDownloads + '\n';
    report += 'Total Searches: ' + analyticsData.totalSearches + '\n\n';
    
    // Page Views
    report += '--- TOP PAGE VIEWS ---\n';
    if (analyticsData.pageViews.length > 0) {
        analyticsData.pageViews.forEach(function(pv) {
            report += JSON.parse(pv.url) + ' - ' + pv.count + ' views\n';
        });
    } else {
        report += 'No page view data available\n';
    }
    report += '\n';
    
    // PDF Downloads
    report += '--- PDF DOWNLOADS ---\n';
    if (analyticsData.pdfDownloads.length > 0) {
        analyticsData.pdfDownloads.forEach(function(dl) {
            report += JSON.parse(dl.name) + ' - ' + dl.count + ' downloads\n';
        });
    } else {
        report += 'No PDF download data available\n';
    }
    report += '\n';
    
    // Search Terms
    report += '--- RESOURCE GUIDE SEARCH TERMS ---\n';
    if (analyticsData.searchTerms.length > 0) {
        analyticsData.searchTerms.forEach(function(st) {
            report += JSON.parse(st.type) + ': ' + JSON.parse(st.term) + ' - ' + st.count + ' searches\n';
        });
    } else {
        report += 'No search data available\n';
    }
    
    report += '\n=== END OF REPORT ===\n';
    
    // Copy to clipboard
    navigator.clipboard.writeText(report).then(function() {
        // Show success message
        const successMsg = document.getElementById('copySuccess');
        successMsg.style.display = 'inline';
        setTimeout(function() {
            successMsg.style.display = 'none';
        }, 3000);
    }).catch(function(err) {
        alert('Failed to copy to clipboard: ' + err);
    });
}

// Clear month/year fields when using date range
function clearMonthYear() {
    document.getElementById('month').value = '';
    document.getElementById('year').value = '';
}

// Clear date range fields when using month/year
function clearDateRange() {
    document.getElementById('startDate').value = '';
    document.getElementById('endDate').value = '';
}

// Set date range for quick filters
function setDateRange(days) {
    const today = new Date();
    const startDate = new Date(today);
    startDate.setDate(today.getDate() - days);
    
    document.getElementById('startDate').value = formatDate(startDate);
    document.getElementById('endDate').value = formatDate(today);
    
    // Switch to date range tab and clear month/year
    document.getElementById('date-range-tab').click();
    clearMonthYear();
    
    document.getElementById('filterForm').submit();
}

// Set current month
function setCurrentMonth() {
    const today = new Date();
    document.getElementById('month').value = today.getMonth() + 1;
    document.getElementById('year').value = today.getFullYear();
    
    // Switch to month/year tab and clear date range
    document.getElementById('month-year-tab').click();
    clearDateRange();
    
    document.getElementById('filterForm').submit();
}

// Set last month
function setLastMonth() {
    const today = new Date();
    const lastMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1);
    
    document.getElementById('month').value = lastMonth.getMonth() + 1;
    document.getElementById('year').value = lastMonth.getFullYear();
    
    // Switch to month/year tab and clear date range
    document.getElementById('month-year-tab').click();
    clearDateRange();
    
    document.getElementById('filterForm').submit();
}

// Format date as YYYY-MM-DD
function formatDate(date) {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}
