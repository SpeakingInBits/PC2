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

/**
 * Mobile Navbar Enhancement
 * Improves the Bootstrap dropdown experience for mobile users
 */
document.addEventListener('DOMContentLoaded', function () {
    // For improved mobile UX in the navbar
    if (window.innerWidth < 992) {
        // Ensure parent nav items with dropdowns are clickable on mobile
        document.querySelectorAll('.navbar .dropdown').forEach(function (dropdown) {
            dropdown.addEventListener('click', function (e) {
                // Make sure we don't intercept clicks on dropdown items
                if (e.target.classList.contains('dropdown-toggle')) {
                    e.stopPropagation();
                }
            });
        });
        
        // Add aria labels for better accessibility
        document.querySelectorAll('.dropdown-menu').forEach(function(menu) {
            const parentText = menu.previousElementSibling.textContent.trim();
            menu.setAttribute('aria-label', parentText + ' submenu');
        });
    }
    
    // Handle the active page highlighting
    const controller = document.body.getAttribute('data-controller');
    const action = document.body.getAttribute('data-action');
    
    if (controller && action) {
        const navItem = document.getElementById(controller + '-' + action);
        if (navItem) {
            navItem.classList.add('active-nav-item');
        }
    }
});