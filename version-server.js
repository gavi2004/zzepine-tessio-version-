const express = require('express');
const fs = require('fs');
const path = require('path');
const jwt = require('jsonwebtoken');
const bcrypt = require('bcryptjs');
const mongoose = require('mongoose');

const app = express();
const PORT = process.env.PORT || 4569;
const JWT_SECRET = process.env.JWT_SECRET || 'supersecret-jwt';
const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://mongo:27017/gtav_injector';

// Middleware
app.use(express.json());
app.use(express.static(path.join(__dirname, 'web-interface'), { index: false }));
app.use((req, res, next) => {
  res.header('Access-Control-Allow-Origin', '*');
  res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept, Authorization');
  res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  if (req.method === 'OPTIONS') return res.sendStatus(200);
  next();
});

// Conexión MongoDB
mongoose.connect(MONGODB_URI, { autoIndex: true })
  .then(() => console.log('✅ Conectado a MongoDB'))
  .catch(err => console.error('❌ Error al conectar MongoDB:', err));

// Modelos
const UserSchema = new mongoose.Schema({
  username: { type: String, unique: true, required: true },
  password_hash: { type: String, required: true },
  role: { type: String, default: 'admin' },
  created_at: { type: Date, default: Date.now },
  last_login_at: { type: Date }
}, { versionKey: false });
const User = mongoose.model('User', UserSchema);

const VersionSchema = new mongoose.Schema({
  version: { type: String, required: true },
  changed_by_user_id: { type: mongoose.Schema.Types.ObjectId, ref: 'User' },
  changed_by_username: { type: String },
  changed_at: { type: Date, default: Date.now }
}, { versionKey: false });
const Version = mongoose.model('Version', VersionSchema);

const AuditSchema = new mongoose.Schema({
  action: { type: String, required: true },
  user_id: { type: mongoose.Schema.Types.ObjectId, ref: 'User' },
  username: { type: String },
  details: { type: Object },
  ip: { type: String },
  user_agent: { type: String },
  created_at: { type: Date, default: Date.now }
}, { versionKey: false });
const Audit = mongoose.model('Audit', AuditSchema);

// Config legacy
const configPath = path.join(__dirname, 'config.json');
let config = { version: '1.0.0', adminKey: 'admin123', updateTimestamp: new Date().toISOString() };
try { if (fs.existsSync(configPath)) config = JSON.parse(fs.readFileSync(configPath, 'utf8')); } catch {}

// Estado en memoria
let currentVersion = null;

// Utilidades
function audit(action, req, user, details) {
  try {
    Audit.create({
      action,
      user_id: user?._id || null,
      username: user?.username || null,
      details: details || null,
      ip: req.headers['x-forwarded-for'] || req.socket?.remoteAddress || null,
      user_agent: req.headers['user-agent'] || null,
    }).catch(() => {});
  } catch {}
}

function authMiddleware(req, res, next) {
  const auth = req.headers['authorization'] || '';
  const token = auth.startsWith('Bearer ') ? auth.slice(7) : null;
  if (!token) return res.status(401).json({ success: false, message: 'No autorizado' });
  try {
    const payload = jwt.verify(token, JWT_SECRET);
    req.user = payload;
    next();
  } catch {
    return res.status(401).json({ success: false, message: 'Token inválido o expirado' });
  }
}

function ensureAdmin(req, res, next) {
  if (req.user?.role !== 'admin') return res.status(403).json({ success: false, message: 'Prohibido' });
  next();
}

function compareVersions(v1, v2) {
  const a = (v1 || '0.0.0').split('.').map(Number);
  const b = (v2 || '0.0.0').split('.').map(Number);
  for (let i = 0; i < Math.max(a.length, b.length); i++) {
    const x = a[i] || 0, y = b[i] || 0;
    if (x > y) return 1; if (x < y) return -1;
  }
  return 0;
}

function isValidVersion(version) { return /^\d+\.\d+\.\d+$/.test(version); }

