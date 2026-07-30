function checkFileSize() {
    var fileInput = document.getElementById('attachmentFile');
    var fileSizeError = document.getElementById('fileSizeError');
    var fileSizeWarning = document.getElementById('fileSizeWarning');
    var submitButton = document.getElementById('submitButton');
    var maxSize = 50 * 1024 * 1024; // 50MB
    var warnSize = 2 * 1024 * 1024; // 2MB

    fileSizeError.textContent = "";
    fileSizeWarning.classList.add('d-none');
    submitButton.disabled = false;

    if (fileInput.files.length > 0) {
        var file = fileInput.files[0];
        if (file.size > maxSize) {
            fileSizeError.textContent = "The file size exceeds the 50MB limit.";
            submitButton.disabled = true;
        } else if (file.size > warnSize) {
            var sizeInMb = (file.size / (1024 * 1024)).toFixed(1);
            fileSizeWarning.textContent = "This file is " + sizeInMb + " MB. Large PDFs may take longer for clients to download.";
            fileSizeWarning.classList.remove('d-none');
        }
    }
}
