function showModal(id) {
    document.getElementById(id).classList.add('show');
}

function hideModal(id) {
    document.getElementById(id).classList.remove('show');
}

function getQueryParams() {
    var params = {};
    var search = window.location.search.substring(1);
    if (search) {
        search.split('&').forEach(function(pair) {
            var parts = pair.split('=');
            params[decodeURIComponent(parts[0])] = decodeURIComponent(parts[1] || '');
        });
    }
    return params;
}

function showToast(msg, type) {
    type = type || 'info';
    var bg = type === 'success' ? '#d1fae5' : type === 'error' ? '#fee2e2' : '#e0e7ff';
    var txt = type === 'success' ? '#15803d' : type === 'error' ? '#b91c1c' : '#4338ca';
    var toast = document.createElement('div');
    toast.style.cssText = 'position:fixed;top:80px;right:24px;background:' + bg + ';color:' + txt +
        ';padding:14px 24px;border-radius:8px;font-size:14px;font-weight:600;z-index:9999;' +
        'box-shadow:0 8px 24px rgba(0,0,0,.12);animation:fadeIn .2s;';
    toast.textContent = msg;
    document.body.appendChild(toast);
    setTimeout(function(){ toast.remove(); }, 3000);
}

function escapeHtml(text) {
    if (!text) return '';
    return text.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/\n/g,'<br>');
}

function formatMarkdown(text) {
    if (!text) return '(暂无)';
    return text.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;')
        .replace(/\*\*(.*?)\*\*/g,'<strong>$1</strong>')
        .replace(/^# (.*$)/gm,'<h1 style="font-size:15px;font-weight:700;margin:10px 0 6px;">$1</h1>')
        .replace(/## (.*$)/gm,'<h2 style="font-size:13px;font-weight:700;margin:8px 0 4px;">$1</h2>')
        .replace(/\n/g,'<br>');
}

function tryJson(str, def) {
    if (!str) return def;
    try { return JSON.parse(str); } catch(e) { return def; }
}