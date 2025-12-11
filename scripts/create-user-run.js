#!/usr/bin/env node
// Permite: npm run user nombre clave [rol]
// Funciona en npm 6/7/8+ y también en Docker (sin depender de npm_config_argv)
const { spawnSync } = require('child_process');

function getPositionalArgs() {
  // 1) Intentar leer de process.argv (npm pasa los args a los scripts)
  const direct = process.argv.slice(2);
  if (direct && direct.length >= 2) return direct;

  // 2) Fallback a npm_config_argv (algunos entornos)
  try {
    const cooked = JSON.parse(process.env.npm_config_argv || '{}').cooked || [];
    const idx = cooked.indexOf('user');
    if (idx >= 0) return cooked.slice(idx + 1);
  } catch {}

  return [];
}

const positional = getPositionalArgs();
const [username, password, role = 'admin'] = positional;

if (!username || !password) {
  console.error('Uso: npm run user <usuario> <contrasena> [rol]');
  process.exit(1);
}

const args = [
  'scripts/create-user.js',
  `--username=${username}`,
  `--password=${password}`,
  `--role=${role}`
];

const result = spawnSync(process.execPath, args, { stdio: 'inherit', env: process.env });
process.exit(result.status || 0);