function saveConfig() { try { config.version = currentVersion; config.updateTimestamp = new Date().toISOString(); fs.writeFileSync(configPath, JSON.stringify(config, null, 2)); return true; } catch { return false; } }

async function loadCurrentVersion() {
  const last = await Version.findOne().sort({ _id: -1 }).lean();
  currentVersion = last?.version || process.env.CURRENT_VERSION || config.version;
  if (!last) { await Version.create({ version: currentVersion }); }
}

loadCurrentVersion().catch(console.error);

// Rutas
// Root -> login page
app.get('/', (req, res) => { res.sendFile(path.join(__dirname, 'web-interface', 'login.html')); });
// Optional: keep old panel at /panel
app.get('/panel', (req, res) => { res.sendFile(path.join(__dirname, 'web-interface', 'index.html')); });

// Registro: libre si no hay usuarios, luego requiere admin
app.post('/api/auth/register', async (req, res) => {
  const { username, password, role } = req.body || {};
  if (!username || !password) return res.status(400).json({ success: false, message: 'Usuario y contraseña requeridos' });
  const users = await User.countDocuments();
  if (users > 0) {
    const auth = req.headers['authorization'] || '';
    if (!auth.startsWith('Bearer ')) return res.status(401).json({ success: false, message: 'No autorizado' });
    try {
      const payload = jwt.verify(auth.slice(7), JWT_SECRET);
      if (payload.role !== 'admin') return res.status(403).json({ success: false, message: 'Prohibido' });
    } catch { return res.status(401).json({ success: false, message: 'Token inválido' }); }
  }
  try {
    const password_hash = bcrypt.hashSync(password, 10);
    const user = await User.create({ username, password_hash, role: role || 'admin' });
    audit('REGISTER_USER', req, user, { username, role: user.role });
    return res.json({ success: true, message: 'Usuario creado', id: user._id });
  } catch (e) {
    if (e.code === 11000) return res.status(409).json({ success: false, message: 'Usuario ya existe' });
    return res.status(500).json({ success: false, message: 'Error creando usuario' });
  }
});

// Login (JWT)
app.post('/api/auth/login', async (req, res) => {
  const { username, password } = req.body || {};
  if (!username || !password) return res.status(400).json({ success: false, message: 'Credenciales requeridas' });
  const u = await User.findOne({ username });
  if (!u) { audit('LOGIN_FAIL', req, null, { username }); return res.status(401).json({ success: false, message: 'Usuario o contraseña inválidos' }); }
  const ok = bcrypt.compareSync(password, u.password_hash);
  if (!ok) { audit('LOGIN_FAIL', req, u, { username }); return res.status(401).json({ success: false, message: 'Usuario o contraseña inválidos' }); }
  u.last_login_at = new Date(); await u.save();
  const token = jwt.sign({ _id: u._id, id: u._id, username: u.username, role: u.role }, JWT_SECRET, { expiresIn: '12h' });
  audit('LOGIN_SUCCESS', req, u, null);
  return res.json({ success: true, token, user: { id: u._id, username: u.username, role: u.role } });
});

// Logout (client-side clears token; this just audita)
app.post('/api/auth/logout', authMiddleware, async (req, res) => {
  try {
    const u = await User.findById(req.user.id || req.user._id);
    audit('LOGOUT', req, u || null, null);
  } catch {}
  return res.json({ success: true, message: 'Sesión cerrada' });
});

app.get('/api/users', authMiddleware, ensureAdmin, async (req, res) => {
  const users = await User.find({}, { password_hash: 0 }).sort({ _id: 1 });
  res.json({ success: true, users });
});

app.get('/api/version', async (req, res) => {
  audit('GET_VERSION', req, req.user, null);
  res.json({ success: true, version: currentVersion, timestamp: new Date().toISOString() });
});

