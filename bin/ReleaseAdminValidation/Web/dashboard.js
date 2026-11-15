// Dashboard JavaScript
let currentUser = null;
let userCharacters = [];
let mallProducts = [];
let mallCharacters = [];
let activeMallCategory = 'all';
let mallLoadPromise = null;
let adminAccounts = [];
let adminCharacters = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    wireDashboardEvents();
    if (!checkLoginStatus()) return;
    loadCharacters();
    loadCommunityStats();
    loadAdminStatus();
    window.setInterval(loadCommunityStats, 30000);
    loadMallCharacters();
    updateTimers();
    setInterval(updateTimers, 1000);
    const supportEmail = typeof CONFIG !== 'undefined' && CONFIG.supportEmail;
    if (supportEmail) {
        const supportLink = document.getElementById('mallSupportLink');
        supportLink.href = 'mailto:' + supportEmail;
        supportLink.textContent = supportEmail;
    }
});

function wireDashboardEvents() {
    document.addEventListener('click', function(e) {
        const target = e.target.closest ? e.target : e.target.parentElement;
        if (!target) return;

        const tabButton = target.closest('[data-tab]');
        if (tabButton) {
            showTab(tabButton.dataset.tab, tabButton);
            return;
        }

        const categoryButton = target.closest('[data-category]');
        if (categoryButton) {
            filterMall(categoryButton.dataset.category, categoryButton);
            return;
        }

        const actionButton = target.closest('[data-action]');
        if (!actionButton) return;
        const action = actionButton.dataset.action;
        if (action === 'logout') logout();
        else if (action === 'create-character') createCharacter();
        else if (action === 'toggle-create') toggleCreateForm();
        else if (action === 'change-password') changePassword();
        else if (action === 'join-event') joinEvent(actionButton.dataset.eventId);
        else if (action === 'claim-reward') claimReward(actionButton.dataset.rewardId);
        else if (action === 'play-character') playCharacter(actionButton.dataset.characterName);
        else if (action === 'delete-character') deleteCharacter(actionButton.dataset.characterId);
        else if (action === 'buy-item') buyItem(Number(actionButton.dataset.itemId));
        else if (action === 'view-reviews') viewItemReviews(Number(actionButton.dataset.itemId));
        else if (action === 'close-reviews') document.getElementById('mallReviewPanel').classList.add('hidden');
    });

    document.getElementById('mallSearch').addEventListener('input', renderMallProducts);
    document.getElementById('mallReviewForm').addEventListener('submit', submitMallReview);
    document.getElementById('mallNewsletterForm').addEventListener('submit', subscribeMallNewsletter);
    document.getElementById('adminCreateAccountForm').addEventListener('submit', createAdminAccount);
    document.getElementById('adminCreateCharacterForm').addEventListener('submit', createAdminCharacter);
    document.getElementById('adminCharacterEditor').addEventListener('submit', updateAdminCharacter);

    document.addEventListener('click', function(e) {
        const actionButton = e.target.closest ? e.target.closest('[data-action]') : null;
        if (!actionButton) return;
        const action = actionButton.dataset.action;
        if (action === 'admin-refresh-accounts') loadAdminAccounts();
        else if (action === 'admin-refresh-characters') loadAdminCharacters();
        else if (action === 'admin-refresh-online') loadAdminOnlinePlayers();
        else if (action === 'admin-reset-password') resetAdminPassword(actionButton.dataset.accountId);
        else if (action === 'admin-toggle-role') toggleAdminRole(actionButton.dataset.accountId);
        else if (action === 'admin-delete-account') deleteAdminAccount(actionButton.dataset.accountId);
        else if (action === 'admin-edit-character') editAdminCharacter(actionButton.dataset.characterId);
        else if (action === 'admin-delete-character') deleteAdminCharacter(actionButton.dataset.characterId);
        else if (action === 'admin-cancel-edit') document.getElementById('adminCharacterEditor').classList.add('hidden');
    });
}

function apiPost(path, values) {
    const body = new URLSearchParams();
    Object.keys(values).forEach(key => body.append(key, values[key]));
    return fetch(CONFIG.getApiBase() + path, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8' },
        body: body.toString()
    }).then(response => response.json().then(data => ({ ok: response.ok, status: response.status, data: data })));
}

