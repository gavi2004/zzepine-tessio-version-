class VersionManager {
    token = null;
    currentUser = null;
    constructor() {
        this.apiBase = window.location.origin;
        this.currentVersion = null;
        this.initializeElements();
        this.loadAuthFromStorage();
        this.bindEvents();
        this.loadInitialData();
        this.startStatusCheck();
    }

    initializeElements() {
        // Status elements
        this.serverStatus = document.getElementById('serverStatus');
        this.serverStatusText = document.getElementById('serverStatusText');
        this.currentVersionEl = document.getElementById('currentVersion');
        this.lastUpdateEl = document.getElementById('lastUpdate');

        // Form elements
        this.newVersionInput = document.getElementById('newVersion');
        this.adminKeyInput = document.getElementById('adminKey');
        this.testVersionInput = document.getElementById('testVersion');

        // Buttons
        this.updateVersionBtn = document.getElementById('updateVersionBtn');
        this.validateBtn = document.getElementById('validateBtn');
        this.testValidationBtn = document.getElementById('testValidationBtn');
        this.clearLogBtn = document.getElementById('clearLogBtn');
        this.refreshHistoryBtn = document.getElementById('refreshHistoryBtn');
        this.refreshAuditsBtn = document.getElementById('refreshAuditsBtn');
        this.exportAuditsBtn = document.getElementById('exportAuditsBtn');

        // Modal elements
        this.confirmModal = document.getElementById('confirmModal');
        this.closeModal = document.getElementById('closeModal');
        this.confirmUpdate = document.getElementById('confirmUpdate');
        this.cancelUpdate = document.getElementById('cancelUpdate');
        this.modalCurrentVersion = document.getElementById('modalCurrentVersion');
        this.modalNewVersion = document.getElementById('modalNewVersion');

        // Auth
        this.loginBtn = document.getElementById('loginBtn');
        this.logoutBtn = document.getElementById('logoutBtn');
        this.loginUser = document.getElementById('loginUser');
        this.loginPass = document.getElementById('loginPass');
        this.authInfo = document.getElementById('authInfo');
        this.loginForm = document.getElementById('loginForm');
        this.currentUserEl = document.getElementById('currentUser');

        // Results and log
        this.validationResult = document.getElementById('validationResult');
        this.activityLog = document.getElementById('activityLog');
        this.toastContainer = document.getElementById('toastContainer');
    }

    bindEvents() {
        this.updateVersionBtn.addEventListener('click', () => this.showUpdateModal());
        this.validateBtn.addEventListener('click', () => this.validateVersionFormat());
        this.testValidationBtn.addEventListener('click', () => this.testClientValidation());
        this.clearLogBtn.addEventListener('click', () => this.clearLog());
        this.refreshHistoryBtn.addEventListener('click', () => this.loadHistory());
        this.refreshAuditsBtn.addEventListener('click', () => this.loadAudits());
        this.exportAuditsBtn.addEventListener('click', () => this.exportAuditsCsv());

        // Modal events
        this.closeModal.addEventListener('click', () => this.hideModal());
        this.cancelUpdate.addEventListener('click', () => this.hideModal());
        this.confirmUpdate.addEventListener('click', () => this.updateVersion());

        // Input validation
        this.newVersionInput.addEventListener('input', () => this.validateInput(this.newVersionInput));
        this.testVersionInput.addEventListener('input', () => this.validateInput(this.testVersionInput));

        // Close modal on outside click
        this.confirmModal.addEventListener('click', (e) => {
            if (e.target === this.confirmModal) this.hideModal();
        });

        // Enter key handlers
        this.adminKeyInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.showUpdateModal();
        });
        this.testVersionInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.testClientValidation();
        });
        this.loginPass.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.login();
        });

        this.loginBtn.addEventListener('click', () => this.login());
        this.logoutBtn.addEventListener('click', () => this.logout());
    }

    loadAuthFromStorage() {
        const token = localStorage.getItem('token');
        const user = localStorage.getItem('user');
        if (token && user) {
            this.token = token;
            this.currentUser = JSON.parse(user);
            this.updateAuthUI(true);
        } else {
            this.updateAuthUI(false);
        }
    }

    updateAuthUI(authed) {
        if (authed) {
            this.authInfo.classList.remove('hidden');
            this.loginForm.classList.add('hidden');
            this.currentUserEl.textContent = `Conectado como ${this.currentUser.username}`;
        } else {
            this.authInfo.classList.add('hidden');
            this.loginForm.classList.remove('hidden');
            this.currentUserEl.textContent = '';
        }
    }

    async login() {
        const username = this.loginUser.value.trim();
        const password = this.loginPass.value.trim();
        if (!username || !password) {
            this.showToast('warning', 'Ingresa usuario y contraseña');
            return;
        }
        try {
            const res = await fetch(`${this.apiBase}/api/auth/login`, {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });
            const data = await res.json();
            if (!data.success) throw new Error(data.message || 'Login inválido');
            this.token = data.token;
            this.currentUser = data.user;
            localStorage.setItem('token', this.token);
            localStorage.setItem('user', JSON.stringify(this.currentUser));
            this.updateAuthUI(true);
            this.showToast('success', 'Inicio de sesión exitoso');
            this.loadHistory();
            this.loadAudits();
        } catch (e) {
            this.showToast('error', e.message);
        }
    }

    logout() {
        this.token = null;
        this.currentUser = null;
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        this.updateAuthUI(false);
        this.showToast('info', 'Sesión cerrada');
    }

    async loadInitialData() {
        this.addLogEntry('info', 'Cargando información del servidor...');
        try {
            const response = await fetch(`${this.apiBase}/api/version`);
            const data = await response.json();
            if (data.success) {
                this.currentVersion = data.version;
                this.currentVersionEl.textContent = data.version;
                this.lastUpdateEl.textContent = new Date(data.timestamp).toLocaleString('es-ES');
                this.setServerStatus(true);
                this.addLogEntry('success', `Versión actual cargada: ${data.version}`);
            } else {
                throw new Error('Respuesta inválida del servidor');
            }
        } catch (error) {
            console.error('Error loading initial data:', error);
            this.setServerStatus(false);
            this.addLogEntry('error', 'Error al conectar con el servidor');
        }
    }

    startStatusCheck() {
        setInterval(() => this.checkServerStatus(), 30000);
    }

    async checkServerStatus() {
        try {
            const response = await fetch(`${this.apiBase}/api/version`);
            const data = await response.json();
            if (data.success && this.currentVersion !== data.version) {
                this.currentVersion = data.version;
                this.currentVersionEl.textContent = data.version;
                this.lastUpdateEl.textContent = new Date(data.timestamp).toLocaleString('es-ES');
                this.addLogEntry('info', `Versión actualizada desde servidor: ${data.version}`);
            }
            this.setServerStatus(true);
        } catch (error) {
            this.setServerStatus(false);
        }
    }

    setServerStatus(online) {
        if (online) {
            this.serverStatus.className = 'status-indicator online';
            this.serverStatus.innerHTML = '<i class="fas fa-circle"></i><span>Conectado</span>';
            this.serverStatusText.textContent = 'Online';
            this.serverStatusText.className = 'status-online';
        } else {
            this.serverStatus.className = 'status-indicator offline';
            this.serverStatus.innerHTML = '<i class="fas fa-circle"></i><span>Desconectado</span>';
            this.serverStatusText.textContent = 'Offline';
            this.serverStatusText.className = 'status-offline';
        }
    }

    validateInput(input) {
        const version = input.value.trim();
        const isValid = /^\d+\.\d+\.\d+$/.test(version);
        if (version && !isValid) {
            input.style.borderColor = 'var(--danger-color)';
        } else {
            input.style.borderColor = 'var(--border-color)';
        }
        return isValid;
    }

    validateVersionFormat() {
        const version = this.newVersionInput.value.trim();
        if (!version) {
            this.showToast('warning', 'Ingresa una versión para validar');
            return;
        }
        if (this.validateInput(this.newVersionInput)) {
            this.showToast('success', `Formato válido: ${version}`);
            this.addLogEntry('success', `Formato de versión validado: ${version}`);
        } else {
            this.showToast('error', 'Formato inválido. Use x.y.z (ej: 1.0.0)');
            this.addLogEntry('error', `Formato de versión inválido: ${version}`);
        }
    }

    showUpdateModal() {
        const newVersion = this.newVersionInput.value.trim();
        const adminKey = this.adminKeyInput.value.trim();
        if (!newVersion) {
            this.showToast('warning', 'Ingresa una nueva versión');
            return;
        }
        if (!this.validateInput(this.newVersionInput)) {
            this.showToast('error', 'Formato de versión inválido');
            return;
        }
        if (!this.token && !adminKey) {
            this.showToast('warning', 'Inicia sesión o ingresa la clave de administrador (legacy)');
            return;
        }
        this.modalCurrentVersion.textContent = this.currentVersion || 'Desconocida';
        this.modalNewVersion.textContent = newVersion;
        this.confirmModal.classList.remove('hidden');
    }

    hideModal() { this.confirmModal.classList.add('hidden'); }

    async updateVersion() {
        const newVersion = this.newVersionInput.value.trim();
        const adminKey = this.adminKeyInput.value.trim();
        this.hideModal();
        this.updateVersionBtn.disabled = true;
        this.updateVersionBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Actualizando...';
        try {
            const headers = { 'Content-Type': 'application/json' };
            if (this.token) headers['Authorization'] = `Bearer ${this.token}`;
            const body = { version: newVersion };
            if (!this.token && adminKey) body.adminKey = adminKey; // compat
            const response = await fetch(`${this.apiBase}/api/version`, { method: 'PUT', headers, body: JSON.stringify(body) });
            const data = await response.json();
            if (data.success) {
                this.currentVersion = newVersion;
                this.currentVersionEl.textContent = newVersion;
                this.lastUpdateEl.textContent = new Date().toLocaleString('es-ES');
                this.newVersionInput.value = '';
                this.adminKeyInput.value = '';
                this.showToast('success', `Versión actualizada a ${newVersion}`);
                this.addLogEntry('success', `Versión actualizada: ${data.oldVersion} → ${newVersion}`);
                this.loadHistory();
                this.loadAudits();
            } else {
                this.showToast('error', data.message);
                this.addLogEntry('error', `Error actualizando versión: ${data.message}`);
            }
        } catch (error) {
            console.error('Update error:', error);
            this.showToast('error', 'Error de conexión');
            this.addLogEntry('error', 'Error de conexión al actualizar versión');
        } finally {
            this.updateVersionBtn.disabled = false;
            this.updateVersionBtn.innerHTML = '<i class="fas fa-upload"></i> Actualizar Versión';
        }
    }

    async testClientValidation() {
        const testVersion = this.testVersionInput.value.trim();
        if (!testVersion) { this.showToast('warning', 'Ingresa una versión para probar'); return; }
        if (!this.validateInput(this.testVersionInput)) { this.showToast('error', 'Formato de versión inválido'); return; }
        this.testValidationBtn.disabled = true;
        this.testValidationBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Probando...';
        try {
            const response = await fetch(`${this.apiBase}/api/validate`, {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ version: testVersion })
            });
            const data = await response.json();
            this.showValidationResult(data);
            this.addLogEntry(data.allowed ? 'success' : 'warning', `Validación de ${testVersion}: ${data.message}`);
        } catch (error) {
            console.error('Validation error:', error);
            this.showToast('error', 'Error de conexión');
            this.addLogEntry('error', 'Error al probar validación');
        } finally {
            this.testValidationBtn.disabled = false;
            this.testValidationBtn.innerHTML = '<i class="fas fa-flask"></i> Probar Validación';
        }
    }

    showValidationResult(result) {
        const className = result.allowed ? 'success' : 'error';
        const icon = result.allowed ? 'fas fa-check-circle' : 'fas fa-times-circle';
        this.validationResult.innerHTML = `
            <div class="validation-header">
                <i class="${icon}"></i>
                <strong>${result.allowed ? 'ACCESO PERMITIDO' : 'ACCESO DENEGADO'}</strong>
            </div>
            <div class="validation-details">
                <p><strong>Mensaje:</strong> ${result.message}</p>
                <p><strong>Versión del cliente:</strong> ${result.clientVersion}</p>
                <p><strong>Versión del servidor:</strong> ${result.serverVersion}</p>
            </div>
        `;
        this.validationResult.className = `validation-result ${className}`;
        this.validationResult.classList.remove('hidden');
    }

    async loadHistory() {
        if (!this.token) return;
        try {
            const res = await fetch(`${this.apiBase}/api/versions/history`, { headers: { 'Authorization': `Bearer ${this.token}` } });
            const data = await res.json();
            if (!data.success) return;
            const tbody = document.querySelector('#historyTable tbody');
            tbody.innerHTML = '';
            data.history.forEach(row => {
                const tr = document.createElement('tr');
                tr.innerHTML = `<td>${row.id}</td><td>${row.version}</td><td>${row.changed_by_username || '-'}</td><td>${new Date(row.changed_at).toLocaleString('es-ES')}</td>`;
                tbody.appendChild(tr);
            });
        } catch {}
    }

    async loadAudits() {
        if (!this.token) return;
        try {
            const q = document.getElementById('auditQ').value.trim();
            const from = document.getElementById('auditFrom').value;
            const to = document.getElementById('auditTo').value;
            const params = new URLSearchParams();
            if (q) params.set('q', q);
            if (from) params.set('from', from);
            if (to) params.set('to', to);
            const res = await fetch(`${this.apiBase}/api/audits?${params.toString()}`, { headers: { 'Authorization': `Bearer ${this.token}` } });
            const data = await res.json();
            if (!data.success) return;
            const tbody = document.querySelector('#auditTable tbody');
            tbody.innerHTML = '';
            data.audits.forEach(row => {
                const tr = document.createElement('tr');
                tr.innerHTML = `<td>${row.id}</td><td>${row.action}</td><td>${row.username || '-'}</td><td><pre>${(row.details || '').substring(0, 500)}</pre></td><td>${new Date(row.created_at).toLocaleString('es-ES')}</td>`;
                tbody.appendChild(tr);
            });
        } catch {}
    }

    exportAuditsCsv() {
        const rows = Array.from(document.querySelectorAll('#auditTable tbody tr'));
        const csv = ['id,action,username,details,created_at'];
        rows.forEach(r => {
            const cols = Array.from(r.querySelectorAll('td')).map(td => '"' + (td.innerText || '').replace(/"/g, '""') + '"');
            csv.push(cols.join(','));
        });
        const blob = new Blob([csv.join('\n')], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `audits_${new Date().toISOString().slice(0,19).replace(/[:T]/g,'-')}.csv`;
        a.click();
        URL.revokeObjectURL(url);
    }

    addLogEntry(type, message) {
        const timestamp = new Date().toLocaleTimeString('es-ES');
        const logEntry = document.createElement('div');
        logEntry.className = `log-entry ${type}`;
        logEntry.innerHTML = `
            <span class="timestamp">${timestamp}</span>
            <span class="message">${message}</span>
        `;
        this.activityLog.insertBefore(logEntry, this.activityLog.firstChild);
        while (this.activityLog.children.length > 50) {
            this.activityLog.removeChild(this.activityLog.lastChild);
        }
    }

    clearLog() { this.activityLog.innerHTML = ''; this.addLogEntry('info', 'Log de actividad limpiado'); this.showToast('info', 'Log limpiado correctamente'); }

    showToast(type, message) {
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.textContent = message;
        this.toastContainer.appendChild(toast);
        setTimeout(() => toast.remove(), 4000);
    }
}

document.addEventListener('DOMContentLoaded', () => { new VersionManager(); });
