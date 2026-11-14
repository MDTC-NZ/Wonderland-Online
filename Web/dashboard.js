// Dashboard JavaScript
let currentUser = null;
let userCharacters = [];

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    checkLoginStatus();
    loadCharacters();
    loadCommunityStats();
    updateTimers();
    setInterval(updateTimers, 1000);
});

// Check if user is logged in
function checkLoginStatus() {
    const token = localStorage.getItem('userToken');
    const username = localStorage.getItem('username');
    
    if (!token || !username) {
        window.location.href = 'register.html';
        return;
    }
    
    currentUser = username;
    document.getElementById('usernameDisplay').textContent = username;
    document.getElementById('accUsername').textContent = username;
    loadUserStats();
}

// Load user statistics
function loadUserStats() {
    // Try to fetch real stats from server API
    const token = localStorage.getItem('userToken');
    const username = localStorage.getItem('username');
    
    if (!token || !username) {
        setDefaultStats();
        return;
    }
    
    // Fetch from API
    fetch(CONFIG.getApiBase() + '/user-stats', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            username: username,
            token: token
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success && data.stats) {
            // Update with real stats from server
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

// Load characters from localStorage (for demo)
function loadCharacters() {
    const saved = localStorage.getItem('userCharacters_' + currentUser);
    userCharacters = saved ? JSON.parse(saved) : [];
    
    // Always ensure at least one test character for new users
    if (!saved || userCharacters.length === 0) {
        autoCreateTestCharacter();
    } else {
        renderCharacters();
        updateCharacterCount();
    }
}

// Auto-create a test character for new players
function autoCreateTestCharacter() {
    const testChar = {
        id: Date.now(),
        name: 'Adventurer',
        class: 'warrior',
        race: 'human',
        gender: 'male',
        skin: 'light',
        level: 1,
        experience: 0,
        gold: 0,
        createdAt: new Date().toISOString()
    };
    
    userCharacters.push(testChar);
    saveCharacters();
    renderCharacters();
    updateCharacterCount();
    showMessage('Welcome! Your first character "Adventurer" has been created! Ready to play?', 'success');
}

// Render character cards
function renderCharacters() {
    const container = document.getElementById('characters-list');
    const emptyState = document.getElementById('emptyState');
    const createBtn = document.querySelector('[onclick="toggleCreateForm()"]');
    
    if (userCharacters.length === 0) {
        container.innerHTML = '';
        emptyState.style.display = 'block';
        return;
    }
    
    emptyState.style.display = 'none';
    container.innerHTML = userCharacters.map((char, idx) => `
        <div class="character-card">
            <div class="char-icon">${getClassIcon(char.class)}</div>
            <div class="char-name">${char.name}</div>
            <div class="char-info">⚔️ ${char.class}</div>
            <div class="char-info">🧬 ${char.race}</div>
            <div class="char-info">⭐ Level <strong>${char.level || 1}</strong></div>
            <div class="char-info">💰 ${(char.gold || 0).toLocaleString()} Gold</div>
            <div class="char-level">Level ${char.level || 1}</div>
            <div class="char-actions">
                <button class="btn-play" onclick="playCharacter(${idx})">Play</button>
                <button class="btn-delete" onclick="deleteCharacter(${idx})">Delete</button>
            </div>
        </div>
    `).join('');
}

// Get class icon
function getClassIcon(className) {
    const icons = {
        'warrior': '⚔️',
        'mage': '🔮',
        'archer': '🏹',
        'cleric': '✨',
        'rogue': '🗡️',
        'paladin': '⚡'
    };
    return icons[className] || '⚔️';
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
        document.getElementById('charClass').value = '';
        document.getElementById('charRace').value = '';
        document.getElementById('charGender').value = '';
        document.querySelector('input[name="skin"]').checked = false;
    }
}

// Update character preview
document.addEventListener('change', function(e) {
    if (e.target.id === 'charClass') {
        const icon = getClassIcon(e.target.value);
        document.getElementById('previewClass').textContent = icon + ' ' + 
            (document.querySelector(`option[value="${e.target.value}"]`)?.textContent || 'Select class');
    }
    if (e.target.id === 'charRace') {
        document.getElementById('previewRace').textContent = 
            document.querySelector(`option[value="${e.target.value}"]`)?.textContent || 'Select race';
    }
    if (e.target.id === 'charName') {
        document.getElementById('previewName').textContent = e.target.value || 'Your Character';
    }
});

// Create character
function createCharacter() {
    const name = document.getElementById('charName').value.trim();
    const charClass = document.getElementById('charClass').value;
    const race = document.getElementById('charRace').value;
    const gender = document.getElementById('charGender').value;
    const skin = document.querySelector('input[name="skin"]:checked')?.value;
    
    // Validation
    if (!name || name.length < 3 || name.length > 20) {
        showMessage('Character name must be 3-20 characters', 'error');
        return;
    }
    
    if (!/^[a-zA-Z0-9_]+$/.test(name)) {
        showMessage('Character name can only contain letters, numbers, and underscores', 'error');
        return;
    }
    
    if (!charClass || !race || !gender || !skin) {
        showMessage('Please fill in all character details', 'error');
        return;
    }
    
    // Check for duplicate names
    if (userCharacters.some(c => c.name.toLowerCase() === name.toLowerCase())) {
        showMessage('A character with this name already exists', 'error');
        return;
    }
    
    // Create character
    const newChar = {
        id: Date.now(),
        name: name,
        class: charClass,
        race: race,
        gender: gender,
        skin: skin,
        level: 1,
        experience: 0,
        gold: 0,
        createdAt: new Date().toISOString()
    };
    
    userCharacters.push(newChar);
    saveCharacters();
    renderCharacters();
    updateCharacterCount();
    toggleCreateForm();
    showMessage('Character "' + name + '" created successfully! 🎉', 'success');
}

// Delete character
function deleteCharacter(idx) {
    if (confirm('Are you sure you want to delete ' + userCharacters[idx].name + '? This cannot be undone!')) {
        const deleted = userCharacters.splice(idx, 1)[0];
        saveCharacters();
        renderCharacters();
        updateCharacterCount();
        showMessage('Character "' + deleted.name + '" deleted', 'success');
    }
}

// Play character
function playCharacter(idx) {
    const char = userCharacters[idx];
    const token = localStorage.getItem('userToken');
    const apiBase = (typeof CONFIG !== 'undefined' && CONFIG.getApiBase) ? CONFIG.getApiBase() : 'http://localhost:8090/api';
    
    showMessage('Launching ' + char.name + '...', 'success');
    
    // Launch the game via API
    try {
        fetch(`${apiBase}/launch-game`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                character_name: char.name,
                character_id: char.id,
                user_token: token
            })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showMessage('Game launching... Check your game window!', 'success');
            } else {
                showMessage('Error launching game: ' + data.error, 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showMessage('Connection error. Make sure the registration server is running.', 'error');
        });
    } catch (error) {
        console.error('Error:', error);
        showMessage('Failed to launch game', 'error');
    }
}