// Check if user is logged in
function checkLoginStatus() {
    const token = localStorage.getItem('userToken');
    const username = localStorage.getItem('username');
    
    if (!token || !username) {
        window.location.href = '/';
        return false;
    }
    
    currentUser = username;
    document.getElementById('usernameDisplay').textContent = username;
    document.getElementById('accUsername').textContent = username;
    loadUserStats();
    return true;
}

// Load user statistics
function loadUserStats() {
    const token = localStorage.getItem('userToken');
    
    if (!token) {
        setDefaultStats();
        return;
    }
    
    apiPost('/user-stats', { token: token })
    .then(response => response.data)
    .then(data => {
        if (data.success && data.stats) {
            document.getElementById('accCreated').textContent = data.stats.created_at || new Date().toLocaleDateString();
            document.getElementById('accLastLogin').textContent = data.stats.last_login || new Date().toLocaleString();
            document.getElementById('totalPlaytime').textContent = (data.stats.playtime_hours || 0) + ' hours';
            document.getElementById('totalLevel').textContent = data.stats.total_level || 0;
            document.getElementById('totalGold').textContent = (data.stats.total_gold || 0).toLocaleString();
            document.getElementById('rewardPoints').textContent = (data.stats.reward_points || 0).toLocaleString();
            document.getElementById('mallPoints').textContent = (data.stats.mall_points || 0).toLocaleString();
        } else {
            setDefaultStats();
        }
    })
    .catch(error => {
        console.error('Error loading stats:', error);
        setDefaultStats();
    });
}

// Set default stats for new account
function setDefaultStats() {
    const now = new Date();
    document.getElementById('accCreated').textContent = now.toLocaleDateString();
    document.getElementById('accLastLogin').textContent = now.toLocaleString();
    document.getElementById('totalPlaytime').textContent = '0 hours';
    document.getElementById('totalLevel').textContent = '0';
    document.getElementById('totalGold').textContent = '0';
    document.getElementById('rewardPoints').textContent = '0';
    document.getElementById('mallPoints').textContent = '0';
}

// Load characters saved in the game database
function loadCharacters() {
    const container = document.getElementById('characters-list');
    container.innerHTML = '<p class="mall-empty">Loading your saved characters…</p>';
    apiPost('/characters', { token: localStorage.getItem('userToken') })
        .then(response => {
            if (!response.ok || !response.data.ok) throw new Error(response.data.message || 'Could not load your characters.');
            userCharacters = response.data.characters || [];
            renderCharacters();
            updateCharacterCount();
        })
        .catch(error => {
            container.innerHTML = '<p class="mall-empty">Your characters could not be loaded. Please try again.</p>';
            showMessage(error.message, 'error');
        });
}

// Render character cards
function renderCharacters() {
    const container = document.getElementById('characters-list');
    const emptyState = document.getElementById('emptyState');
    
    if (userCharacters.length === 0) {
        container.innerHTML = '';
        emptyState.style.display = 'block';
        return;
    }
    
    emptyState.style.display = 'none';
    container.innerHTML = userCharacters.map((char, idx) => `
        <div class="character-card">
            <div class="char-icon">⚔️</div>
            <div class="char-name">${escapeHtml(char.name)}</div>
            <div class="char-info">Slot ${Number(char.slot)} · ${getBodyName(char.body)} · ${getElementName(char.element)}</div>
            <div class="char-info">HP ${Number(char.hp)} · SP ${Number(char.sp)} · ${Number(char.experience).toLocaleString()} EXP</div>
            <div class="char-info">STR ${Number(char.strength)} · CON ${Number(char.constitution)} · INT ${Number(char.intelligence)} · WIS ${Number(char.wisdom)} · AGI ${Number(char.agility)}</div>
            <div class="char-info">Gold ${Number(char.gold).toLocaleString()} · Map ${Number(char.map)} (${Number(char.x)}, ${Number(char.y)})</div>
            <div class="char-info">Equipment item IDs: ${char.equipment && char.equipment.length ? char.equipment.map(Number).join(', ') : 'None'}</div>
            <div class="char-actions">
                <button class="btn-play" data-action="play-character" data-character-name="${escapeHtml(char.name)}">How to play</button>
                <button class="btn-delete" data-action="delete-character" data-character-id="${Number(char.id)}">Delete</button>
            </div>
        </div>
    `).join('');
}

function getBodyName(body) {
    return ({ 1: 'Small Male', 2: 'Small Female', 3: 'Big Male', 4: 'Big Female' })[Number(body)] || 'Unknown appearance';
}

