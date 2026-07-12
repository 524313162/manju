function showReplaceModalFromClick(assetId, name, isBgm) {
    document.getElementById('replaceAssetId').value = assetId;
    document.getElementById('replaceResourceTitle').textContent = isBgm ? '替换音频: ' + name : '替换图片: ' + name;
    var fileInput = document.getElementById('replaceFileInput');
    fileInput.accept = isBgm ? "audio/*" : "image/*";
    fileInput.value = '';
    document.getElementById('replacePreviewImg').style.display = 'none';
    document.getElementById('replacePreviewImg').src = '';
    showModal('replaceResourceModal');
    setTimeout(function() { fileInput.focus(); }, 100);
}

function uploadReplace() {
    var fileInput = document.getElementById('replaceFileInput');
    var file = fileInput && fileInput.files && fileInput.files[0];
    if (!file) { alert('请选择文件'); return; }

    var accept = fileInput.getAttribute('accept');
    var isBgm = accept === 'audio/*';
    var assetId = document.getElementById('replaceAssetId').value;
    var fd = new FormData();
    fd.append('assetid', assetId);
    fd.append('projectid', projectId);
    fd.append('uploadFile', file);
    var url = isBgm ? '/Assets/ReplaceAudio' : '/Assets/ReplaceResource';
    fetch(url, { method: 'POST', body: fd })
    .then(function(r) { return r.text().then(function(t) { try { return JSON.parse(t); } catch(e) { return {success:false,message:t}; } }); })
    .then(function(res) {
        if (res.success) {
            hideModal('replaceResourceModal');
            window.location.reload();
        } else {
            alert('替换失败: ' + (res.message || ''));
        }
    })
    .catch(function(err) { alert('请求失败: ' + err.message); });
}

function deleteAssetFromId(id, name) {
    if (!confirm('确认删除「' + name + '」？')) return;
    fetch('/Assets/Delete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: id })
    })
    .then(function(r) { return r.text().then(function(t) { try { return JSON.parse(t); } catch(e) { return {success:false,message:t}; } }); })
    .then(function(res) {
        if (res.success) {
            showToast('已删除', 'success');
            window.location.reload();
        } else {
            alert('删除失败: ' + (res.message || ''));
        }
    })
    .catch(function(err) { alert('删除请求失败: ' + err.message); });
}

function clearAllAssets() {
    if (!confirm('确认清除当前项目所有资产？此操作不可撤销！')) return;
    if (!confirm('再次确认：将删除当前项目的全部资产数据！')) return;
    fetch('/Assets/ClearAll', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ projectId: projectId })
    })
    .then(function(r) { return r.json(); })
    .then(function(res) {
        if (res.success) {
            showToast(res.message, 'success');
            window.location.reload();
        } else {
            alert('清除失败: ' + (res.message || ''));
        }
    })
    .catch(function(err) { alert('请求失败: ' + err.message); });
}

function showImagePreview(src, title) {
    if (!src) { return; }
    document.getElementById('previewImage').src = src;
    showModal('imagePreviewModal');
}

function playBgm(src, title) {
    if (!src) { return; }
    var audioId = src.split('/').pop();
    var audio = document.getElementById('audio-' + audioId.replace(/\.[^.]+$/, ''));
    if (!audio) { return; }
    var wasPlaying = audio.paused;
    document.querySelectorAll('audio').forEach(function(a) { a.pause(); });
    if (wasPlaying) {
        audio.play();
    } else {
        audio.play();
    }
    showImagePreview(src, title);
}

function showAssetImportModal() {
    document.getElementById('assetImportJson').value = '';
    showModal('assetImportModal');
}

function doAssetImport() {
    var jsonStr = document.getElementById('assetImportJson').value.trim();
    if (!jsonStr) { alert('请粘贴 JSON 资产数据'); return; }

    var parsed;
    try { parsed = JSON.parse(jsonStr); } catch(e) {
        alert('JSON 格式错误：' + e.message);
        return;
    }
    var assets = Array.isArray(parsed) ? parsed : (parsed.assets || []);
    if (assets.length === 0) { alert('未找到资产数据'); return; }

    hideModal('assetImportModal');
    showAssetImportMergeView(assets);
}

function showAssetImportMergeView(importAssets) {
    _assetImportNewAssets = importAssets;
    var typeNames = { 'Actor':'角色', 'Scene':'场景', 'Bgm':'BGM', 'Prop':'道具', 'VoiceVoice':'声音' };

    apiListByProject(projectId)
        .then(function(res){
            var existingNames = new Set((res.data || []).map(function(a){ return a.name.toLowerCase(); }));
            renderAssetImportMergeList(importAssets, existingNames);
        })
        .catch(function(){
            renderAssetImportMergeList(importAssets, new Set());
        });
}

var _assetImportNewAssets = null;

