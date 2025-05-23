// Get DOM elements
const sidebar = document.getElementById('sidebar');
const mainContent = document.querySelector('.main-content');
const navbar = document.querySelector('.admin-navbar');
const sidebarToggle = document.getElementById('sidebarToggle');

// Check localStorage for saved state
const isSidebarCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';

// Apply initial state
if (isSidebarCollapsed) {
    sidebar.classList.add('collapsed');
    mainContent.classList.add('expanded');
    navbar.classList.add('expanded');
}

// Toggle sidebar
sidebarToggle.addEventListener('click', function () {
    sidebar.classList.toggle('collapsed');
    mainContent.classList.toggle('expanded');
    navbar.classList.toggle('expanded');

    // Save state to localStorage
    localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
});

// Handle responsive behavior
function handleResize() {
    if (window.innerWidth <= 991.98) {
        sidebar.classList.remove('collapsed');
        mainContent.classList.add('expanded');
        navbar.classList.add('expanded');
    } else {
        // Restore saved state on desktop
        const savedState = localStorage.getItem('sidebarCollapsed') === 'true';
        if (savedState) {
            sidebar.classList.add('collapsed');
            mainContent.classList.add('expanded');
            navbar.classList.add('expanded');
        } else {
            sidebar.classList.remove('collapsed');
            mainContent.classList.remove('expanded');
            navbar.classList.remove('expanded');
        }
    }
}

window.addEventListener('resize', handleResize);
handleResize();