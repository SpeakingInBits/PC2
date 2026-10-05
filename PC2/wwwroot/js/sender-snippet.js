// Loads Sender.net, which manages PC2's mailing list. It fills in every sign-up form placed
// with _MailingListSignupPartial. Provided by Sender.net; only formatted for readability.
(function (s, e, n, d, er) {
    s['Sender'] = er;
    s[er] = s[er] || function () {
        (s[er].q = s[er].q || []).push(arguments)
    }, s[er].l = 1 * new Date();
    s[er].on = function (event, callback) {
        s[er].listeners = s[er].listeners || {};
        (s[er].listeners[event] = s[er].listeners[event] || []).push(callback);
    };
    var a = e.createElement(n), m = e.getElementsByTagName(n)[0];
    a.async = 1;
    a.src = d;
    m.parentNode.insertBefore(a, m)
})(window, document, 'script', 'https://cdn.sender.net/accounts_resources/universal.js', 'sender');
sender('75bcedda809181');