function renderAssetImportMergeList(assets, existingNames) {
    var typeNames = { 'Actor':'角色', 'Scene':'场景', 'Bgm':'BGM', 'Prop':'道具', 'VoiceVoice':'声音' };
    var typeIcons = { 'Actor':'\uD83D\uDC64', 'Scene':'\uD83C\uDFD0\uFE0F', 'Bgm':'\uD83C\uDFB5', 'Prop':'\uD83D\uDD25', 'VoiceVoice':'\uD83C\uDFA4' };
    var html = '';
    assets.forEach(function(a, i){
        var icon = typeIcons[a.assetType] || '\uD83D\uDCC4';
        var typeLabel = typeNames[a.assetType] || a.assetType || '';
        var exists = existingNames.has((a.name || '').toLowerCase());
        var indent = a.belong ? 'margin-left:20px;' : '';
        var existsTag = exists ? '<span style="color:var(--danger);font-size:10px;font-weight:600;">已存在</span>' : '<span style="color:var(--ok);font-size:10px;font-weight:600;">新资产</span>';
        html += '<div style="display:flex;align-items:flex-start;gap:8px;padding:8px 10px;border-bottom:1px solid var(--border);' + indent + '">'
            + '<input type="checkbox" class="asset-import-chk" value="' + i + '" ' + (exists ? '' : 'checked') + ' data-exists="' + exists + '" style="margin-top:3px;accent-color:var(--primary);">'
            + '<div style="flex:1;min-width:0;">'
            + '<div style="display:flex;align-items:center;gap:4px;font-size:13px;">'
            + '<span>' + icon + '</span>'
            + '<strong>' + escapeHtml(a.name || '') + '</strong>'
            + '<span style="color:var(--text3);font-size:10px;">[' + typeLabel + ']</span>'
            + (a.belong ? '<span style="color:var(--sec);font-size:10px;">→ ' + escapeHtml(a.belong) + '</span>' : '')
            + '</div>'
            + '<div style="font-size:12px;color:var(--text3);margin-top:2px;line-height:1.5;">' + escapeHtml((a.description || '') || '暂无描述') + '</div>'
            + '</div>'
            + '<div style="white-space:nowrap;font-size:11px;">' + existsTag + '</div>'
            + '</div>';
    });
    document.getElementById('assetImportMergeList').innerHTML = html;
    var total = assets.length;
    var sel = document.querySelectorAll('#assetImportMergeList .asset-import-chk:checked').length;
    document.getElementById('assetImportMergeSaveBtn').textContent = '保存勾选 (' + sel + '/' + total + ')';
    document.querySelectorAll('#assetImportMergeList .asset-import-chk').forEach(function(c){
        c.addEventListener('change', function(){
            var s = document.querySelectorAll('#assetImportMergeList .asset-import-chk:checked').length;
            document.getElementById('assetImportMergeSaveBtn').textContent = '保存勾选 (' + s + '/' + total + ')';
        });
    });
    showModal('assetImportMergeModal');
}

function copyAssetImportJson() {
    var json = JSON.stringify(_assetImportNewAssets, null, 2);
    navigator.clipboard.writeText(json).then(function() {
        showToast('已复制 JSON 到剪贴板', 'success');
    }).catch(function() {
        var ta = document.createElement('textarea');
        ta.value = json;
        document.body.appendChild(ta);
        ta.select();
        document.execCommand('copy');
        document.body.removeChild(ta);
        showToast('已复制 JSON 到剪贴板', 'success');
    });
}

function saveAssetImportMerge() {
    var chks = document.querySelectorAll('#assetImportMergeList .asset-import-chk:checked');
    if (chks.length === 0) { alert('请至少勾选一个资产'); return; }

    var selected = [];
    chks.forEach(function(c){
        var idx = parseInt(c.value);
        var item = _assetImportNewAssets[idx];
        var exists = c.getAttribute('data-exists') === 'true';
        selected.push({
            name: item.name,
            assetType: item.assetType,
            description: item.description || '',
            parentName: item.belong || '',
            override: exists
        });
    });

    var btn = document.getElementById('assetImportMergeSaveBtn');
    btn.disabled = true;
    btn.textContent = '保存中...';

    apiBulkCreate(projectId, selected)
    .then(function(res){
        btn.disabled = false;
        btn.textContent = '保存勾选';
        if (res.success) {
            showToast(res.message, 'success');
            hideModal('assetImportMergeModal');
            setTimeout(function() { window.location.reload(); }, 800);
        } else {
            alert('保存失败：' + (res.message || '未知错误'));
        }
    })
    .catch(function(err){
        btn.disabled = false;
        btn.textContent = '保存勾选';
        alert('网络错误：' + err.message);
    });
}

var _mergeAssetList = [];

