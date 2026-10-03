// Opens external links in a new tab

(function () {
    const links = document.querySelectorAll("a[href^='https://'], a[href^='http://']");
    const host = window.location.hostname;

    const internalLink = link => new URL(link).hostname === host

    links.forEach(link => {
        if (internalLink(link)) return;
        if (link.getAttribute("target", "_blank")) {
            return link;
        }
        else {
            link.setAttribute("target", "_blank");
            link.setAttribute("rel", "noopener");
        }
    });
})();

// Tells screen reader users when a link opens in a new tab, so a new tab isn't a surprise.
// Runs after the code above so external links are included.
(function () {
    const newTabText = "(opens in new tab)";
    const newTabLinks = document.querySelectorAll("a[target='_blank']");

    newTabLinks.forEach(link => {
        if (link.textContent.includes(newTabText)) return;

        // aria-label replaces the link text for screen readers, so the note must go in the label
        const label = link.getAttribute("aria-label");
        if (label) {
            if (!label.includes(newTabText)) {
                link.setAttribute("aria-label", `${label} ${newTabText}`);
            }
            return;
        }

        const note = document.createElement("span");
        note.className = "visually-hidden";
        note.textContent = ` ${newTabText}`;
        link.appendChild(note);
    });
})();
