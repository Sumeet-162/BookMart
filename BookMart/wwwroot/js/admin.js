// Sidebar toggle functionality
const sidebar = document.querySelector('.sidebar');
const navbar = document.querySelector('.admin-navbar');
const mainContent = document.querySelector('.main-content');
const sidebarToggle = document.querySelector('#sidebarToggle');

if (sidebarToggle) {
    sidebarToggle.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');
        navbar.classList.toggle('full-width');
        mainContent.classList.toggle('full-width');
    });
}

// Handle mobile view
function handleMobileView() {
    if (window.innerWidth <= 768) {
        sidebar.classList.add('collapsed');
        navbar.classList.add('full-width');
        mainContent.classList.add('full-width');
    } else {
        sidebar.classList.remove('collapsed');
        navbar.classList.remove('full-width');
        mainContent.classList.remove('full-width');
    }
}

// Initial check and event listener for window resize
handleMobileView();
window.addEventListener('resize', handleMobileView);

// Update active nav link based on current page
const currentPath = window.location.pathname;
document.querySelectorAll('.sidebar .nav-link').forEach(link => {
    if (link.getAttribute('href') === currentPath) {
        link.classList.add('active');
    }
});