function getElementName(element) {
    return ({ 0: 'Normal', 1: 'Earth', 2: 'Water', 3: 'Fire', 4: 'Wind' })[Number(element)] || 'Unknown element';
}

// Update character count
function updateCharacterCount() {
    document.getElementById('charCount').textContent = userCharacters.length;
    document.getElementById('charsCreated').textContent = userCharacters.length;
}

// Toggle create character form
function toggleCreateForm() {
    const form = document.getElementById('createCharForm');
    const canCreate = userCharacters.length < 2;
    
    if (!canCreate) {
        showMessage('You have reached the maximum of 2 characters', 'error');
        return;
    }
    
    form.classList.toggle('hidden');
    if (!form.classList.contains('hidden')) {
        // Reset form
        document.getElementById('charName').value = '';
        document.getElementById('charBody').value = '';
        document.getElementById('charElement').value = '';
    }
}

// Update character preview
document.addEventListener('change', function(e) {
    if (e.target.id === 'charBody') {
        document.getElementById('previewBody').textContent = getBodyName(e.target.value);
    }
    if (e.target.id === 'charElement') {
        document.getElementById('previewElement').textContent = getElementName(e.target.value);
    }
    if (e.target.id === 'charName') {
        document.getElementById('previewName').textContent = e.target.value || 'Your Character';
    }
});

// Create character
function createCharacter() {
    const name = document.getElementById('charName').value.trim();
    const body = document.getElementById('charBody').value;
    const element = document.getElementById('charElement').value;

    if (!/^[a-zA-Z0-9_]{4,14}$/.test(name) || !body || !element) {
        showMessage('Enter a 4–14 character name and choose an appearance and element.', 'error');
        return;
    }

    apiPost('/characters/create', {
        token: localStorage.getItem('userToken'),
        name: name,
        body: body,
        element: element
    }).then(response => {
        if (!response.ok || !response.data.ok) throw new Error(response.data.message || 'Character creation failed.');
        userCharacters = response.data.characters || [];
        renderCharacters();
        updateCharacterCount();
        document.getElementById('createCharForm').classList.add('hidden');
        document.getElementById('mallCharacter').dataset.loaded = 'false';
        loadMallCharacters();
        showMessage('Character saved to your game account. Log in through the game client to play.', 'success');
    }).catch(error => showMessage(error.message, 'error'));
}

// Delete character
function deleteCharacter(charId) {
    const character = userCharacters.find(item => Number(item.id) === Number(charId));
    if (!character || !confirm('Delete ' + character.name + '? This cannot be undone.')) return;

    apiPost('/characters/delete', {
        token: localStorage.getItem('userToken'),
        characterId: String(charId)
    }).then(response => {
        if (!response.ok || !response.data.ok) throw new Error(response.data.message || 'Character deletion failed.');
        userCharacters = response.data.characters || [];
        renderCharacters();
        updateCharacterCount();
        document.getElementById('mallCharacter').dataset.loaded = 'false';
        loadMallCharacters();
        showMessage('Character deleted.', 'success');
    }).catch(error => showMessage(error.message, 'error'));
}

// Play character
function playCharacter(name) {
    showMessage(name + ' is saved. Start the game client, log in with this account, and select the character.', 'success');
}

// Tab switching
function showTab(tabName, button) {
    // Hide all tabs
    document.querySelectorAll('.tab-content').forEach(tab => {
        tab.classList.remove('active');
    });
    
    // Remove active class from all buttons
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    
    // Show selected tab
    document.getElementById(tabName + '-tab').classList.add('active');
    
    // Add active class to clicked button
    if (button) button.classList.add('active');
    
    // Load tab-specific content
    if (tabName === 'community') {
        loadCommunityStats();
    } else if (tabName === 'shop') {
        loadMallItems();
        loadMallCharacters();
    } else if (tabName === 'admin') {
        loadAdminData();
    }
}

function adminPost(path, values) {
    const formValues = Object.assign({}, values, { token: localStorage.getItem('userToken') });
    return apiPost('/admin' + path, formValues).then(response => {
        if (!response.ok || !response.data.ok) {
            throw new Error(response.data.message || 'The admin request failed.');
        }
        return response.data;
    });
}

function loadAdminStatus() {
    adminPost('/status', {}).then(data => {
        document.getElementById('adminTabButton').hidden = !data.isAdmin;
        if (data.isAdmin) loadAdminData();
    }).catch(error => {
        document.getElementById('adminTabButton').hidden = true;
        console.error('Could not verify GM/Admin access:', error);
    });
}