// Save characters to localStorage
function saveCharacters() {
    localStorage.setItem('userCharacters_' + currentUser, JSON.stringify(userCharacters));
}

// Tab switching
function showTab(tabName) {
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
    event.target.classList.add('active');
    
    // Load tab-specific content
    if (tabName === 'community') {
        loadCommunityStats();
    } else if (tabName === 'shop') {
        loadMallItems();
    }
}

// Load community statistics
function loadCommunityStats() {
    // Get actual data from server
    const apiBase = (typeof CONFIG !== 'undefined' && CONFIG.getApiBase) ? CONFIG.getApiBase() : 'http://localhost:8090/api';
    
    fetch(`${apiBase}/server-stats`)
        .then(response => response.json())
        .then(data => {
            // Update with real data
            document.getElementById('activePlayers').textContent = data.activePlayers || 0;
            document.getElementById('totalPlayers').textContent = (data.totalAccounts || 0).toLocaleString();
            document.getElementById('totalGuilds').textContent = data.totalGuilds || 0;
            
            // Load leaderboard with actual characters
            loadLeaderboard(data.topCharacters || []);
        })
        .catch(error => {
            console.log('Using demo stats:', error);
            // Fallback to demo data
            document.getElementById('activePlayers').textContent = '1';
            document.getElementById('totalPlayers').textContent = '1';
            document.getElementById('totalGuilds').textContent = '0';
            loadLeaderboard([]);
        });
}

// Load leaderboard
function loadLeaderboard(characters) {
    // Use provided characters or demo data
    if (!characters || characters.length === 0) {
        characters = [
            { name: 'Adventurer', level: 1, exp: 0 }
        ];
    }
    
    const tbody = document.getElementById('leaderboardBody');
    tbody.innerHTML = characters.map((char, idx) => `
        <tr>
            <td>${idx + 1}</td>
            <td>${char.name}</td>
            <td>Level ${char.level}</td>
            <td>${char.exp.toLocaleString()} EXP</td>
        </tr>
    `).join('');
}

// Load mall items
function loadMallItems() {
    // Items are already in HTML, just ensure they're displayed
}

// Filter mall items
function filterMall(category) {
    // Update active button
    document.querySelectorAll('.category-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    event.target.classList.add('active');
    
    // In production, filter items by category
    // For now, show all items
}

// Buy item from mall
function buyItem(itemId) {
    const points = parseInt(document.getElementById('mallPoints').textContent);
    const prices = {
        'sword': 2500,
        'dragon': 5000,
        'mount': 3500,
        'crown': 4000,
        'aura': 1800,
        'costume': 2200
    };
    
    const price = prices[itemId] || 0;
    
    if (points < price) {
        showMessage('You need ' + (price - points) + ' more points!', 'error');
        return;
    }
    
    const newPoints = points - price;
    document.getElementById('mallPoints').textContent = newPoints;
    localStorage.setItem('mallPoints_' + currentUser, newPoints);
    showMessage('Purchase successful! ✨', 'success');
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
    const newPass = document.getElementById('newPassword').value;
    const confirmPass = document.getElementById('confirmPassword').value;
    
    if (!newPass || !confirmPass) {
        showMessage('Please fill in both password fields', 'error');
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
    
    // In production, send to API
    showMessage('Password changed successfully! 🔐', 'success');
    document.getElementById('newPassword').value = '';
    document.getElementById('confirmPassword').value = '';
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

// Auto-save progress every minute
setInterval(function() {
    if (currentUser && userCharacters.length > 0) {
        saveCharacters();
        // In production, send to server
    }
}, 60000);
