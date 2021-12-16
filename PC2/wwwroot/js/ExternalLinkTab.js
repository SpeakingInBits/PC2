// Opens external links in a new tab

(function () {
    const links = document.querySelectorAll("a[href^='https://'], a[href^='http://']");
    const host = window.location.hostname;

    const internalLink = link => new URL(link).hostname === host

    links.forEach(link => {
        if (internalLink(link)) return;
        if (link.getAttribute("target", "blank")) {
            return link;
        }
        else {
            link.setAttribute("target", "blank");
            link.setAttribute("rel", "noopener");
        }
    });
})()