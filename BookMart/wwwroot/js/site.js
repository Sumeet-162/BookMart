// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

window.runPaymentSuccessConfetti = function() {
    const script = document.createElement('script');
    script.src = 'https://cdn.jsdelivr.net/npm/@tsparticles/confetti@3.0.3/tsparticles.confetti.bundle.min.js';
    script.onload = function() {
        if (window.confetti) {
            const defaults = {
                spread: 360,
                ticks: 100,
                gravity: 0,
                decay: 0.94,
                startVelocity: 30,
            };
            function shoot() {
                window.confetti({
                    ...defaults,
                    particleCount: 40,
                    scalar: 1.2,
                    shapes: ["circle", "square"],
                    colors: ["#a864fd", "#29cdff", "#78ff44", "#ff718d", "#fdff6a"],
                });
                window.confetti({
                    ...defaults,
                    particleCount: 40,
                    scalar: 2,
                    shapes: ["emoji"],
                    shapeOptions: {
                        emoji: {
                            value: [ "🛒", "📖" ,"📦","🚚"],
                        },
                    },
                });
            }
            setTimeout(shoot, 0);
            setTimeout(shoot, 100);
            setTimeout(shoot, 200);
        } else {
            console.warn('Confetti library loaded but window.confetti is not available.');
        }
    };
    script.onerror = function() {
        console.warn('Failed to load confetti library.');
    };
    document.head.appendChild(script);
}
