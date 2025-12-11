(function(){
  const apiBase = window.location.origin;
  const token = localStorage.getItem('token');
  const user = JSON.parse(localStorage.getItem('user')||'null');
  const toastC = document.getElementById('toastContainer');
  function toast(t,m){ const d=document.createElement('div'); d.className=`toast ${t}`; d.textContent=m; toastC.appendChild(d); setTimeout(()=>d.remove(), 4000);}  
  function requireAuth(){ if(!token||!user){ location.href='/login'; return false;} return true; }
  if(!requireAuth()) return;
  document.getElementById('currentUser').textContent = `Conectado: ${user.username}`;
  document.getElementById('logoutBtn').addEventListener('click', ()=>{ localStorage.removeItem('token'); localStorage.removeItem('user'); location.href='/login'; });

  async function loadAudits(){
    try {
      const q = document.getElementById('auditQ').value.trim();
      const from = document.getElementById('auditFrom').value;
      const to = document.getElementById('auditTo').value;
      const params = new URLSearchParams();
      if (q) params.set('q', q);
      if (from) params.set('from', from);
      if (to) params.set('to', to);
      const res = await fetch(`${apiBase}/api/audits?${params.toString()}`, { headers: { 'Authorization': `Bearer ${token}` } });
      const data = await res.json();
      if (!data.success) { toast('error', data.message || 'Error'); return; }
      const tbody = document.querySelector('#auditTable tbody');
      tbody.innerHTML = '';
      data.audits.forEach(row => {
        const tr = document.createElement('tr');
        const details = typeof row.details === 'object' ? JSON.stringify(row.details) : (row.details || '');
        tr.innerHTML = `<td>${row.id}</td><td>${row.action}</td><td>${row.username || '-'}</td><td><pre>${(details).substring(0, 500)}</pre></td><td>${new Date(row.created_at).toLocaleString('es-ES')}</td>`;
        tbody.appendChild(tr);
      });
    } catch(e){ toast('error','Error cargando auditorías'); }
  }

  document.getElementById('refreshAuditsBtn').addEventListener('click', loadAudits);
  document.getElementById('exportAuditsBtn').addEventListener('click', ()=>{
    const rows = Array.from(document.querySelectorAll('#auditTable tbody tr'));
    const csv = ['id,action,username,details,created_at'];
    rows.forEach(r => {
      const cols = Array.from(r.querySelectorAll('td')).map(td => '"' + (td.innerText || '').replace(/"/g, '""') + '"');
      csv.push(cols.join(','));
    });
    const blob = new Blob([csv.join('\n')], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = `audits_${new Date().toISOString().slice(0,19).replace(/[:T]/g,'-')}.csv`; a.click(); URL.revokeObjectURL(url);
  });

  loadAudits();
})();
