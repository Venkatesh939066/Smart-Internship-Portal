document.addEventListener('DOMContentLoaded', function () {
    var sidebar = document.getElementById('sidebar');
    var overlay = document.getElementById('sidebarOverlay');
    var toggle = document.getElementById('sidebarToggle');

    if (!sidebar || !toggle) return;

    function openSidebar() {
        sidebar.classList.add('open');
        overlay.classList.add('open');
        document.body.style.overflow = 'hidden';
    }

    function closeSidebar() {
        sidebar.classList.remove('open');
        overlay.classList.remove('open');
        document.body.style.overflow = '';
    }

    toggle.addEventListener('click', function () {
        if (sidebar.classList.contains('open')) {
            closeSidebar();
        } else {
            openSidebar();
        }
    });

    overlay.addEventListener('click', closeSidebar);

    sidebar.querySelectorAll('.sidebar-link').forEach(function (link) {
        link.addEventListener('click', function (e) {
            if (window.innerWidth <= 768) closeSidebar();

            var targetUrl = link.getAttribute('href');
            if (targetUrl && !targetUrl.startsWith('#') && targetUrl !== 'javascript:void(0)' && !link.hasAttribute('data-no-replace')) {
                var currentPath = window.location.pathname.toLowerCase();
                var isCurrentDashboard = currentPath === '/student' || currentPath === '/student/index' || 
                                         currentPath === '/company' || currentPath === '/company/index' ||
                                         currentPath === '/';

                if (!isCurrentDashboard) {
                    e.preventDefault();
                    window.location.replace(targetUrl);
                }
            }
        });
    });
});