function loadAdminData() {
    return Promise.all([loadAdminAccounts(), loadAdminCharacters(), loadAdminOnlinePlayers()])
        .catch(error => showMessage(error.message, 'error'));
}

function loadAdminAccounts() {
    return adminPost('/accounts', {}).then(data => {
        adminAccounts = data.accounts || [];
        renderAdminAccounts();
        const select = document.getElementById('adminCharacterAccount');
        const selected = select.value;
        select.innerHTML = adminAccounts.map(account =>
            `<option value="${Number(account.id)}">${escapeHtml(account.username)}</option>`
        ).join('');
        if (adminAccounts.some(account => String(account.id) === selected)) select.value = selected;
    }).catch(error => {
        document.getElementById('adminAccountsBody').innerHTML = '<tr><td colspan="3">Accounts could not be loaded.</td></tr>';
        throw error;
    });
}

function renderAdminAccounts() {
    const body = document.getElementById('adminAccountsBody');
    if (!adminAccounts.length) {
        body.innerHTML = '<tr><td colspan="3">No accounts found.</td></tr>';
        return;
    }
    body.innerHTML = adminAccounts.map(account => {
        const id = Number(account.id);
        const isAdmin = Number(account.gmLevel) > 0;
        return `<tr>
            <td>${escapeHtml(account.username)}</td>
            <td>${isAdmin ? 'GM/Admin' : 'Player'}</td>
            <td>
                <button class="btn btn-secondary" type="button" data-action="admin-reset-password" data-account-id="${id}">Reset password</button>
                <button class="btn btn-secondary" type="button" data-action="admin-toggle-role" data-account-id="${id}">${isAdmin ? 'Make player' : 'Make GM/Admin'}</button>
                <button class="btn btn-secondary" type="button" data-action="admin-delete-account" data-account-id="${id}">Delete</button>
            </td>
        </tr>`;
    }).join('');
}

function loadAdminCharacters() {
    return adminPost('/characters', {}).then(data => {
        adminCharacters = data.characters || [];
        renderAdminCharacters();
    }).catch(error => {
        document.getElementById('adminCharactersBody').innerHTML = '<tr><td colspan="7">Characters could not be loaded.</td></tr>';
        throw error;
    });
}

function renderAdminCharacters() {
    const body = document.getElementById('adminCharactersBody');
    if (!adminCharacters.length) {
        body.innerHTML = '<tr><td colspan="7">No characters found.</td></tr>';
        return;
    }
    body.innerHTML = adminCharacters.map(character => `<tr>
        <td>${escapeHtml(character.name)} (slot ${Number(character.slot)})</td>
        <td>${escapeHtml(character.accountUsername)}</td>
        <td>${Number(character.level)}</td>
        <td>${Number(character.gold).toLocaleString()}</td>
        <td>${Number(character.map)} (${Number(character.x)}, ${Number(character.y)})</td>
        <td>${character.online ? 'Online' : 'Offline'}</td>
        <td>
            <button class="btn btn-secondary" type="button" data-action="admin-edit-character" data-character-id="${Number(character.id)}" ${character.online ? 'disabled' : ''}>Edit</button>
            <button class="btn btn-secondary" type="button" data-action="admin-delete-character" data-character-id="${Number(character.id)}" data-account-id="${Number(character.accountId)}" ${character.online ? 'disabled' : ''}>Delete</button>
        </td>
    </tr>`).join('');
}

function loadAdminOnlinePlayers() {
    return adminPost('/online-players', {}).then(data => {
        const body = document.getElementById('adminOnlineBody');
        const players = data.characters || [];
        body.innerHTML = players.length ? players.map(character => `<tr>
            <td>${escapeHtml(character.name)}</td>
            <td>${escapeHtml(character.accountUsername)}</td>
            <td>${Number(character.level)}</td>
            <td>${Number(character.map)}</td>
            <td>${Number(character.x)}</td>
            <td>${Number(character.y)}</td>
            <td>Online</td>
        </tr>`).join('') : '<tr><td colspan="7">No players are online.</td></tr>';
    }).catch(error => {
        document.getElementById('adminOnlineBody').innerHTML = '<tr><td colspan="7">Online players could not be loaded.</td></tr>';
        throw error;
    });
}

