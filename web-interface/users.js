(function(){
  const apiBase = window.location.origin;
  const token = localStorage.getItem('token');
  const user = JSON.parse(localStorage.getItem('user')||'null');
  const toastC = document.getElementById('toastContainer');
  function toast(t,m){ const d=document.createElement('div'); d.className=`toast ${t}`; d.textContent=m; toastC.appendChild(d); setTimeout(()=>d.remove(), 4000);}  
  function requireAuth(){ if(!token||!user){ location.href='/login'; return false;} if(user.role!=='admin'){ toast('error','Requiere rol admin'); location.href='/dashboard'; return false;} return true; }
  if(!requireAuth()) return;
  document.getElementById('currentUser').textContent = `Conectado: ${user.username}`;
  document.getElementById('logoutBtn').addEventListener('click', ()=>{ localStorage.removeItem('token'); localStorage.removeItem('user'); location.href='/login'; });

  async function loadUsers(){
    try {
      const res = await fetch(`${apiBase}/api/users`, { headers: { 'Authorization': `Bearer ${token}` } });
      const data = await res.json();
      if (!data.success) { toast('error', data.message || 'Error'); return; }
      const tbody = document.querySelector('#usersTable tbody');
      tbody.innerHTML = '';
      data.users.forEach(u => {
        const tr = document.createElement('tr');
        tr.innerHTML = `<td>${u._id}</td><td>${u.username}</td><td>${u.role}</td><td>${u.created_at ? new Date(u.created_at).toLocaleString('es-ES') : '-'}</td><td>${u.last_login_at ? new Date(u.last_login_at).toLocaleString('es-ES') : '-'}</td>`;
        tbody.appendChild(tr);
      });
    } catch(e){ toast('error','Error cargando usuarios'); }
  }

  document.getElementById('refreshBtn').addEventListener('click', loadUsers);
  document.getElementById('createUserBtn').addEventListener('click', async ()=>{
    const username = document.getElementById('newUser').value.trim();
    const password = document.getElementById('newPass').value.trim();
    const role = document.getElementById('newRole').value;
    if(!username || !password) { toast('warning','Usuario y contraseña requeridos'); return; }
    try {
      const res = await fetch(`${apiBase}/api/auth/register`, { method:'POST', headers:{ 'Content-Type':'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify({ username, password, role }) });
      const data = await res.json();
      if (!data.success) throw new Error(data.message || 'Error');
      toast('success','Usuario creado');
      document.getElementById('newUser').value='';
      document.getElementById('newPass').value='';
      loadUsers();
    } catch(e){ toast('error', e.message); }
  });

  loadUsers();
})();
