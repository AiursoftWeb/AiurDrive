(() => {
    'use strict';
    const page = document.getElementById('sharing-page');
    if (!page) return;
    const text = (key, fallback) => page.querySelector(`[data-key="${key}"]`)?.textContent ?? fallback;
    const addDetails = document.getElementById('add-share-details');
    const addForm = document.getElementById('add-share-form');
    const roleSelect = document.getElementById('roleSelect');
    const userSearch = document.getElementById('userSearch');
    const userId = document.getElementById('selectedUserId');
    const userDisplay = document.getElementById('selectedUserDisplay');
    const results = document.getElementById('userSearchResults');
    const searchStatus = document.getElementById('user-search-status');
    const visibilityForm = document.getElementById('visibility-form');
    const linkAccess = document.getElementById('link-access');
    let searchTimeout;
    let searchRequest;

    function showError(form, message) {
        const details = form.closest('details');
        if (details) details.open = true;
        const error = form.querySelector('.form-error');
        error.textContent = message;
        error.hidden = false;
    }

    page.querySelectorAll('input[name="shareType"]').forEach(radio => radio.addEventListener('change', () => {
        const isUser = document.getElementById('shareTypeUser').checked;
        document.getElementById('role-selection').hidden = isUser;
        document.getElementById('user-selection').hidden = !isUser;
        roleSelect.disabled = isUser;
        userSearch.disabled = !isUser;
        userId.disabled = !isUser;
        searchRequest?.abort();
        results.classList.add('d-none');
        searchStatus.textContent = '';
    }));

    userSearch.addEventListener('input', () => {
        clearTimeout(searchTimeout);
        searchRequest?.abort();
        results.replaceChildren();
        results.classList.add('d-none');
        searchStatus.textContent = '';
        userId.value = '';
        userDisplay.classList.add('d-none');
        const query = userSearch.value.trim();
        if (query.length < 3) return;
        searchTimeout = setTimeout(() => loadUsers(query), 300);
    });

    async function loadUsers(query) {
        const request = new AbortController();
        searchRequest = request;
        searchStatus.textContent = text('searching', 'Searching...');
        try {
            const response = await fetch(`/Users/Search?query=${encodeURIComponent(query)}`, { signal: request.signal });
            if (!response.ok || response.redirected) throw new Error('Search failed');
            const data = await response.json();
            if (request.signal.aborted || query !== userSearch.value.trim() || userSearch.disabled) return;
            results.replaceChildren();
            for (const user of data.users) {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'list-group-item list-group-item-action text-break';
                const name = document.createElement('strong');
                name.textContent = user.DisplayName;
                const username = document.createElement('div');
                username.className = 'small text-muted';
                username.textContent = user.UserName;
                button.append(name, username);
                button.addEventListener('click', () => {
                    userId.value = user.Id;
                    document.getElementById('selectedUserName').textContent = user.DisplayName;
                    document.getElementById('selectedUserUsername').textContent = user.UserName;
                    userDisplay.classList.remove('d-none');
                    results.classList.add('d-none');
                    userSearch.value = user.DisplayName;
                    userSearch.classList.add('d-none');
                    searchStatus.textContent = '';
                    document.getElementById('clearSelectedUser').focus();
                });
                results.append(button);
            }
            results.classList.toggle('d-none', !data.users.length);
            searchStatus.textContent = data.users.length ? '' : text('noUsers', 'No users found. Try another name or email.');
        } catch (error) {
            if (request.signal.aborted) return;
            searchStatus.textContent = text('searchFailed', 'Search failed. Change the search text to try again.');
        }
    }

    document.getElementById('clearSelectedUser').addEventListener('click', () => {
        userId.value = '';
        userSearch.value = '';
        userSearch.classList.remove('d-none');
        userDisplay.classList.add('d-none');
        userSearch.focus();
    });
    userSearch.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
            event.preventDefault();
            if (!results.classList.contains('d-none')) results.querySelector('button')?.focus();
        }
        if (event.key === 'Escape') results.classList.add('d-none');
    });
    page.addEventListener('keydown', event => {
        if (event.key === 'Escape' && results.contains(event.target)) {
            results.classList.add('d-none');
            userSearch.focus();
        }
    });

    linkAccess.addEventListener('change', () => {
        document.getElementById('visibility-pending').hidden = linkAccess.value === visibilityForm.dataset.savedPublic;
    });
    addForm.addEventListener('invalid', () => { addDetails.open = true; }, true);

    // Keep inputs in place on validation, network or session failures. Only a
    // redirect back to this management page confirms a successful operation.
    page.querySelectorAll('form').forEach(form => form.addEventListener('submit', async event => {
        event.preventDefault();
        if (form.dataset.busy === 'true') return;
        if (form === addForm && ((!roleSelect.disabled && !roleSelect.value) ||
            (!userId.disabled && !userId.value))) {
            showError(form, text('invalid', 'Select a user from the search results or choose a role.'));
            (userId.disabled ? roleSelect : userSearch).focus();
            return;
        }
        if (form.classList.contains('remove-share-form') && !window.confirm(text('remove', 'Remove this share? Other access may still apply.'))) return;
        const data = new FormData(form);
        const submit = form.querySelector('[type="submit"]');
        form.dataset.busy = 'true';
        form.setAttribute('aria-busy', 'true');
        submit.disabled = true;
        form.querySelector('.form-error').hidden = true;
        try {
            const response = await fetch(form.action, { method: 'POST', body: data });
            const destination = new URL(response.url);
            if (!response.ok || !response.redirected || destination.origin !== location.origin || destination.pathname !== location.pathname) {
                throw new Error('Change not confirmed');
            }
            const error = destination.searchParams.get('error');
            if (error) {
                showError(form, error === 'duplicate'
                    ? text('duplicate', 'This user or role already has access to this site.')
                    : text('invalid', 'Select a user from the search results or choose a role.'));
                return;
            }
            location.assign(destination.href);
        } catch {
            showError(form, text('failed', 'Could not confirm the change. Your input is kept. Try again, or reload to check the saved state.'));
        } finally {
            form.dataset.busy = 'false';
            form.removeAttribute('aria-busy');
            submit.disabled = false;
        }
    }));

    document.getElementById('copy-share-link').addEventListener('click', async () => {
        const status = document.getElementById('copy-status');
        const link = document.getElementById('share-link');
        try {
            await navigator.clipboard.writeText(link.value);
            status.textContent = text('copied', 'Link copied.');
        } catch {
            link.focus();
            link.select();
            status.textContent = text('copyFailed', 'Could not copy. Select and copy the link above.');
        }
        status.hidden = false;
    });

    const error = new URLSearchParams(location.search).get('error');
    if (error) showError(addForm, error === 'duplicate'
        ? text('duplicate', 'This user or role already has access to this site.')
        : text('invalid', 'Select a user from the search results or choose a role.'));
})();