function createAdminAccount(event) {
    event.preventDefault();
    adminPost('/account/create', {
        username: document.getElementById('adminNewUsername').value.trim(),
        password: document.getElementById('adminNewPassword').value,
        isAdmin: document.getElementById('adminNewIsAdmin').checked ? 'true' : 'false'
    }).then(() => {
        event.target.reset();
        return loadAdminData();
    }).then(() => showMessage('Account created.', 'success'))
        .catch(error => showMessage(error.message, 'error'));
}

function resetAdminPassword(accountId) {
    const password = prompt('Enter a new password (4-14 characters):');
    if (password === null) return;
    adminPost('/account/password', { accountId: String(accountId), password: password })
        .then(data => showMessage(data.message, 'success'))
        .catch(error => showMessage(error.message, 'error'));
}

function toggleAdminRole(accountId) {
    const account = adminAccounts.find(item => Number(item.id) === Number(accountId));
    if (!account) return;
    const gmLevel = Number(account.gmLevel) > 0 ? 0 : 1;
    adminPost('/account/role', { accountId: String(accountId), gmLevel: String(gmLevel) })
        .then(() => loadAdminAccounts())
        .then(() => showMessage('Account role updated.', 'success'))
        .catch(error => showMessage(error.message, 'error'));
}

function deleteAdminAccount(accountId) {
    const account = adminAccounts.find(item => Number(item.id) === Number(accountId));
    if (!account || !confirm('Delete account ' + account.username + '? The account must have no characters.')) return;
    adminPost('/account/delete', { accountId: String(accountId) })
        .then(() => loadAdminData())
        .then(() => showMessage('Account deleted.', 'success'))
        .catch(error => showMessage(error.message, 'error'));
}

function createAdminCharacter(event) {
    event.preventDefault();
    const form = event.target;
    adminPost('/character/create', {
        accountId: form.elements.accountId.value,
        slot: form.elements.slot.value,
        name: form.elements.name.value.trim(),
        body: form.elements.body.value,
        head: form.elements.head.value,
        element: form.elements.element.value
    }).then(() => {
        document.getElementById('adminCharacterName').value = '';
        return loadAdminData();
    }).then(() => showMessage('Character created.', 'success'))
        .catch(error => showMessage(error.message, 'error'));
}

function editAdminCharacter(characterId) {
    const character = adminCharacters.find(item => Number(item.id) === Number(characterId));
    if (!character || character.online) return;
    document.getElementById('adminEditCharacterId').value = character.id;
    document.getElementById('adminEditExperience').value = character.experience;
    document.getElementById('adminEditGold').value = character.gold;
    document.getElementById('adminEditHp').value = character.hp;
    document.getElementById('adminEditSp').value = character.sp;
    document.getElementById('adminEditStrength').value = character.strength;
    document.getElementById('adminEditConstitution').value = character.constitution;
    document.getElementById('adminEditIntelligence').value = character.intelligence;
    document.getElementById('adminEditWisdom').value = character.wisdom;
    document.getElementById('adminEditAgility').value = character.agility;
    document.getElementById('adminEditMap').value = character.map;
    document.getElementById('adminEditX').value = character.x;
    document.getElementById('adminEditY').value = character.y;
    document.getElementById('adminCharacterEditor').classList.remove('hidden');
    document.getElementById('adminCharacterEditor').scrollIntoView({ behavior: 'smooth', block: 'center' });
}

function updateAdminCharacter(event) {
    event.preventDefault();
    const values = {
        characterId: document.getElementById('adminEditCharacterId').value,
        experience: document.getElementById('adminEditExperience').value,
        gold: document.getElementById('adminEditGold').value,
        hp: document.getElementById('adminEditHp').value,
        sp: document.getElementById('adminEditSp').value,
        strength: document.getElementById('adminEditStrength').value,
        constitution: document.getElementById('adminEditConstitution').value,
        intelligence: document.getElementById('adminEditIntelligence').value,
        wisdom: document.getElementById('adminEditWisdom').value,
        agility: document.getElementById('adminEditAgility').value,
        map: document.getElementById('adminEditMap').value,
        x: document.getElementById('adminEditX').value,
        y: document.getElementById('adminEditY').value
    };
    adminPost('/character/update', values)
        .then(() => {
            document.getElementById('adminCharacterEditor').classList.add('hidden');
            return loadAdminData();
        }).then(() => showMessage('Character updated.', 'success'))
        .catch(error => showMessage(error.message, 'error'));
}