app.post('/api/validate', async (req, res) => {
  const { version } = req.body || {};
  if (!version || !isValidVersion(version)) { audit('VALIDATE_VERSION_BAD_FORMAT', req, null, { version }); return res.status(400).json({ success: false, allowed: false, message: 'Formato de versión inválido. Use formato x.y.z' }); }
  const cmp = compareVersions(version, currentVersion);
  const result = { success: true, clientVersion: version, serverVersion: currentVersion, allowed: cmp === 0, message: cmp === 0 ? 'Versión válida. Acceso permitido.' : (cmp < 0 ? 'Outdated version detected.' : 'Versión del cliente más nueva que la del servidor.') };
  audit('VALIDATE_VERSION', req, null, { clientVersion: version, serverVersion: currentVersion, allowed: result.allowed });
  res.json(result);
});

app.put('/api/version', async (req, res) => {
  const { version, adminKey } = req.body || {};
  if (!version || !isValidVersion(version)) { audit('UPDATE_VERSION_BAD_FORMAT', req, req.user, { version }); return res.status(400).json({ success: false, message: 'Formato de versión inválido' }); }
  let actingUser = null;
  const legacyOk = adminKey && (adminKey === (process.env.ADMIN_KEY || config.adminKey));
  if (!legacyOk) {
    const auth = req.headers['authorization'] || '';
    if (!auth.startsWith('Bearer ')) return res.status(401).json({ success: false, message: 'No autorizado' });
    try {
      const payload = jwt.verify(auth.slice(7), JWT_SECRET);
      if (payload.role !== 'admin') return res.status(403).json({ success: false, message: 'Prohibido' });
      actingUser = await User.findById(payload.id || payload._id);
    } catch { return res.status(401).json({ success: false, message: 'Token inválido o expirado' }); }
  }
  const oldVersion = currentVersion;
  currentVersion = version;
  await Version.create({ version: currentVersion, changed_by_user_id: actingUser?._id || null, changed_by_username: actingUser?.username || (legacyOk ? 'legacy-adminKey' : null) });
  audit('UPDATE_VERSION', req, actingUser, { oldVersion, newVersion: currentVersion, legacy: legacyOk });
  const saved = saveConfig();
  res.json({ success: true, message: saved ? 'Versión actualizada y guardada correctamente' : 'Versión actualizada (no se pudo guardar config.json)', oldVersion, newVersion: currentVersion, savedToFile: saved });
});

app.get('/api/versions/history', authMiddleware, async (req, res) => {
  const rows = await Version.find().sort({ _id: -1 }).limit(200).lean();
  const mapped = rows.map(r => ({ id: r._id?.toString?.() || r.id || '', version: r.version, changed_by_username: r.changed_by_username || null, changed_at: r.changed_at }));
  res.json({ success: true, history: mapped });
});

app.get('/api/audits', authMiddleware, ensureAdmin, async (req, res) => {
  const { q, action, user, from, to, limit = 200 } = req.query;
  const filter = {};
  if (action) filter.action = action;
  if (user) filter.username = user;
  if (from || to) filter.created_at = {};
  if (from) filter.created_at.$gte = new Date(from);
  if (to) filter.created_at.$lte = new Date(to);
  if (q) filter.$or = [
    { details: { $regex: q, $options: 'i' } },
    { ip: { $regex: q, $options: 'i' } },
    { user_agent: { $regex: q, $options: 'i' } }
  ];
  const rows = await Audit.find(filter).sort({ _id: -1 }).limit(Math.min(Number(limit) || 200, 1000)).lean();
  const audits = rows.map(r => ({
    id: r._id?.toString?.() || r.id || '',
    action: r.action,
    username: r.username || null,
    details: typeof r.details === 'object' ? JSON.stringify(r.details) : (r.details || ''),
    created_at: r.created_at
  }));
  res.json({ success: true, audits });
});


