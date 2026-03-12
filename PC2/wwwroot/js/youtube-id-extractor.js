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