function deleteAdminCharacter(characterId) {
    const character = adminCharacters.find(item => Number(item.id) === Number(characterId));
    if (!character || character.online || !confirm('Delete character ' + character.name + '? This cannot be undone.')) return;
    adminPost('/character/delete', {
        accountId: String(character.accountId),
        characterId: String(character.id)
    }).then(() => loadAdminData())
        .then(() => showMessage('Character deleted.', 'success'))
        .catch(error => showMessage(error.message, 'error'));
}

// Load community statistics
function loadCommunityStats() {
    const apiBase = (typeof CONFIG !== 'undefined' && CONFIG.getApiBase) ? CONFIG.getApiBase() : window.location.origin + '/api';
    fetch(`${apiBase}/server-status`, { cache: 'no-store' })
        .then(response => {
            if (!response.ok) throw new Error('Status service returned HTTP ' + response.status);
            return response.json();
        })
        .then(data => {
            document.getElementById('activePlayers').textContent = data.playersOnline.toLocaleString();
            document.getElementById('totalPlayers').textContent = '—';
            document.getElementById('totalGuilds').textContent = '—';
            document.getElementById('dashboardServersRunning').textContent =
                data.serversRunning + ' / ' + data.configuredServers;
            const status = document.getElementById('dashboardServerStatus');
            status.textContent = data.gameOnline ? '🟢 Online' : '🔴 Offline';
            status.className = 'status-badge ' + (data.gameOnline ? 'active' : '');
            loadLeaderboard([]);
        })
        .catch(error => {
            console.error('Could not load live server status:', error);
            document.getElementById('activePlayers').textContent = '—';
            document.getElementById('totalPlayers').textContent = '—';
            document.getElementById('totalGuilds').textContent = '—';
            document.getElementById('dashboardServersRunning').textContent = '—';
            const status = document.getElementById('dashboardServerStatus');
            status.textContent = 'Unavailable';
            status.className = 'status-badge';
            loadLeaderboard([]);
        });
}

// Load leaderboard
function loadLeaderboard(characters) {
    const tbody = document.getElementById('leaderboardBody');
    if (!characters || characters.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4">Leaderboard data is not available.</td></tr>';
        return;
    }

    tbody.innerHTML = characters.map((char, idx) => `
        <tr>
            <td>${idx + 1}</td>
            <td>${escapeHtml(char.name)}</td>
            <td>Level ${Number(char.level) || 0}</td>
            <td>${(Number(char.exp) || 0).toLocaleString()} EXP</td>
        </tr>
    `).join('');
}

function loadMallItems() {
    if (mallLoadPromise) return mallLoadPromise;
    document.getElementById('mallGrid').innerHTML = '<p class="mall-empty">Loading the in-game catalog…</p>';
    mallLoadPromise = apiPost('/mall/catalog', { token: localStorage.getItem('userToken') })
        .then(response => {
            if (!response.ok || !response.data.ok) throw new Error(response.data.message || 'Could not load the item catalog.');
            mallProducts = response.data.products || [];
            renderMallProducts();
        })
        .catch(error => {
            mallLoadPromise = null;
            document.getElementById('mallGrid').innerHTML = '<p class="mall-empty">The item catalog is unavailable. Please try again later.</p>';
            showMessage(error.message, 'error');
        });
    return mallLoadPromise;
}

function loadMallCharacters() {
    const select = document.getElementById('mallCharacter');
    if (select.dataset.loaded === 'true' || select.dataset.loading === 'true') return;
    select.dataset.loading = 'true';
    apiPost('/mall/characters', { token: localStorage.getItem('userToken') })
        .then(response => {
            if (!response.ok || !response.data.ok) throw new Error(response.data.message || 'Could not load your game characters.');
            mallCharacters = response.data.characters || [];
            select.innerHTML = '';
            select.dataset.loading = 'false';
            select.dataset.loaded = 'true';
            if (mallCharacters.length === 0) {
                select.innerHTML = '<option value="">No game characters found</option>';
                renderMallProducts();
                return;
            }
            select.innerHTML = '<option value="">Choose a character</option>' + mallCharacters.map(character =>
                `<option value="${character.id}">${escapeHtml(character.name)} (Slot ${character.slot})</option>`
            ).join('');
            renderMallProducts();
        })
        .catch(error => {
            select.dataset.loading = 'false';
            select.innerHTML = '<option value="">Characters unavailable</option>';
            showMessage(error.message, 'error');
        });
}