function showExtractMergeModal(aiAssets) {
    _mergeAssetList = aiAssets;
    var typeIcons = { 'Actor':'\uD83D\uDCC8', 'Scene':'\uD83C\uDFD0', 'Bgm':'\uD83C\uDFB5', 'Prop':'\uD83D\uDD25', 'VoiceVoice':'\uD83C\uDFA4' };
    var names = { 'Actor':'角色', 'Scene':'场景', 'Bgm':'BGM', 'Prop':'道具', 'VoiceVoice':'声音' };

    var h = '<div class="modal-header"><h3>\uD83C\uDFAF AI 提取资产 - 确认合并</h3><button class="modal-close" onclick="hideModal(\'extractMergeModal\')">&times;</button></div>';
    h += '<div class="modal-body">';
    h += '<p style="color:var(--text2);font-size:13px;margin-bottom:16px;">AI 从章节中提取了以下资产，请勾选需要保存的项：</p>';
    h += '<div id="mergeAssetList" style="max-height:400px;overflow-y:auto;">';

    aiAssets.forEach(function(a, i) {
        var icon = typeIcons[a.assetType] || '\uD83D\uDCC4';
        var typeLabel = names[a.assetType] || a.assetType || '';
        h += '<label style="display:flex;align-items:flex-start;gap:10px;padding:10px 12px;border:1px solid var(--border);border-radius:6px;margin-bottom:6px;cursor:pointer;transition:.1s;" onmouseover="this.style.background=\'var(--bg)\'" onmouseout="this.style.background=\'\'">'
            + '<input type="checkbox" class="merge-chk" value="' + i + '" checked style="margin-top:3px;accent-color:var(--primary);">'
            + '<div style="flex:1;min-width:0;">'
            + '<div style="display:flex;align-items:center;gap:6px;">'
            + '<span>' + icon + '</span>'
            + '<strong style="font-size:14px;">' + escapeHtml(a.name || '') + '</strong>'
            + '<span class="asset-badge asset-badge-' + (a.assetType || '').toLowerCase() + '" style="font-size:10px;">' + typeLabel + '</span>'
            + '</div>'
            + '<div style="font-size:12px;color:var(--text3);margin-top:4px;line-height:1.5;">' + escapeHtml(a.description || '暂无描述') + '</div>'
            + '</div>'
            + '</label>';
    });

    h += '</div></div>';
    h += '<div class="modal-footer">'
        + '<button class="btn btn-ghost" onclick="hideModal(\'extractMergeModal\')">取消</button>'
        + '<button class="btn btn-primary" id="mergeSaveBtn" onclick="saveMergeAssets()">保存勾选 (' + aiAssets.length + ')</button>'
        + '</div>';

    var overlay = document.getElementById('extractMergeModal');
    if (!overlay) {
        overlay = document.createElement('div');
        overlay.className = 'modal-overlay';
        overlay.id = 'extractMergeModal';
        document.body.appendChild(overlay);
    }
    overlay.innerHTML = '<div class="modal" style="width:600px;max-height:90vh;">' + h + '</div>';
    showModal('extractMergeModal');

    document.querySelectorAll('#mergeAssetList .merge-chk').forEach(function(c) {
        c.addEventListener('change', function() {
            var total = _mergeAssetList.length;
            var sel = document.querySelectorAll('#mergeAssetList .merge-chk:checked').length;
            document.getElementById('mergeSaveBtn').textContent = '保存勾选 (' + sel + ')';
        });
    });
}

function saveMergeAssets() {
    var chks = document.querySelectorAll('#mergeAssetList .merge-chk:checked');
    if (chks.length === 0) { alert('请至少勾选一个资产'); return; }

    var selected = [];
    chks.forEach(function(c) {
        var idx = parseInt(c.value);
        selected.push(_mergeAssetList[idx]);
    });

    var btn = document.getElementById('mergeSaveBtn');
    btn.disabled = true;
    btn.textContent = '保存中...';

    apiBulkCreate(projectId, selected)
    .then(function(res) {
        if (res.success) {
            showToast(res.message, 'success');
            hideModal('extractMergeModal');
            setTimeout(function() { window.location.reload(); }, 800);
        } else {
            alert('保存失败：' + (res.message || '未知错误'));
            btn.disabled = false;
            btn.textContent = '保存勾选 (' + selected.length + ')';
        }
    })
    .catch(function(err) {
        alert('网络错误：' + err.message);
        btn.disabled = false;
        btn.textContent = '保存勾选 (' + selected.length + ')';
    });
}

function checkPendingExtractAssets() {
    var key = 'extractAssetResult_' + projectId;
    var raw = sessionStorage.getItem(key);
    if (!raw) return;

    sessionStorage.removeItem(key);
    var parsed;
    try { parsed = JSON.parse(raw); } catch(e) { return; }

    var assetsArray = Array.isArray(parsed) ? parsed : (parsed.assets && Array.isArray(parsed.assets) ? parsed.assets : null);
    if (!assetsArray || assetsArray.length === 0) return;

    showExtractMergeModal(assetsArray);
}