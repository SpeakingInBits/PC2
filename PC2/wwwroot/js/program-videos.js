function extractYouTubeId(value) {
    // Matches full YouTube URLs: youtube.com/watch?v=ID, youtu.be/ID, youtube.com/embed/ID, youtube.com/v/ID
    var patterns = [
        /[?&]v=([a-zA-Z0-9_-]{11,})/,
        /youtu\.be\/([a-zA-Z0-9_-]{11,})/,
        /\/embed\/([a-zA-Z0-9_-]{11,})/,
        /\/v\/([a-zA-Z0-9_-]{11,})/
    ];
    for (var i = 0; i < patterns.length; i++) {
        var match = value.match(patterns[i]);
        if (match) return match[1];
    }
    return null;
}

function checkFileSize() {
    var fileInput = document.getElementById('pdfFile');
    var fileSizeError = document.getElementById('fileSizeError');
    var submitButton = document.getElementById('submitButton');
    var maxSize = 50 * 1024 * 1024; // 50MB

    if (fileInput.files.length > 0) {
        var file = fileInput.files[0];
        if (file.size > maxSize) {
            fileSizeError.textContent = "The file size exceeds the 50MB limit.";
            submitButton.disabled = true;
        } else {
            fileSizeError.textContent = "";
            submitButton.disabled = false;
        }
    } else {
        fileSizeError.textContent = "";
        submitButton.disabled = false;
    }
}

document.addEventListener('DOMContentLoaded', function () {
    var input = document.getElementById('youTubeVideoId');
    if (input) {
        input.addEventListener('blur', function () {
            var extracted = extractYouTubeId(this.value.trim());
            if (extracted) {
                this.value = extracted;
            }
        });
    }
});