function filterMall(category, button) {
    activeMallCategory = category;
    document.querySelectorAll('.category-btn').forEach(item => item.classList.remove('active'));
    if (button) button.classList.add('active');
    renderMallProducts();
}

function renderMallProducts() {
    const search = document.getElementById('mallSearch').value.trim().toLowerCase();
    const available = mallProducts.filter(product => {
        const categoryMatches = activeMallCategory === 'all' || product.category === activeMallCategory;
        const searchMatches = !search ||
            String(product.id).includes(search) ||
            product.name.toLowerCase().includes(search) ||
            product.category.toLowerCase().includes(search) ||
            product.description.toLowerCase().includes(search);
        return categoryMatches && searchMatches;
    });
    const canBuy = mallCharacters.length > 0;
    if (available.length === 0) {
        document.getElementById('mallGrid').innerHTML = '<p class="mall-empty">No matching in-game items were found.</p>';
        return;
    }

    document.getElementById('mallGrid').innerHTML = available.map(product => {
        const average = Number(product.average) || 0;
        const stars = '★'.repeat(Math.round(average)) + '☆'.repeat(5 - Math.round(average));
        return `
            <article class="shop-item">
                <div class="item-image" role="img" aria-label="${escapeHtml(product.category)} item">${escapeHtml(product.icon)}</div>
                <h4>${escapeHtml(product.name)}</h4>
                <p class="item-desc">${escapeHtml(product.description)}</p>
                <div class="item-rating" aria-label="${average.toFixed(1)} out of 5 stars">${stars} ${average ? average.toFixed(1) : 'No ratings'} (${Number(product.reviewCount) || 0})</div>
                <div class="item-price"><span class="item-old-price">${Number(product.basePrice).toLocaleString()} 🎫</span>${Number(product.price).toLocaleString()} 🎫</div>
                <button class="btn btn-primary" data-action="buy-item" data-item-id="${product.id}" ${canBuy ? '' : 'disabled'}>Buy</button>
                <button class="btn btn-secondary" data-action="view-reviews" data-item-id="${product.id}">Reviews</button>
            </article>
        `;
    }).join('');
}

function buyItem(itemId) {
    const product = mallProducts.find(item => Number(item.id) === itemId);
    const charID = document.getElementById('mallCharacter').value;
    if (!product) {
        showMessage('This item is no longer available.', 'error');
        return;
    }
    if (!charID) {
        showMessage('Choose the character that should receive this item.', 'error');
        return;
    }
    if (!confirm(`Buy ${product.name} for ${Number(product.price).toLocaleString()} Mall points? It will be delivered when the selected character next logs in.`)) return;

    const requestStorageKey = `mallRequest_${currentUser}_${charID}_${itemId}`;
    let requestId = localStorage.getItem(requestStorageKey);
    if (!requestId) {
        requestId = Date.now().toString(36) + '-' + Math.random().toString(36).slice(2);
        localStorage.setItem(requestStorageKey, requestId);
    }
    apiPost('/mall/purchase', {
        token: localStorage.getItem('userToken'),
        characterId: charID,
        itemId: String(itemId),
        requestId: requestId
    }).then(response => {
        if (!response.ok || !response.data.ok) {
            if (response.status < 500) localStorage.removeItem(requestStorageKey);
            throw new Error(response.data.message || 'Purchase failed.');
        }
        localStorage.removeItem(requestStorageKey);
        document.getElementById('mallPoints').textContent = Number(response.data.balance).toLocaleString();
        showMessage(response.data.message, 'success');
    }).catch(error => showMessage(error.message, 'error'));
}

function viewItemReviews(itemId) {
    document.getElementById('mallReviewPanel').classList.remove('hidden');
    document.getElementById('reviewItemId').value = itemId;
    document.getElementById('mallReviews').innerHTML = '<p class="item-review">Loading reviews…</p>';
    apiPost('/mall/reviews', {
        token: localStorage.getItem('userToken'),
        itemId: String(itemId)
    }).then(response => {
        if (!response.ok || !response.data.ok) throw new Error(response.data.message || 'Could not load reviews.');
        const reviews = response.data.reviews || [];
        document.getElementById('mallReviews').innerHTML = reviews.length
            ? reviews.map(review => `<div class="item-review"><strong>${escapeHtml(review.username)}</strong> · ${'★'.repeat(Number(review.rating))}${'☆'.repeat(5 - Number(review.rating))}<br>${escapeHtml(review.review)}</div>`).join('')
            : '<p class="item-review">No reviews yet.</p>';
    }).catch(error => {
        document.getElementById('mallReviews').innerHTML = '<p class="item-review">Reviews could not be loaded.</p>';
        showMessage(error.message, 'error');
    });
}

