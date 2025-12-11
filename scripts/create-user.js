#!/usr/bin/env node
/*
Usage:
  node scripts/create-user.js --username=admin --password=Secret123 --role=admin
Env vars:
  MONGODB_URI=mongodb://localhost:27017/gtav_injector
*/
const mongoose = require('mongoose');
const bcrypt = require('bcryptjs');

const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://mongo:27017/gtav_injector';

function parseArgs() {
  const args = process.argv.slice(2);
  const out = {};
  for (const a of args) {
    const m = a.match(/^--([^=]+)=(.*)$/);
    if (m) out[m[1]] = m[2];
  }
  return out;
}

(async () => {
  const { username, password, role = 'admin' } = parseArgs();
  if (!username || !password) {
    console.error('Error: debes indicar --username y --password');
    process.exit(1);
  }
  try {
    await mongoose.connect(MONGODB_URI, { autoIndex: true });

    const userSchema = new mongoose.Schema({
      username: { type: String, unique: true, required: true },
      password_hash: { type: String, required: true },
      role: { type: String, default: 'admin' },
      created_at: { type: Date, default: Date.now },
      last_login_at: { type: Date }
    }, { versionKey: false, collection: 'users' });

    const auditSchema = new mongoose.Schema({
      action: { type: String, required: true },
      user_id: { type: mongoose.Schema.Types.ObjectId, ref: 'User' },
      username: { type: String },
      details: { type: Object },
      ip: { type: String },
      user_agent: { type: String },
      created_at: { type: Date, default: Date.now }
    }, { versionKey: false, collection: 'audits' });

    const User = mongoose.model('User', userSchema);
    const Audit = mongoose.model('Audit', auditSchema);

    const exists = await User.findOne({ username });
    if (exists) {
      console.error('Error: el usuario ya existe');
      process.exit(2);
    }

    const password_hash = bcrypt.hashSync(password, 10);
    const user = await User.create({ username, password_hash, role });

    await Audit.create({
      action: 'REGISTER_USER_SCRIPT',
      user_id: user._id,
      username: user.username,
      details: { role, source: 'scripts/create-user.js' },
      ip: null,
      user_agent: 'script'
    });

    console.log('Usuario creado correctamente');
    console.log({ id: user._id.toString(), username: user.username, role: user.role });
    process.exit(0);
  } catch (e) {
    console.error('Fallo creando usuario:', e?.message || e);
    process.exit(3);
  } finally {
    try { await mongoose.disconnect(); } catch {}
  }
})();
