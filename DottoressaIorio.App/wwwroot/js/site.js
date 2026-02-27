window.jsLoadFeather = () => {
    'use strict'
    feather.replace({ 'aria-hidden': 'true' });
}

window._pdfTabs = window._pdfTabs || {};

window.jsPreparePdfTab = () => {
    var newTab = window.open('about:blank', '_blank');
    if (!newTab) {
        return null;
    }

    var id = 'pdf_' + Date.now().toString() + '_' + Math.floor(Math.random() * 1000000).toString();
    window._pdfTabs[id] = newTab;
    newTab.document.title = 'Generazione PDF...';
    newTab.document.body.style.margin = '0';
    newTab.document.body.innerHTML = "<div style='padding:24px;font-family:Arial,sans-serif;'>Generazione PDF in corso...</div>";
    return id;
}

window.jsClosePreparedPdfTab = (tabId) => {
    if (!tabId || !window._pdfTabs[tabId]) {
        return;
    }

    try {
        if (!window._pdfTabs[tabId].closed) {
            window._pdfTabs[tabId].close();
        }
    } finally {
        delete window._pdfTabs[tabId];
    }
}

window.jsSaveAsFile = (tabId, filename, byteBase64) => {
    var pdfDataUrl = 'data:application/pdf;base64,' + byteBase64;
    var targetTab = null;

    if (tabId && window._pdfTabs[tabId] && !window._pdfTabs[tabId].closed) {
        targetTab = window._pdfTabs[tabId];
    } else {
        targetTab = window.open('about:blank', '_blank');
    }

    if (!targetTab) {
        return;
    }

    targetTab.document.title = filename;
    targetTab.document.body.style.margin = '0';
    targetTab.document.body.innerHTML =
        "<iframe title='therapy-pdf' src='" + pdfDataUrl + "' style='border:0;width:100vw;height:100vh;'></iframe>";

    if (tabId) {
        delete window._pdfTabs[tabId];
    }
}

window.jsCloseNavbar = () => {
    // Close the navbar using the Collapse API
    var sidebar = document.getElementById('sidebarMenu');
    if (sidebar.classList.contains('show')) {
        var bootstrapCollapse = new bootstrap.Collapse(sidebar);
        bootstrapCollapse.hide();
    }
}
