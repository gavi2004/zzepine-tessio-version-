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

  // Update version (remote or manual)
  const resultEl = document.getElementById('remoteResult');
  const applyBtn = document.getElementById('applyVersionBtn');
  const manualInput = document.getElementById('manualVersion');
  const methodRadios = Array.from(document.querySelectorAll('input[name="verMethod"]'));
  const remoteHelp = document.getElementById('remoteHelp');
  const manualGroup = document.getElementById('manualGroup');

  function updateUiByMethod(){
    const method = (methodRadios.find(r=>r.checked)||{}).value;
    const isRemote = method === 'remote';
    remoteHelp.style.display = isRemote ? 'block' : 'none';
    manualGroup.style.display = isRemote ? 'none' : 'block';
  }
  methodRadios.forEach(r => r.addEventListener('change', updateUiByMethod));
  updateUiByMethod();

  applyBtn.addEventListener('click', async ()=>{
    const method = (methodRadios.find(r=>r.checked)||{}).value;
    resultEl.textContent='';
    applyBtn.disabled=true; applyBtn.innerHTML='<i class="fas fa-spinner fa-spin"></i> Aplicando...';
    try{
      if(method === 'remote'){
        const res = await fetch(`${apiBase}/api/version/fetch-remote`, { method:'POST', headers:{ 'Authorization': `Bearer ${token}` } });
        const data = await res.json();
        if(!data.success) throw new Error(data.message||'Error');
        toast('success', data.message);
        resultEl.textContent = data.changed ? `Actualizado: ${data.oldVersion} → ${data.newVersion}` : `Sin cambios. Versión actual: ${data.version || data.newVersion}`;
      } else {
        const v = manualInput.value.trim();
        if(!/^\d+\.\d+\.\d+$/.test(v)) throw new Error('Versión inválida. Usa formato x.y.z');
        const res = await fetch(`${apiBase}/api/version`, { method:'PUT', headers:{ 'Content-Type':'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify({ version: v }) });
        const data = await res.json();
        if(!data.success) throw new Error(data.message||'Error');
        toast('success', `Versión establecida a ${v}`);
        resultEl.textContent = `Versión actualizada a ${v}`;
      }
    }catch(e){ toast('error', e.message); }
    finally{ applyBtn.disabled=false; applyBtn.innerHTML='<i class="fas fa-sync-alt"></i> Aplicar'; }
  });

  // Show current version
  document.getElementById('refreshCurrentBtn').addEventListener('click', async ()=>{
    try{
      const res = await fetch(`${apiBase}/api/version`);
      const data = await res.json();
      resultEl.textContent = data?.version ? `Versión actual: ${data.version}` : 'No se pudo obtener la versión actual';
    }catch(e){ resultEl.textContent = 'No se pudo obtener la versión actual'; }
  });
})();
