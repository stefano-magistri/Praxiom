window.jsLoadFeather = () => {
    'use strict'
    feather.replace({ 'aria-hidden': 'true' });
}

window.jsSaveAsFile = (filename, byteBase64) => {
    var binary = atob(byteBase64);
    var bytes = new Uint8Array(binary.length);
    for (var i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }

    var blob = new Blob([bytes], { type: 'application/pdf' });
    var url = URL.createObjectURL(blob);

    var link = document.createElement('a');
    link.download = filename;
    link.href = url;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    // Revoke asynchronously to avoid revoking before the browser starts the download.
    setTimeout(function () {
        URL.revokeObjectURL(url);
    }, 100);
}

window.jsCloseNavbar = () => {
    // Close the navbar using the Collapse API
    var sidebar = document.getElementById('sidebarMenu');
    if (sidebar.classList.contains('show')) {
        var bootstrapCollapse = new bootstrap.Collapse(sidebar);
        bootstrapCollapse.hide();
    }
}
