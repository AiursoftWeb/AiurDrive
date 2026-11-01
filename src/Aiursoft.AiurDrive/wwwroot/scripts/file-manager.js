(() => {
    const manager = document.getElementById('file-manager');
    const localized = key => document.querySelector(`#file-manager-loc [data-key="${key}"]`).textContent;
    $(document).ready(function() {
        $('#fileTable').DataTable({
            "destroy": true,
            "responsive": true,
            "paging": true,
            "pageLength": 25,
            "searching": true,
            "info": false,
            "order": [[0, "asc"]]
        });
    });

    function reloadPage() {
        location.reload();
    }

    window.reloadPage = reloadPage;

    function openRenameModal(currentName, currentPath) {
        document.getElementById('renamePath').value = currentPath;
        document.getElementById('newName').value = currentName;
        var renameModal = new bootstrap.Modal(document.getElementById('renameModal'));
        renameModal.show();
    }

    function cutItem(siteName, path, name) {
        sessionStorage.setItem('clipboard', JSON.stringify({
            siteName: siteName,
            path: path,
            name: name
        }));
        checkClipboard();
    }

    function cancelCut() {
        sessionStorage.removeItem('clipboard');
        checkClipboard();
    }

    function checkClipboard() {
        var clipboard = sessionStorage.getItem('clipboard');
        var pasteForm = document.getElementById('pasteForm');
        if (!pasteForm) return;
        var pasteSourcePath = document.getElementById('pasteSourcePath');
        var pasteButtonText = document.getElementById('pasteButtonText');

        if (clipboard) {
            var data = JSON.parse(clipboard);
            if (data.siteName === manager.dataset.siteName) {
                pasteForm.classList.remove('d-none');
                pasteSourcePath.value = data.path;
                pasteButtonText.innerText = localized('paste') + ' ' + data.name;
            } else {
                pasteForm.classList.add('d-none');
            }
        } else {
            pasteForm.classList.add('d-none');
        }
    }

    // View Toggle Logic
    var currentView = localStorage.getItem('fileManagerView') || 'list';

    function updateView() {
        if (currentView === 'grid') {
            $('#listView').addClass('d-none');
            $('#gridView').removeClass('d-none');
            $('#viewToggleIcon').removeClass('fa-th-large').addClass('fa-list');
            $('#viewToggleBtn').attr('title', localized('list-view'));
        } else {
            $('#gridView').addClass('d-none');
            $('#listView').removeClass('d-none');
            $('#viewToggleIcon').removeClass('fa-list').addClass('fa-th-large');
            $('#viewToggleBtn').attr('title', localized('grid-view'));
        }
    }

    function toggleView() {
        currentView = currentView === 'list' ? 'grid' : 'list';
        localStorage.setItem('fileManagerView', currentView);
        updateView();
    }

    $(document).ready(function() {
        // Check clipboard on load
        checkClipboard();
        // Initialize view
        updateView();

        // Clear clipboard after successful paste
        $('#pasteForm').on('submit', function() {
            sessionStorage.removeItem('clipboard');
        });
    });

    manager.addEventListener('click', event => {
        const button = event.target.closest('[data-file-action]');
        if (!button) return;
        const {fileAction, path, name} = button.dataset;
        if (fileAction === 'cut') cutItem(manager.dataset.siteName, path, name);
        if (fileAction === 'rename') openRenameModal(name, path);
        if (fileAction === 'cancel-cut') cancelCut();
        if (fileAction === 'toggle-view') toggleView();
    });
    manager.addEventListener('submit', event => {
        const kind = event.target.dataset.confirmDelete;
        if (kind && !confirm(localized(`delete-${kind}`))) event.preventDefault();
    });

})();
