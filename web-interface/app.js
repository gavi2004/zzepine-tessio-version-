class VersionManager {
    constructor() {
        this.apiBase = window.location.origin;
        this.currentVersion = null;
        this.initializeElements();
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

        // Modal elements
        this.confirmModal = document.getElementById('confirmModal');
        this.closeModal = document.getElementById('closeModal');
        this.confirmUpdate = document.getElementById('confirmUpdate');
        this.cancelUpdate = document.getElementById('cancelUpdate');
        this.modalCurrentVersion = document.getElementById('modalCurrentVersion');
        this.modalNewVersion = document.getElementById('modalNewVersion');

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
        setInterval(() => this.checkServerStatus(), 30000); // Check every 30 seconds
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

        if (!adminKey) {
            this.showToast('warning', 'Ingresa la clave de administrador');
            return;
        }

        this.modalCurrentVersion.textContent = this.currentVersion || 'Desconocida';
        this.modalNewVersion.textContent = newVersion;
        this.confirmModal.classList.remove('hidden');
    }

    hideModal() {
        this.confirmModal.classList.add('hidden');
    }

    async updateVersion() {
        const newVersion = this.newVersionInput.value.trim();
        const adminKey = this.adminKeyInput.value.trim();

        this.hideModal();
        this.updateVersionBtn.disabled = true;
        this.updateVersionBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Actualizando...';

        try {
            const response = await fetch(`${this.apiBase}/api/version`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    version: newVersion,
                    adminKey: adminKey
                })
            });

            const data = await response.json();

            if (data.success) {
                this.currentVersion = newVersion;
                this.currentVersionEl.textContent = newVersion;
                this.lastUpdateEl.textContent = new Date().toLocaleString('es-ES');
                this.newVersionInput.value = '';
                this.adminKeyInput.value = '';
                
                this.showToast('success', `Versión actualizada a ${newVersion}`);
                this.addLogEntry('success', `Versión actualizada: ${data.oldVersion} → ${newVersion}`);
                
                if (!data.savedToFile) {
                    this.showToast('warning', 'Versión actualizada solo en memoria');
                    this.addLogEntry('warning', 'No se pudo guardar en archivo de configuración');
                }
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

        if (!testVersion) {
            this.showToast('warning', 'Ingresa una versión para probar');
            return;
        }

        if (!this.validateInput(this.testVersionInput)) {
            this.showToast('error', 'Formato de versión inválido');
            return;
        }

        this.testValidationBtn.disabled = true;
        this.testValidationBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Probando...';

        try {
            const response = await fetch(`${this.apiBase}/api/validate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    version: testVersion
                })
            });

            const data = await response.json();
            this.showValidationResult(data);
            this.addLogEntry(data.allowed ? 'success' : 'warning', 
                `Validación de ${testVersion}: ${data.message}`);

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

    addLogEntry(type, message) {
        const timestamp = new Date().toLocaleTimeString('es-ES');
        const logEntry = document.createElement('div');
        logEntry.className = `log-entry ${type}`;
        
        logEntry.innerHTML = `
            <span class="timestamp">${timestamp}</span>
            <span class="message">${message}</span>
        `;
        
        this.activityLog.insertBefore(logEntry, this.activityLog.firstChild);
        
        // Mantener solo las últimas 50 entradas
        while (this.activityLog.children.length > 50) {
            this.activityLog.removeChild(this.activityLog.lastChild);
        }
    }

    clearLog() {
        this.activityLog.innerHTML = '';
        this.addLogEntry('info', 'Log de actividad limpiado');
        this.showToast('info', 'Log limpiado correctamente');
    }

    showToast(type, message) {
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.textContent = message;
        
        this.toastContainer.appendChild(toast);
        
        setTimeout(() => {
            toast.remove();
        }, 4000);
    }
}

// Initialize the application when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new VersionManager();
});