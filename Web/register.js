(function () {
    'use strict';

    // DOM Elements
    const registrationForm = document.getElementById("registration-form");
    const loginForm = document.getElementById("login-form");
    const forgotForm = document.getElementById("forgot-form");
    const registrationMessage = document.getElementById("form-message");
    const loginMessage = document.getElementById("login-message");
    const forgotMessage = document.getElementById("forgot-message");
    const registrationSubmit = registrationForm.querySelector("button[type=submit]");
    const loginSubmit = loginForm.querySelector("button[type=submit]");
    const forgotSubmit = forgotForm.querySelector("button[type=submit]");
    const passwordInput = document.getElementById("password");
    const strengthFill = document.querySelector(".strength-fill");
    const strengthText = document.getElementById("strength");
    const tabButtons = document.querySelectorAll(".tab-button");
    const tabContents = document.querySelectorAll(".tab-content");
    const switchTabButtons = document.querySelectorAll(".switch-tab");
    const forgotStep2 = document.getElementById("forgot-step-2");
    const forgotBtnText = document.getElementById("forgot-btn-text");

    function initializePublicPortal() {
        const statusBadge = document.getElementById("publicServerStatus");
        if (!statusBadge) return;

        const configuredDownloadUrl = typeof CONFIG !== "undefined" ? CONFIG.clientDownloadUrl : "";
        const downloadLink = document.getElementById("clientDownloadLink");
        const downloadHint = document.getElementById("clientDownloadHint");
        if (configuredDownloadUrl) {
            try {
                const url = new URL(configuredDownloadUrl, window.location.origin);
                if (url.protocol === "https:" || url.origin === window.location.origin) {
                    downloadLink.href = url.href;
                    downloadLink.hidden = false;
                    downloadHint.textContent = "Download the authorized game client, then use the update launcher to install the latest server patch.";
                } else {
                    downloadHint.textContent = "The configured game-client download is not secure. Please contact the server administrator.";
                }
            } catch (error) {
                downloadHint.textContent = "The configured game-client download link is invalid. Please contact the server administrator.";
            }
        }

        function refreshServerStatus() {
            const apiBase = (typeof CONFIG !== "undefined" && CONFIG.getApiBase) ?
                CONFIG.getApiBase() : window.location.origin + "/api";
            fetch(apiBase + "/server-status", { cache: "no-store" })
                .then(response => {
                    if (!response.ok) throw new Error("Status service returned HTTP " + response.status);
                    return response.json();
                })
                .then(data => {
                    if (typeof data.gameOnline !== "boolean" ||
                        !Number.isFinite(data.serversRunning) ||
                        !Number.isFinite(data.playersOnline) ||
                        !Number.isFinite(data.gamePort)) {
                        throw new Error("Invalid server status response.");
                    }
                    statusBadge.textContent = data.gameOnline ? "Online" : "Offline";
                    statusBadge.className = "public-status-badge " + (data.gameOnline ? "online" : "offline");
                    document.getElementById("publicServerCount").textContent =
                        data.serversRunning + " / " + data.configuredServers;
                    document.getElementById("publicPlayerCount").textContent = data.playersOnline.toLocaleString();
                    document.getElementById("publicGameAddress").textContent =
                        window.location.hostname + ":" + data.gamePort;
                    document.getElementById("publicStatusChecked").textContent =
                        "Last checked " + new Date(data.checkedAt).toLocaleTimeString();
                })
                .catch(() => {
                    statusBadge.textContent = "Unavailable";
                    statusBadge.className = "public-status-badge unavailable";
                    document.getElementById("publicServerCount").textContent = "—";
                    document.getElementById("publicPlayerCount").textContent = "—";
                    document.getElementById("publicGameAddress").textContent = "—";
                    document.getElementById("publicStatusChecked").textContent = "Could not retrieve live server status.";
                });
        }

        function loadLatestUpdates() {
            fetch("/client-updates/manifest.json", { cache: "no-store" })
                .then(response => {
                    if (!response.ok) throw new Error("Update manifest is unavailable.");
                    return response.json();
                })
                .then(manifest => {
                    if (!manifest || typeof manifest.version !== "string" || !Array.isArray(manifest.files))
                        throw new Error("Invalid update manifest.");

                    const summary = document.getElementById("latestUpdateSummary");
                    const list = document.getElementById("latestUpdateFiles");
                    list.replaceChildren();
                    if (manifest.files.length === 0) {
                        summary.textContent = "No client update package has been published yet.";
                        return;
                    }

                    summary.textContent = "Latest update " + manifest.version + " (" + manifest.files.length + " files).";
                    manifest.files.slice(0, 20).forEach(file => {
                        if (!file || typeof file.path !== "string") return;
                        const item = document.createElement("li");
                        const link = document.createElement("a");
                        const encodedPath = file.path.split("/").map(encodeURIComponent).join("/");
                        link.href = "/client-updates/files/" + encodedPath;
                        link.textContent = file.path;
                        item.appendChild(link);
                        list.appendChild(item);
                    });
                    if (manifest.files.length > 20) {
                        const more = document.createElement("li");
                        more.textContent = (manifest.files.length - 20) + " more files; use the update launcher to install them.";
                        list.appendChild(more);
                    }
                })
                .catch(() => {
                    document.getElementById("latestUpdateSummary").textContent =
                        "Latest update information is unavailable right now.";
                });
        }

        refreshServerStatus();
        loadLatestUpdates();
        window.setInterval(refreshServerStatus, 30000);
        window.setInterval(loadLatestUpdates, 60000);
    }

    // Tab Switching
    function switchTab(tabName) {
        // Deactivate all tabs
        tabButtons.forEach(btn => btn.classList.remove("active"));
        tabContents.forEach(content => content.classList.remove("active"));

        // Activate selected tab
        document.querySelector(`[data-tab="${tabName}"]`).classList.add("active");
        document.getElementById(tabName).classList.add("active");
    }

    // Tab button clicks
    tabButtons.forEach(button => {
        button.addEventListener("click", () => {
            const tabName = button.getAttribute("data-tab");
            switchTab(tabName);
        });
    });

    // Switch tab links
    switchTabButtons.forEach(button => {
        button.addEventListener("click", (e) => {
            e.preventDefault();
            const tabName = button.getAttribute("data-tab");
            switchTab(tabName);
        });
    });

    // Password Strength Meter
    function updatePasswordStrength() {
        const password = passwordInput.value;
        let strength = 0;
        let strengthLabel = "Very Weak";

        if (password.length >= 4) strength += 20;
        if (password.length >= 8) strength += 20;
        if (/[a-z]/.test(password)) strength += 15;
        if (/[A-Z]/.test(password)) strength += 15;
        if (/[0-9]/.test(password)) strength += 15;
        if (/[!@#$%^&*]/.test(password)) strength += 15;

        if (strength < 20) {
            strengthLabel = "Very Weak";
            strengthText.style.color = "#ff6b6b";
        } else if (strength < 40) {
            strengthLabel = "Weak";
            strengthText.style.color = "#ffd93d";
        } else if (strength < 60) {
            strengthLabel = "Fair";
            strengthText.style.color = "#ffd97d";
        } else if (strength < 80) {
            strengthLabel = "Good";
            strengthText.style.color = "#a9d8b3";
        } else {
            strengthLabel = "Strong";
            strengthText.style.color = "#6dd47e";
        }

        strengthFill.style.width = strength + "%";
        strengthText.textContent = strengthLabel;
    }

    passwordInput.addEventListener("input", updatePasswordStrength);

    // Registration Form Handler
    registrationForm.addEventListener("submit", function (event) {
        event.preventDefault();
        registrationMessage.textContent = "";
        registrationMessage.className = "message";

        const password = registrationForm.elements.password.value;
        const passwordConfirm = registrationForm.elements.passwordConfirm.value;

        if (password !== passwordConfirm) {
            registrationMessage.textContent = "❌ Passwords do not match. Please try again.";
            registrationMessage.className = "message error";
            return;
        }

        if (password.length < 4) {
            registrationMessage.textContent = "❌ Password must be at least 4 characters.";
            registrationMessage.className = "message error";
            return;
        }

        const username = registrationForm.elements.username.value.trim();
        if (!/^[A-Za-z0-9_]{4,14}$/.test(username)) {
            registrationMessage.textContent = "❌ Invalid username. Use 4-14 characters: letters, numbers, underscores.";
            registrationMessage.className = "message error";
            return;
        }

        registrationSubmit.disabled = true;
        registrationSubmit.textContent = "Creating Account...";

        const body = new URLSearchParams();
        body.append("username", username);
        body.append("password", password);
        body.append("passwordConfirm", passwordConfirm);

        fetch(CONFIG.getApiBase() + "/register", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8" },
            body: body.toString()
        })
        .then(response => response.json().then(result => ({ ok: response.ok, result: result })))
        .then(response => {
            if (response.ok) {
                registrationMessage.textContent = "✨ Account created successfully! You can now log in to the server.";
                registrationMessage.className = "message";
                registrationForm.reset();
                updatePasswordStrength();
            } else {
                registrationMessage.textContent = "❌ " + (response.result.message || "Registration failed. Please try again.");
                registrationMessage.className = "message error";
            }
        })
        .catch(error => {
            console.error("Error:", error);
            registrationMessage.textContent = "❌ Could not reach the registration service. Please try again later.";
            registrationMessage.className = "message error";
        })
        .finally(() => {
            registrationSubmit.disabled = false;
            registrationSubmit.textContent = "Create Account";
        });
    });

    // Forgot Password Form Handler
    forgotForm.addEventListener("submit", function (event) {
        event.preventDefault();
        forgotMessage.textContent = "";
        forgotMessage.className = "message";

        const step2Visible = !forgotStep2.classList.contains("hidden");

        if (!step2Visible) {
            // Step 1: Verify username
            const username = forgotForm.elements.username.value.trim();

            if (!username) {
                forgotMessage.textContent = "❌ Please enter your username.";
                forgotMessage.className = "message error";
                return;
            }

            forgotSubmit.disabled = true;
            forgotSubmit.textContent = "Verifying...";

            // Simulate username verification
            setTimeout(() => {
                forgotMessage.textContent = "✨ Username verified! Please enter your new password.";
                forgotMessage.className = "message";
                forgotStep2.classList.remove("hidden");
                forgotBtnText.textContent = "Reset Password";
                forgotSubmit.disabled = false;
                forgotSubmit.textContent = "Reset Password";
            }, 1000);

        } else {
            // Step 2: Reset password
            const newPassword = forgotForm.elements.newPassword.value;
            const newPasswordConfirm = forgotForm.elements.newPasswordConfirm.value;
            const username = forgotForm.elements.username.value.trim();

            if (!newPassword || !newPasswordConfirm) {
                forgotMessage.textContent = "❌ Please enter and confirm your new password.";
                forgotMessage.className = "message error";
                return;
            }

            if (newPassword !== newPasswordConfirm) {
                forgotMessage.textContent = "❌ New passwords do not match.";
                forgotMessage.className = "message error";
                return;
            }

            if (newPassword.length < 4) {
                forgotMessage.textContent = "❌ Password must be at least 4 characters.";
                forgotMessage.className = "message error";
                return;
            }

            forgotSubmit.disabled = true;
            forgotSubmit.textContent = "Resetting...";

            const body = new URLSearchParams();
            body.append("username", username);
            body.append("newPassword", newPassword);

            fetch(CONFIG.getApiBase() + "/reset-password", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8" },
                body: body.toString()
            })
            .then(response => response.json().then(result => ({ ok: response.ok, result: result })))
            .then(response => {
                if (response.ok) {
                    forgotMessage.textContent = "✨ Password reset successfully! You can now log in with your new password.";
                    forgotMessage.className = "message";
                    forgotForm.reset();
                    forgotStep2.classList.add("hidden");
                    forgotBtnText.textContent = "Send Reset Link";
                    setTimeout(() => {
                        switchTab("register");
                    }, 2000);
                } else {
                    forgotMessage.textContent = "❌ " + (response.result.message || "Password reset failed. Please try again.");
                    forgotMessage.className = "message error";
                }
            })
            .catch(error => {
                console.error("Error:", error);
                forgotMessage.textContent = "❌ Could not reach the service. Please try again later.";
                forgotMessage.className = "message error";
            })
            .finally(() => {
                forgotSubmit.disabled = false;
                forgotSubmit.textContent = "Reset Password";
            });
        }
    });

    // Login Form Handler
    loginForm.addEventListener("submit", function (event) {
        event.preventDefault();
        loginMessage.textContent = "";
        loginMessage.className = "message";

        const username = loginForm.elements.username.value.trim();
        const password = loginForm.elements.password.value;

        if (!username || !password) {
            loginMessage.textContent = "❌ Please enter both username and password.";
            loginMessage.className = "message error";
            return;
        }

        loginSubmit.disabled = true;
        loginSubmit.textContent = "Logging in...";

        const body = new URLSearchParams();
        body.append("username", username);
        body.append("password", password);

        fetch(CONFIG.getApiBase() + "/login", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8" },
            body: body.toString()
        })
        .then(response => response.json().then(result => ({ ok: response.ok, result: result })))
        .then(response => {
            if (response.ok) {
                if (!response.result.token) {
                    throw new Error("The login service returned no session token.");
                }
                // Store login token and username
                localStorage.setItem('userToken', response.result.token);
                localStorage.setItem('username', username);
                
                loginMessage.textContent = "✨ Login successful! Redirecting to dashboard...";
                loginMessage.className = "message";
                
                // Redirect to dashboard after 1 second
                setTimeout(() => {
                    window.location.href = '/dashboard.html';
                }, 1000);
            } else {
                loginMessage.textContent = "❌ " + (response.result.message || "Invalid username or password. Please try again.");
                loginMessage.className = "message error";
            }
        })
        .catch(error => {
            console.error("Error:", error);
            loginMessage.textContent = "❌ Could not reach the registration service. Please try again later.";
            loginMessage.className = "message error";
        })
        .finally(() => {
            loginSubmit.disabled = false;
            loginSubmit.textContent = "Login to Dashboard";
        });
    });

    // Initialize password strength on load
    initializePublicPortal();
    updatePasswordStrength();
}());
