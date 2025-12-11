(function(){
  const apiBase = window.location.origin;
  const token = localStorage.getItem('token');
  const user = JSON.parse(localStorage.getItem('user')||'null');
  const toastC = document.getElementById('toastContainer');
  function toast(t,m){ const d=document.createElement('div'); d.className=`toast ${t}`; d.textContent=m; toastC.appendChild(d); setTimeout(()=>d.remove(), 4000);}  
  function requireAuth(){ if(!token||!user){ location.href='/login'; return false;} return true; }
  if(!requireAuth()) return;
  document.getElementById('currentUser').textContent = `Conectado: ${user.username}`;
  document.getElementById('username').textContent = user.username;
  document.getElementById('role').textContent = user.role;
  document.getElementById('logoutBtn').addEventListener('click', ()=>{ localStorage.removeItem('token'); localStorage.removeItem('user'); location.href='/login'; });

  // Change password
  const btn = document.getElementById('changePassBtn');
  btn.addEventListener('click', async ()=>{
    const currentPassword = document.getElementById('currentPassword').value.trim();
    const newPassword = document.getElementById('newPassword').value.trim();
    const confirmPassword = document.getElementById('confirmPassword').value.trim();
    if(!currentPassword || !newPassword || !confirmPassword){ toast('warning','Completa todos los campos'); return; }
    if(newPassword !== confirmPassword){ toast('error','La confirmación no coincide'); return; }
    btn.disabled=true; btn.innerHTML='<i class="fas fa-spinner fa-spin"></i> Actualizando...';
    try{
      const res = await fetch(`${apiBase}/api/auth/change-password`, { method:'POST', headers:{ 'Content-Type':'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify({ currentPassword, newPassword }) });
      const data = await res.json();
      if(!data.success) throw new Error(data.message||'Error');
      toast('success','Contraseña actualizada');
      document.getElementById('currentPassword').value='';
      document.getElementById('newPassword').value='';
      document.getElementById('confirmPassword').value='';
    }catch(e){ toast('error', e.message); }
    finally{ btn.disabled=false; btn.innerHTML='<i class="fas fa-key"></i> Actualizar contraseña'; }
  });

  // Fetch remote version
  const fetchBtn = document.getElementById('fetchRemoteBtn');
  const resultEl = document.getElementById('remoteResult');
  fetchBtn.addEventListener('click', async ()=>{
    fetchBtn.disabled=true; fetchBtn.innerHTML='<i class="fas fa-spinner fa-spin"></i> Sincronizando...'; resultEl.textContent='';
    try{
      const res = await fetch(`${apiBase}/api/version/fetch-remote`, { method:'POST', headers:{ 'Authorization': `Bearer ${token}` } });
      const data = await res.json();
      if(!data.success) throw new Error(data.message||'Error');
      toast('success', data.message);
      resultEl.textContent = data.changed ? `Actualizado: ${data.oldVersion} → ${data.newVersion}` : `Sin cambios. Versión actual: ${data.version || data.newVersion}`;
    }catch(e){ toast('error', e.message); }
    finally{ fetchBtn.disabled=false; fetchBtn.innerHTML='<i class="fas fa-sync-alt"></i> Obtener y aplicar versión'; }
  });
})();
