const form = document.getElementById('downloadForm');
const urlInput = document.getElementById('urlInput');
const submitBtn = document.getElementById('submitBtn');
const btnText = submitBtn.querySelector('.btn-text');
const btnLoading = submitBtn.querySelector('.btn-loading');
const downloadsList = document.getElementById('downloadsList');
const refreshBtn = document.getElementById('refreshBtn');
const toast = document.getElementById('toast');

let pollingTimers = new Map();

/* Toast */
function showToast(message, type) {
  toast.textContent = message;
  toast.className = 'toast';
  if (type) toast.classList.add(type);
  setTimeout(() => toast.classList.add('hidden'), 3500);
}

/* Format helpers */
function formatDuration(seconds) {
  if (!seconds) return '?';
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s.toString().padStart(2, '0')}`;
}

function formatSize(bytes) {
  if (!bytes) return '?';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
}

function timeAgo(dateStr) {
  const now = new Date();
  const d = new Date(dateStr);
  const diff = Math.floor((now - d) / 1000);
  if (diff < 60) return 'hace unos segundos';
  if (diff < 3600) return `hace ${Math.floor(diff / 60)} min`;
  if (diff < 86400) return `hace ${Math.floor(diff / 3600)} h`;
  return d.toLocaleDateString();
}

function truncateUrl(url) {
  return url.length > 50 ? url.slice(0, 47) + '...' : url;
}

function getBadgeClass(status) {
  return `badge-${status}`;
}

function getStatusLabel(status) {
  const labels = { pending: 'Pendiente', processing: 'Procesando', completed: 'Completado', failed: 'Error' };
  return labels[status] || status;
}

/* Render a download card */
function createDownloadCard(d) {
  const card = document.createElement('div');
  card.className = 'download-card';
  card.id = `card-${d.id}`;

  const isProcessing = d.status === 'processing' || d.status === 'pending';
  const isCompleted = d.status === 'completed';
  const isFailed = d.status === 'failed';

  card.innerHTML = `
    <div class="download-card-header">
      <span class="download-title ${isProcessing ? 'loading' : ''}">
        ${d.title || 'Procesando...'}
      </span>
      <span class="download-badge ${getBadgeClass(d.status)}">
        ${getStatusLabel(d.status)}
      </span>
    </div>
    <div class="download-url" title="${d.url}">${truncateUrl(d.url)}</div>
    <div class="download-meta">
      ${d.duration ? `<span>⏱ ${formatDuration(d.duration)}</span>` : ''}
      ${d.file_size ? `<span>💾 ${formatSize(d.file_size)}</span>` : ''}
      <span class="meta-dot">•</span>
      <span>${timeAgo(d.created_at)}</span>
    </div>
    ${isCompleted ? `
      <div class="download-actions">
        <a href="/download/${d.id}/file" class="btn-download" target="_blank">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
            <polyline points="7 10 12 15 17 10"/>
            <line x1="12" y1="15" x2="12" y2="3"/>
          </svg>
          Descargar MP3
        </a>
      </div>
    ` : ''}
    ${isFailed && d.error_message ? `
      <div class="download-error">✕ ${d.error_message}</div>
    ` : ''}
  `;

  return card;
}

/* Fetch and render downloads */
async function fetchDownloads() {
  try {
    const res = await fetch('/downloads');
    if (!res.ok) throw new Error('Error al obtener descargas');
    const data = await res.json();

    if (!data.length) {
      downloadsList.innerHTML = `
        <div class="empty-state">
          <p>No hay descargas aún. ¡Pega una URL arriba para empezar!</p>
        </div>
      `;
      return [];
    }

    downloadsList.innerHTML = '';
    for (const d of data) {
      const card = createDownloadCard(d);
      downloadsList.appendChild(card);
    }

    return data;
  } catch (err) {
    console.error(err);
    return [];
  }
}

/* Start polling a download */
function startPolling(downloadId) {
  if (pollingTimers.has(downloadId)) return;

  const timer = setInterval(async () => {
    try {
      const res = await fetch(`/download/${downloadId}`);
      if (!res.ok) {
        stopPolling(downloadId);
        return;
      }
      const d = await res.json();

      const existingCard = document.getElementById(`card-${downloadId}`);
      if (existingCard) {
        const newCard = createDownloadCard(d);
        existingCard.replaceWith(newCard);
      }

      if (d.status === 'completed' || d.status === 'failed') {
        stopPolling(downloadId);
        showToast(
          d.status === 'completed'
            ? `✅ "${d.title}" descargado correctamente`
            : `❌ Error: ${d.error_message || 'Falló la descarga'}`,
          d.status === 'completed' ? 'success' : 'error'
        );
      }
    } catch {
      stopPolling(downloadId);
    }
  }, 2000);

  pollingTimers.set(downloadId, timer);
}

function stopPolling(downloadId) {
  const timer = pollingTimers.get(downloadId);
  if (timer) {
    clearInterval(timer);
    pollingTimers.delete(downloadId);
  }
}

/* Submit new download */
form.addEventListener('submit', async (e) => {
  e.preventDefault();
  const url = urlInput.value.trim();
  if (!url) return;

  submitBtn.disabled = true;
  btnText.classList.add('hidden');
  btnLoading.classList.remove('hidden');

  try {
    const res = await fetch(`/download?url=${encodeURIComponent(url)}`, { method: 'POST' });
    if (!res.ok) {
      const err = await res.json();
      showToast(`✕ Error: ${err.detail || 'Error al iniciar descarga'}`, 'error');
      return;
    }

    const data = await res.json();
    urlInput.value = '';
    showToast('⏳ Descarga iniciada', 'success');
    startPolling(data.id);

    /* Refresh list and scroll to top */
    await fetchDownloads();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  } catch (err) {
    showToast('✕ Error de conexión', 'error');
  } finally {
    submitBtn.disabled = false;
    btnText.classList.remove('hidden');
    btnLoading.classList.add('hidden');
  }
});

/* Refresh button */
refreshBtn.addEventListener('click', fetchDownloads);

/* On load */
document.addEventListener('DOMContentLoaded', async () => {
  const downloads = await fetchDownloads();
  /* Resume polling for any in-progress downloads */
  for (const d of downloads) {
    if (d.status === 'pending' || d.status === 'processing') {
      startPolling(d.id);
    }
  }
});