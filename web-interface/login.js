(function(){
  const apiBase = window.location.origin;
  // Si ya hay token, ir directo al dashboard
  try { const t = localStorage.getItem('token'); if (t) { location.href = '/dashboard'; return; } } catch {}
  const btn = document.getElementById('loginBtn');
  const userEl = document.getElementById('loginUser');
  const passEl = document.getElementById('loginPass');
  const toasts = document.getElementById('toastContainer');
  function showToast(type, msg){ const d=document.createElement('div'); d.className=`toast ${type}`; d.textContent=msg; toasts.appendChild(d); setTimeout(()=>d.remove(), 4000); }
  async function login(){
    const username = userEl.value.trim();
    const password = passEl.value.trim();
    if(!username || !password){ showToast('warning','Ingresa usuario y contraseña'); return; }
    btn.disabled = true; btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Ingresando...';
    try{
      const res = await fetch(`${apiBase}/api/auth/login`, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ username, password })});
      const data = await res.json();
      if(!data.success) throw new Error(data.message || 'Credenciales inválidas');
      localStorage.setItem('token', data.token);
      localStorage.setItem('user', JSON.stringify(data.user));
      showToast('success','Bienvenido');
      location.href = '/dashboard';
    }catch(e){ showToast('error', e.message); }
    finally{ btn.disabled=false; btn.innerHTML = '<i class="fas fa-sign-in-alt"></i> Ingresar'; }
  }
  passEl.addEventListener('keypress', e=>{ if(e.key==='Enter') login(); });
  btn.addEventListener('click', login);
})();
