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
})()