// Change password
app.post('/api/auth/change-password', authMiddleware, async (req, res) => {
  const { currentPassword, newPassword } = req.body || {};
  if (!currentPassword || !newPassword) return res.status(400).json({ success: false, message: 'Contraseñas requeridas' });
  try {
    const u = await User.findById(req.user.id || req.user._id);
    if (!u) return res.status(404).json({ success: false, message: 'Usuario no encontrado' });
    const ok = bcrypt.compareSync(currentPassword, u.password_hash);
    if (!ok) { audit('PASSWORD_CHANGE_FAIL', req, u, null); return res.status(401).json({ success: false, message: 'Contraseña actual incorrecta' }); }
    u.password_hash = bcrypt.hashSync(newPassword, 10);
    await u.save();
    audit('PASSWORD_CHANGE', req, u, null);
    res.json({ success: true, message: 'Contraseña actualizada correctamente' });
  } catch (e) { res.status(500).json({ success: false, message: 'Error actualizando contraseña' }); }
});

async function fetchAndApplyRemoteVersion(auditSource, reqUser){
  const url = 'https://raw.githubusercontent.com/Tessio/Translations/refs/heads/master/version_l.txt';
  const result = { changed:false };
  try{
    const r = await fetch(url);
    if (!r.ok) return { ok:false, message:'No se pudo obtener la versión remota' };
    const txt = (await r.text()).trim();
    const match = txt.match(/\d+\.\d+\.\d+/);
    if (!match) return { ok:false, message:'Contenido remoto no contiene versión válida' };
    const newVersion = match[0];
    const oldVersion = currentVersion;
    if (newVersion === oldVersion) {
      audit(auditSource === 'cron' ? 'FETCH_REMOTE_VERSION_NOCHANGE_CRON' : 'FETCH_REMOTE_VERSION_NOCHANGE', { headers:{} }, reqUser || null, { version: newVersion });
      return { ok:true, message:'La versión ya está actualizada', version:newVersion, changed:false };
    }
    currentVersion = newVersion;
    await Version.create({ version: currentVersion, changed_by_user_id: reqUser?._id || null, changed_by_username: reqUser?.username || (auditSource==='cron' ? 'cron' : null) });
    audit(auditSource === 'cron' ? 'FETCH_REMOTE_VERSION_CRON' : 'FETCH_REMOTE_VERSION', { headers:{} }, reqUser || null, { oldVersion, newVersion });
    const saved = saveConfig();
    return { ok:true, message:'Versión sincronizada desde remoto', oldVersion, newVersion, savedToFile:saved, changed:true };
  }catch(e){ return { ok:false, message:'Error obteniendo versión remota' }; }
}

// Fetch remote version and set (manual)
app.post('/api/version/fetch-remote', authMiddleware, ensureAdmin, async (req, res) => {
  const actingUser = await User.findById(req.user.id || req.user._id);
  const r = await fetchAndApplyRemoteVersion('manual', actingUser);
  if (!r.ok) return res.status(502).json({ success:false, message:r.message });
  res.json({ success:true, ...r });
});

// Cron cada 15 minutos
setInterval(() => {
  fetchAndApplyRemoteVersion('cron', null).catch(()=>{});
}, 15 * 60 * 1000);

// Static routes for pages
app.get('/login', (req, res) => res.sendFile(path.join(__dirname, 'web-interface', 'login.html')));
app.get('/dashboard', (req, res) => res.sendFile(path.join(__dirname, 'web-interface', 'dashboard.html')));
app.get('/audits', (req, res) => res.sendFile(path.join(__dirname, 'web-interface', 'audits.html')));
app.get('/users', (req, res) => res.sendFile(path.join(__dirname, 'web-interface', 'users.html')));

app.listen(PORT, '0.0.0.0', () => {
  console.log(`🚀 Servidor de versiones (MongoDB) en puerto ${PORT}`);
  console.log(`🌐 Panel web: http://0.0.0.0:${PORT}`);
  console.log('🔗 Endpoints: GET /api/version, POST /api/validate, PUT /api/version, POST /api/auth/register, POST /api/auth/login, POST /api/auth/change-password, POST /api/version/fetch-remote');
});

module.exports = app;
