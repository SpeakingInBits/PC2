window.addEventListener("load", () => {
    const uri = document.getElementById("qrCodeData").getAttribute('data-url');
    const qrContainer = document.getElementById("qrCode");
    new QRCode(qrContainer,
        {
            text: uri,
            width: 150,
            height: 150
        });

    // The QR code library gives the generated image the alt text "Scan me!"; replace it with a meaningful description
    qrContainer.querySelectorAll("img").forEach(img => {
        img.alt = "QR code for setting up your authenticator app. You can also enter the key shown above.";
    });
});