function submitMallReview(e) {
    e.preventDefault();
    apiPost('/mall/submit-review', {
        token: localStorage.getItem('userToken'),
        itemId: document.getElementById('reviewItemId').value,
        rating: document.getElementById('reviewRating').value,
        review: document.getElementById('reviewText').value.trim()
    }).then(response => {
        if (!response.ok || !response.data.ok) throw new Error(response.data.message || 'Could not save your review.');
        document.getElementById('reviewText').value = '';
        showMessage(response.data.message, 'success');
        mallLoadPromise = null;
        loadMallItems();
        viewItemReviews(Number(document.getElementById('reviewItemId').value));
    }).catch(error => showMessage(error.message, 'error'));
}

function subscribeMallNewsletter(e) {
    e.preventDefault();
    if (!document.getElementById('newsletterConsent').checked) {
        showMessage('Please agree to receive Mall update emails.', 'error');
        return;
    }
    apiPost('/mall/newsletter', { email: document.getElementById('newsletterEmail').value.trim() })
        .then(response => {
            if (!response.ok || !response.data.ok) throw new Error(response.data.message || 'Subscription failed.');
            document.getElementById('mallNewsletterForm').reset();
            showMessage(response.data.message, 'success');
        }).catch(error => showMessage(error.message, 'error'));
}

function escapeHtml(value) {
    return String(value).replace(/[&<>"']/g, character => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    }[character]));
}

// Join event
function joinEvent(eventId) {
    showMessage('You joined the event! 🎉', 'success');
}

// Claim reward
function claimReward(rewardId) {
    const currentPoints = parseInt(document.getElementById('rewardPoints').textContent);
    const newPoints = currentPoints + 500;
    document.getElementById('rewardPoints').textContent = newPoints;
    localStorage.setItem('rewardPoints_' + currentUser, newPoints);
    showMessage('Reward claimed! +500 points 🎫', 'success');
}

// Change password
function changePassword() {
    const currentPass = document.getElementById('currentPassword').value;
    const newPass = document.getElementById('newPassword').value;
    const confirmPass = document.getElementById('confirmPassword').value;
    
    if (!currentPass || !newPass || !confirmPass) {
        showMessage('Please fill in all password fields', 'error');
        return;
    }
    
    if (newPass !== confirmPass) {
        showMessage('Passwords do not match', 'error');
        return;
    }
    
    if (newPass.length < 4) {
        showMessage('Password must be at least 4 characters', 'error');
        return;
    }
    
    apiPost('/change-password', {
        token: localStorage.getItem('userToken'),
        currentPassword: currentPass,
        newPassword: newPass
    }).then(response => {
        if (!response.ok || !response.data.ok) throw new Error(response.data.message || 'Password update failed.');
        showMessage(response.data.message, 'success');
        document.getElementById('currentPassword').value = '';
        document.getElementById('newPassword').value = '';
        document.getElementById('confirmPassword').value = '';
    }).catch(error => showMessage(error.message, 'error'));
}

// Update countdown timers
function updateTimers() {
    const timers = [
        { id: 'timer1', days: 7 },
        { id: 'timer2', days: 14 },
        { id: 'timer3', days: -3 }, // negative = upcoming
        { id: 'timer4', days: 21 }
    ];
    
    timers.forEach(timer => {
        const el = document.getElementById(timer.id);
        if (el) {
            const text = timer.days > 0 ? timer.days + ' day' + (timer.days !== 1 ? 's' : '') : 
                         timer.days < 0 ? (-timer.days) + ' day' + (-timer.days !== 1 ? 's' : '') : 'Now';
            el.textContent = text;
        }
    });
}

// Logout
function logout() {
    if (confirm('Are you sure you want to logout?')) {
        localStorage.removeItem('userToken');
        localStorage.removeItem('username');
        window.location.href = 'register.html';
    }
}

// Show message
function showMessage(text, type = 'success') {
    const msgEl = document.getElementById('statusMessage');
    msgEl.textContent = text;
    msgEl.className = 'status-message show ' + type;
    
    setTimeout(() => {
        msgEl.classList.remove('show');
    }, 4000);
}

