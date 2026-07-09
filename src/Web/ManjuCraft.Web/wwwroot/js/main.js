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

/**
 * 创建轮询器，用于 ComfyUI 异步任务结果轮询
 * @param {object} opts
 * @param {string}   opts.promptId        - 任务ID
 * @param {string}   opts.workflowType    - 工作流类型
 * @param {string}   opts.resultType      - 结果类型: "text" / "image" / "video" / "audio"
 * @param {number}   [opts.intervalMs]    - 每次轮询间隔(ms)，默认 10000
 * @param {number}   [opts.timeoutMs]     - 超时时间(ms)，默认 600000
 * @param {function} opts.onSuccess       - 成功回调 (data)
 * @param {function} [opts.onTimeout]     - 超时回调
 * @param {function} [opts.onError]       - 请求失败回调
 * @returns {{ stop: function, fetchNow: function }}
 */
function createPoller(opts) {
    var timer = null;
    var stopped = false;
    var startTime = Date.now();
    var intervalMs = opts.intervalMs || 10000;
    var timeoutMs = opts.timeoutMs || 600000;
    var baseUrl = '/api/v1/comfyui/result/' + encodeURIComponent(opts.promptId) + '/' + encodeURIComponent(opts.resultType || 'text') + '?workflowType=' + encodeURIComponent(opts.workflowType);

    function poll() {
        if (stopped) return;
        if (Date.now() - startTime > timeoutMs) {
            if (opts.onTimeout) opts.onTimeout();
            return;
        }
        fetch(baseUrl)
            .then(function(r) { return r.json(); })
            .then(function(res) {
                if (stopped) return;
                if (res.success) {
                    if (opts.onSuccess) opts.onSuccess(res.data);
                } else if (res.pending) {
                    timer = setTimeout(poll, intervalMs);
                } else {
                    if (opts.onError) { opts.onError(res.message); } else { timer = setTimeout(poll, intervalMs); }
                }
            })
            .catch(function() {
                if (stopped) return;
                if (opts.onError) { opts.onError(); } else { timer = setTimeout(poll, intervalMs); }
            });
    }

    function fetchNow() {
        if (stopped) return;
        fetch(baseUrl)
            .then(function(r) { return r.json(); })
            .then(function(res) {
                if (stopped) return;
                if (res.success) {
                    stop();
                    if (opts.onSuccess) opts.onSuccess(res.data);
                } else if (res.pending) {
                    if (opts.onFetchEmpty) opts.onFetchEmpty();
                } else {
                    if (opts.onFetchError) opts.onFetchError(res.message);
                }
            })
            .catch(function() {
                if (opts.onFetchError) opts.onFetchError();
            });
    }

    function stop() {
        stopped = true;
        if (timer) { clearTimeout(timer); timer = null; }
    }

    timer = setTimeout(poll, intervalMs);

    return { stop: stop, fetchNow: fetchNow };
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
    return text.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
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