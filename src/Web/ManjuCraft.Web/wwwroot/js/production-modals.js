// Production Modals - Asset/Shot extraction dialogs
(function() {
    // ---- 一键提取资产信息 ----
    var _extractPoller = null;
    var _lastExtractPromptId = null;
    var _lastExtractWorkflowType = null;

    function showExtractAssetModal() {
        renderExtractChapterList();
        var extractGoBtn = document.getElementById('extractGoBtn');
        if (extractGoBtn) {
            extractGoBtn.style.display = '';
            extractGoBtn.disabled = false;
            extractGoBtn.textContent = '提取';
        }
        var extractRetryFetchBtn = document.getElementById('extractRetryFetchBtn');
        if (extractRetryFetchBtn) extractRetryFetchBtn.style.display = 'none';
        _lastExtractPromptId = null;
        _lastExtractWorkflowType = null;
        loadExtractProviders();
        loadExtractTemplate();
        showModal('extractAssetModal');
    }

    function loadExtractProviders() {
        var sel = document.getElementById('extractProviderSelect');
        var saved = localStorage.getItem('extractAsset_providerId');
        fetch('/api/v1/providers/list')
            .then(function(r){ return r.json(); })
            .then(function(res){
                var list = (res.data || []).filter(function(p){ return p.capability === 1; });
                var html = '';
                var preSelected = saved || (list.length > 0 ? list[0].id : null);
                list.forEach(function(p) {
                    var s = preSelected && preSelected == p.id ? ' selected' : '';
                    html += '<option value="' + p.id + '"' + s + '>' + p.name + ' [' + (p.model || '') + ']</option>';
                });
                sel.innerHTML = html || '<option value="">未找到提供者</option>';
            })
            .catch(function(){
                sel.innerHTML = '<option value="">加载失败</option>';
            });
    }

    function loadExtractTemplate() {
        fetch('/api/v1/production/template?type=AssetExtraction')
            .then(function(r){ return r.json(); })
            .then(function(res){
                if (res.success && res.content) {
                    var extractTemplate = document.getElementById('extractTemplate');
                    if (extractTemplate) extractTemplate.value = res.content;
                    var extractTemplateType = document.getElementById('extractTemplateType');
                    if (extractTemplateType) extractTemplateType.textContent = 'AssetExtraction';
                }
            });
    }

    function renderExtractChapterList() {
        var container = document.getElementById('extractChapterList');
        if (!container) return;
        if (window.chapters.length === 0) {
            container.innerHTML = '<div style="padding:8px;color:var(--text3);font-size:13px;">暂无章节</div>';
            var extractChapterCount = document.getElementById('extractChapterCount');
            if (extractChapterCount) extractChapterCount.textContent = '';
            return;
        }
        var html = '';
        window.chapters.forEach(function(c, i) {
            var num = '第' + (c.chapterNumber || (i+1)) + '章';
            html += '<div class="extract-chapter-btn selected" data-idx="' + i + '" onclick="toggleExtractChapter(this)">'
                + '<span class="chk-icon">✓</span>'
                + '<span class="ch-num">' + num + '</span>'
                + '<span class="ch-name">' + (c.chapterName || '') + '</span>'
                + '</div>';
        });
        container.innerHTML = html;
        updateExtractChapterCount();
        updateExtractPreview();
    }

    function toggleExtractChapter(el) {
        el.classList.toggle('selected');
        updateExtractChapterCount();
        updateExtractPreview();
    }

    function updateExtractChapterCount() {
        var total = window.chapters.length;
        var sel = document.querySelectorAll('#extractChapterList .extract-chapter-btn.selected').length;
        var label = document.getElementById('extractChapterCount');
        if (!label) return;
        if (sel === 0) label.textContent = '（未选择）';
        else if (sel === total) label.textContent = '（全部勾选）';
        else label.textContent = '（' + sel + '/' + total + '）';
    }

    function updateExtractPreview() {
        var chks = document.querySelectorAll('#extractChapterList .extract-chapter-btn.selected');
        if (chks.length === 0) {
            var extractChapterPreview = document.getElementById('extractChapterPreview');
            if (extractChapterPreview) extractChapterPreview.textContent = '请勾选上方的章节';
            return;
        }
        var texts = [];
        chks.forEach(function(el) {
            var idx = parseInt(el.getAttribute('data-idx'));
            var c = window.chapters[idx];
            if (c) {
                var num = '第' + (c.chapterNumber || (idx+1)) + '章';
                texts.push(num + ': ' + c.chapterName + '\n' + (c.content || ''));
            }
        });
        var extractChapterPreview = document.getElementById('extractChapterPreview');
        if (extractChapterPreview) extractChapterPreview.textContent = texts.join('\n\n');
    }

    function copyExtractPreview() {
        var text = document.getElementById('extractChapterPreview').textContent;
        if (!text || text === '请勾选上方的章节') { alert('没有可复制的内容'); return; }
        navigator.clipboard.writeText(text).then(function() {
            showToast('已复制到剪贴板', 'success');
        }).catch(function() {
            var ta = document.createElement('textarea');
            ta.value = text;
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
            showToast('已复制到剪贴板', 'success');
        });
    }

    function doExtractAsset() {
        var chks = document.querySelectorAll('#extractChapterList .extract-chapter-btn.selected');
        if (chks.length === 0) { alert('请至少勾选一个章节'); return; }

        var providerId = document.getElementById('extractProviderSelect').value;
        if (!providerId) { alert('请选择 AI 提供者'); return; }
        localStorage.setItem('extractAsset_providerId', providerId);

        var chapterIds = [];
        chks.forEach(function(el) {
            var idx = parseInt(el.getAttribute('data-idx'));
            var c = window.chapters[idx];
            if (c) chapterIds.push(c.id);
        });
        var template = document.getElementById('extractTemplate').value.trim();

        var goBtn = document.getElementById('extractGoBtn');
        goBtn.disabled = true;
        goBtn.textContent = '正在提取中...';

        fetch('/api/v1/ai/extract-asset-info', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ providerId: parseInt(providerId), template: template, chapterIds: chapterIds })
        })
        .then(function(r) { return r.json(); })
        .then(function(res) {
            goBtn.disabled = false;
            goBtn.textContent = '重新提取';

            if (res.success) {
                if (res.isComfyui) {
                    _lastExtractPromptId = res.promptId;
                    _lastExtractWorkflowType = res.workflowType;
                    pollExtractAssetResult(res.promptId, res.workflowType);
                    return;
                }
                var text = res.data || '';
                if (showMergeView(text)) {
                    showToast('提取完成', 'success');
                } else {
                    showToast('提取完成（格式异常）', 'success');
                }
            } else {
                showToast('提取失败：' + (res.message || '未知错误'), 'error');
            }
        })
        .catch(function(err) {
            goBtn.disabled = false;
            goBtn.textContent = '重新提取';
            showToast('请求失败：' + err.message, 'error');
        });
    }

    function pollExtractAssetResult(promptId, workflowType) {
        var goBtn = document.getElementById('extractGoBtn');
        goBtn.disabled = true;
        goBtn.textContent = '正在提取中...';

        _extractPoller = createPoller({
            promptId: promptId,
            workflowType: workflowType,
            resultType: 'text',
            intervalMs: 10000,
            timeoutMs: 600000,
            onSuccess: function(data) {
                goBtn.disabled = false;
                goBtn.textContent = '重新提取';
                var text = data.text || '';
                try {
                    var raw = text;
                    var m = raw.match(/```(?:json)?\s*([\s\S]*?)```/);
                    if (m) raw = m[1].trim();
                    JSON.parse(raw);
                } catch(e) {}
                if (!showMergeView(text)) {
                    var extractSubmitBtn = document.getElementById('extractSubmitBtn');
                    if (extractSubmitBtn) extractSubmitBtn.style.display = '';
                }
                showToast('提取完成', 'success');
            },
            onTimeout: function() {
                goBtn.disabled = false;
                goBtn.textContent = '重新提取';
                var extractRetryFetchBtn = document.getElementById('extractRetryFetchBtn');
                if (extractRetryFetchBtn) extractRetryFetchBtn.style.display = '';
                showToast('等待超时（10分钟），任务仍在后台执行', 'error');
            },
            onFetchError: function(msg) {
                goBtn.disabled = false;
                goBtn.textContent = '重新提取';
                var extractRetryFetchBtn = document.getElementById('extractRetryFetchBtn');
                if (extractRetryFetchBtn) extractRetryFetchBtn.style.display = '';
                showToast('请求失败：' + (msg || '未知错误'), 'error');
            },
            onError: function(msg) {
                goBtn.disabled = false;
                goBtn.textContent = '重新提取';
                showToast('生成失败：' + (msg || '未知错误'), 'error');
            }
        });
    }

    function retryFetchExtractResult() {
        if (!_lastExtractPromptId) { showToast('没有可重新获取的任务', 'error'); return; }
        var extractRetryFetchBtn = document.getElementById('extractRetryFetchBtn');
        if (extractRetryFetchBtn) extractRetryFetchBtn.style.display = 'none';
        pollExtractAssetResult(_lastExtractPromptId, _lastExtractWorkflowType);
    }

    // ---- Shot & Asset Extraction Dialog ----
    var _allProviders = [];
    var _shotAssetExtractPoller = null;
    var _extractedNewAssets = null;
    var _extractionPreviewData = null;
    var _extractionAiResponse = null;

    function showMergeView(aiText) {
        var raw = aiText;
        var m = raw.match(/```(?:json)?\s*([\s\S]*?)```/);
        if (m) raw = m[1].trim();
        var parsed;
        try { parsed = JSON.parse(raw); } catch(e) { return false; }
        var assets = Array.isArray(parsed) ? parsed : (parsed.assets || []);
        if (assets.length === 0) return false;

        hideModal('extractAssetModal');
        _extractedNewAssets = assets;

        var typeNames = { 'Actor':'角色', 'Scene':'场景', 'Bgm':'BGM', 'Prop':'道具', 'VoiceVoice':'声音' };
        var typeIcons = { 'Actor':'\uD83D\uDC64', 'Scene':'\uD83C\uDFD0\uFE0F', 'Bgm':'\uD83C\uDFB5', 'Prop':'\uD83D\uDD25', 'VoiceVoice':'\uD83C\uDFA4' };

        fetch('/Assets/ListByProject?projectId=' + window.projectId)
            .then(function(r){ return r.json(); })
            .then(function(res){
                var existingNames = new Set((res.data || []).map(function(a){ return a.name.toLowerCase(); }));
                renderMergeList(assets, existingNames);
            })
            .catch(function(){
                renderMergeList(assets, new Set());
            });

        return true;
    }

    function renderMergeList(assets, existingNames) {
        var typeNames = { 'Actor':'角色', 'Scene':'场景', 'Bgm':'BGM', 'Prop':'道具', 'VoiceVoice':'声音' };
        var typeIcons = { 'Actor':'\uD83D\uDC64', 'Scene':'\uD83C\uDFD0\uFE0F', 'Bgm':'\uD83C\uDFB5', 'Prop':'\uD83D\uDD25', 'VoiceVoice':'\uD83C\uDFA4' };
        var html = '';
        assets.forEach(function(a, i){
            var icon = typeIcons[a.assetType] || '\uD83D\uDCC4';
            var typeLabel = typeNames[a.assetType] || a.assetType || '';
            var exists = existingNames.has((a.name || '').toLowerCase());
            var indent = a.parentName || a.belong ? 'margin-left:20px;' : '';
            var existsTag = exists ? '<span style="color:var(--danger);font-size:10px;font-weight:600;">已存在</span>' : '<span style="color:var(--ok);font-size:10px;font-weight:600;">新资产</span>';
            html += '<div style="display:flex;align-items:flex-start;gap:8px;padding:8px 10px;border-bottom:1px solid var(--border);' + indent + '">'
                + '<input type="checkbox" class="merge-item-chk" value="' + i + '" ' + (exists ? '' : 'checked') + ' data-exists="' + exists + '" style="margin-top:3px;accent-color:var(--primary);">'
                + '<div style="flex:1;min-width:0;">'
                + '<div style="display:flex;align-items:center;gap:4px;font-size:13px;">'
                + '<span>' + icon + '</span>'
                + '<strong>' + escapeHtml(a.name || '') + '</strong>'
                + '<span style="color:var(--text3);font-size:10px;">[' + typeLabel + ']</span>'
                + (a.parentName || a.belong ? '<span style="color:var(--sec);font-size:10px;">→ ' + escapeHtml(a.parentName || a.belong) + '</span>' : '')
                + '</div>'
                + '<div style="font-size:12px;color:var(--text3);margin-top:2px;line-height:1.5;">' + escapeHtml((a.description || '') || '暂无描述') + '</div>'
                + '</div>'
                + '<div style="white-space:nowrap;font-size:11px;">' + existsTag + '</div>'
                + '</div>';
        });
        var mergeAssetList = document.getElementById('mergeAssetList');
        if (mergeAssetList) mergeAssetList.innerHTML = html;
        updateMergeCount();
        document.querySelectorAll('#mergeAssetList .merge-item-chk').forEach(function(c){
            c.addEventListener('change', updateMergeCount);
        });
        showModal('mergeAssetModal');
    }

    function updateMergeCount() {
        var total = document.querySelectorAll('#mergeAssetList .merge-item-chk').length;
        var sel = document.querySelectorAll('#mergeAssetList .merge-item-chk:checked').length;
        var mergeSaveBtn = document.getElementById('mergeSaveBtn');
        if (mergeSaveBtn) mergeSaveBtn.textContent = '保存勾选 (' + sel + '/' + total + ')';
    }

    function copyMergeJson() {
        var json = JSON.stringify(_extractedNewAssets, null, 2);
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

    function saveMergeConfirm() {
        var chks = document.querySelectorAll('#mergeAssetList .merge-item-chk:checked');
        if (chks.length === 0) { alert('请至少勾选一个资产'); return; }

        var selected = [];
        chks.forEach(function(c){
            var idx = parseInt(c.value);
            var item = _extractedNewAssets[idx];
            var exists = c.getAttribute('data-exists') === 'true';
            selected.push({
                name: item.name,
                assetType: item.assetType,
                description: item.description || '',
                parentName: item.parentName || item.belong || '',
                override: exists
            });
        });

        var btn = document.getElementById('mergeSaveBtn');
        btn.disabled = true;
        btn.textContent = '保存中...';

        fetch('/Assets/BulkCreate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ projectId: window.projectId, assets: selected })
        })
        .then(function(r){ return r.json(); })
        .then(function(res){
            btn.disabled = false;
            btn.textContent = '保存勾选';
            if (res.success) {
                showToast(res.message, 'success');
                hideModal('mergeAssetModal');
            } else {
                alert('保存失败：' + (res.message || '未知错误'));
            }
        });
    }

    var _importNewAssets = null;

    function showImportAssetModal() {
        var importAssetJson = document.getElementById('importAssetJson');
        if (importAssetJson) importAssetJson.value = '';
        var importAssetGoBtn = document.getElementById('importAssetGoBtn');
        if (importAssetGoBtn) {
            importAssetGoBtn.disabled = false;
            importAssetGoBtn.textContent = '导入并合并';
        }
        showModal('importAssetModal');
    }

    function doImportAsset() {
        var jsonStr = document.getElementById('importAssetJson').value.trim();
        if (!jsonStr) { alert('请粘贴 JSON 资产数据'); return; }

        var parsed;
        try { parsed = JSON.parse(jsonStr); } catch(e) {
            alert('JSON 格式错误：' + e.message);
            return;
        }
        var assets = Array.isArray(parsed) ? parsed : (parsed.assets || []);
        if (assets.length === 0) { alert('未找到资产数据'); return; }

        hideModal('importAssetModal');
        showImportMergeView(assets);
    }

    function showImportMergeView(importAssets) {
        _importNewAssets = importAssets;
        var typeNames = { 'Actor':'角色', 'Scene':'场景', 'Bgm':'BGM', 'Prop':'道具', 'VoiceVoice':'声音' };

        fetch('/Assets/ListByProject?projectId=' + window.projectId)
            .then(function(r){ return r.json(); })
            .then(function(res){
                var existing = res.data || [];
                var html = existing.length === 0
                    ? '<div style="padding:12px;text-align:center;color:var(--text3);font-size:12px;">暂无资产</div>'
                    : existing.map(function(a){
                        var icon = a.assetType === 'Actor' ? '\uD83D\uDC64' : a.assetType === 'Scene' ? '\uD83C\uDFD0\uFE0F' : a.assetType === 'Bgm' ? '\uD83C\uDFB5' : '\uD83D\uDD25';
                        return '<div style="display:flex;align-items:center;gap:6px;padding:4px 6px;border-bottom:1px solid var(--border);font-size:12px;">'
                            + '<span>' + icon + '</span>'
                            + '<span style="font-weight:600;">' + escapeHtml(a.name) + '</span>'
                            + '<span style="color:var(--text3);font-size:10px;">[' + (typeNames[a.assetType] || a.assetType || '未知') + ']</span>'
                            + '</div>';
                    }).join('');
                var importExistingAssets = document.getElementById('importExistingAssets');
                if (importExistingAssets) importExistingAssets.innerHTML = html;
            });

        var newHtml = importAssets.map(function(a, i){
            var icon = a.assetType === 'Actor' ? '\uD83D\uDC64' : a.assetType === 'Scene' ? '\uD83C\uDFD0\uFE0F' : a.assetType === 'Bgm' ? '\uD83C\uDFB5' : '\uD83D\uDD25';
            return '<label style="display:flex;align-items:flex-start;gap:8px;padding:6px 8px;border-bottom:1px solid var(--border);cursor:pointer;" onmouseover="this.style.background=\'var(--bg)\'" onmouseout="this.style.background=\'\'">'
                + '<input type="checkbox" class="import-new-chk" value="' + i + '" checked style="margin-top:2px;accent-color:var(--primary);">'
                + '<div style="flex:1;min-width:0;">'
                + '<div style="display:flex;align-items:center;gap:4px;font-size:12px;">'
                + '<span>' + icon + '</span>'
                + '<strong>' + escapeHtml(a.name || '') + '</strong>'
                + '<span style="color:var(--text3);font-size:10px;">[' + (typeNames[a.assetType] || a.assetType || '') + ']</span>'
                + '</div>'
                + '<div style="font-size:11px;color:var(--text3);margin-top:2px;line-height:1.4;">' + escapeHtml((a.description || '') || '暂无描述') + '</div>'
                + '</div>'
                + '</label>';
        }).join('');
        var importNewAssets = document.getElementById('importNewAssets');
        if (importNewAssets) importNewAssets.innerHTML = newHtml;

        var importMergeSaveBtn = document.getElementById('importMergeSaveBtn');
        if (importMergeSaveBtn) importMergeSaveBtn.textContent = '保存合并 (' + importAssets.length + ')';
        showModal('importMergeModal');
    }

    function saveImportMerge() {
        var chks = document.querySelectorAll('#importNewAssets .import-new-chk:checked');
        if (chks.length === 0) { alert('请至少勾选一个资产'); return; }

        var selected = [];
        chks.forEach(function(c){
            var idx = parseInt(c.value);
            selected.push(_importNewAssets[idx]);
        });

        var btn = document.getElementById('importMergeSaveBtn');
        btn.disabled = true;
        btn.textContent = '保存中...';

        fetch('/Assets/BulkCreate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ projectId: window.projectId, assets: selected })
        })
        .then(function(r){ return r.json(); })
        .then(function(res){
            btn.disabled = false;
            if (res.success) {
                showToast(res.message, 'success');
                hideModal('importMergeModal');
            } else {
                alert('保存失败：' + (res.message || '未知错误'));
                btn.textContent = '保存合并 (' + selected.length + ')';
            }
        })
        .catch(function(err){
            btn.disabled = false;
            btn.textContent = '保存合并 (' + selected.length + ')';
            alert('网络错误：' + err.message);
        });
    }

    // ---- Import Shots Modal ----
    var _importShotsData = null;

    function showToastLocal(msg, type) {
        if (window.showToast) return window.showToast(msg, type);
        var color = type === 'success' ? 'var(--ok)' : type === 'error' ? 'var(--danger)' : 'var(--info)';
        var toast = document.createElement('div');
        toast.style.cssText = 'position:fixed;top:20px;right:20px;padding:12px 20px;background:' + color + ';color:#fff;border-radius:8px;z-index:9999;font-size:13px;font-weight:600;';
        toast.textContent = msg;
        document.body.appendChild(toast);
        setTimeout(function(){ toast.remove(); }, 3000);
    }

    function showImportShotsModal() {
        if (window.chapters.length === 0) {
            showToastLocal('请先创建剧本章节', 'error');
            return;
        }
        if (window.currentChapterIdx === -1) {
            showToastLocal('请先在左侧选择一个章节', 'error');
            return;
        }
        var importShotsJson = document.getElementById('importShotsJson');
        if (importShotsJson) importShotsJson.value = '';
        var importShotsPreview = document.getElementById('importShotsPreview');
        if (importShotsPreview) importShotsPreview.innerHTML = '<div style="text-align:center;padding:20px;color:var(--text3);">请粘贴 JSON 数据并点击预览</div>';
        var importShotsGoBtn = document.getElementById('importShotsGoBtn');
        if (importShotsGoBtn) {
            importShotsGoBtn.disabled = true;
            importShotsGoBtn.textContent = '导入并保存';
        }
        showModal('importShotsModal');
    }

    function previewImportShots() {
        var text = document.getElementById('importShotsJson').value.trim();
        var container = document.getElementById('importShotsPreview');
        var goBtn = document.getElementById('importShotsGoBtn');
        if (!text) {
            if (container) container.innerHTML = '<div style="text-align:center;padding:20px;color:var(--text3);">请粘贴 JSON 数据</div>';
            if (goBtn) goBtn.disabled = true;
            return;
        }
        try {
            var parsed = JSON.parse(text);
            var shots = Array.isArray(parsed) ? parsed : (parsed.shots || []);
            var assets = parsed.assets || [];
            if (!shots.length) throw new Error('无分镜数据');
            _importShotsData = parsed;

            var typeIcons = { 'Actor':'\uD83D\uDCC8', 'Scene':'\uD83C\uDFD0', 'Bgm':'\uD83C\uDFA5', 'Prop':'\uD83D\uDD25', 'VoiceVoice':'\uD83C\uDFA4' };
            var html = '<div style="font-size:13px;color:var(--ok);margin-bottom:8px;">\u2713 解析成功，共 ' + shots.length + ' 个分镜' + (assets.length > 0 ? '，' + assets.length + ' 个资产' : '') + '</div>';
            html += '<div style="max-height:300px;overflow-y:auto;display:flex;flex-direction:column;gap:6px;">';
            shots.forEach(function(shot, i) {
                var frames = shot.frames || [];
                var assetStr = (shot.assetRefs || []).join(', ');
                html += '<div style="background:var(--surface);border:1px solid var(--border);border-radius:6px;padding:10px;font-size:12px;">'
                    + '<div style="font-weight:700;margin-bottom:4px;">分镜 ' + (i+1) + ': ' + (shot.shotName || shot.shotNumber || '') + '</div>'
                    + '<div style="color:var(--text2);font-size:11px;">'
                    + '景别: ' + (shot.shotSize || '-') + ' | 运镜: ' + (shot.cameraMovement || '-') + ' | 时长: ' + (shot.duration || '-') + 's'
                    + '</div>'
                    + '<div style="color:var(--text2);font-size:11px;margin-top:2px;">帧数: ' + frames.length + '</div>'
                    + (assetStr ? '<div style="color:var(--text2);font-size:11px;margin-top:2px;">资产: ' + assetStr + '</div>' : '')
                    + '</div>';
            });
            if (assets.length > 0) {
                html += '<div style="margin-top:8px;font-weight:600;font-size:12px;">📦 待导入资产</div>';
                assets.forEach(function(a) {
                    var icon = typeIcons[a.assetType] || '📦';
                    html += '<div style="font-size:11px;color:var(--text2);padding:2px 0;">' + icon + ' ' + a.name + ' (' + a.assetType + ')</div>';
                });
            }
            html += '</div>';
            if (container) container.innerHTML = html;
            if (goBtn) goBtn.disabled = false;
        } catch(e) {
            if (container) container.innerHTML = '<div style="color:var(--danger);padding:12px;">解析失败：' + e.message + '</div>';
            _importShotsData = null;
            if (goBtn) goBtn.disabled = true;
        }
    }

    function saveImportShots() {
        if (!_importShotsData) {
            alert('请先粘贴并预览有效的分镜 JSON');
            return;
        }
        var parsed = _importShotsData;
        var shots = Array.isArray(parsed) ? parsed : (parsed.shots || []);
        if (!shots.length) { alert('无分镜数据'); return; }
        var btn = document.getElementById('importShotsGoBtn');
        if (btn) {
            btn.disabled = true;
            btn.textContent = '保存中...';
        }

        var mappedShots = shots.map(function(s) {
            return {
                shotName: s.shotName || '',
                shotNumber: s.shotNumber || '',
                shotSize: s.shotSize || '',
                cameraMovement: s.cameraMovement || '',
                duration: s.duration || 5,
                assetRefs: s.assetRefs || [],
                frames: (s.frames || []).map(function(f, fi) {
                    return {
                        frameType: f.frameType || f.FrameType || (fi === 0 ? 'First' : fi === (s.frames?.length - 1 || 0) ? 'Last' : 'Middle'),
                        description: f.description || f.NarrativeDescription || '',
                        narrativeDescription: f.NarrativeDescription || f.description || '',
                        generatePrompt: f.GeneratePrompt || '',
                        dialogue: f.dialogue || f.Dialogue || '',
                        cameraMovement: f.cameraMovement || f.CameraMovement || '',
                        shotSize: f.shotSize || f.ShotSize || '',
                        assetRefs: f.assetRefs || f.AssetRefs || [],
                        order: f.order ?? f.Order ?? fi,
                        startTime: f.startTime ?? f.StartTime,
                        duration: f.duration ?? f.Duration
                    };
                })
            };
        });

        var mappedAssets = parsed.assets || [];
        var body = {
            projectId: window.projectId,
            chapterIdx: window.currentChapterIdx,
            aiResponse: JSON.stringify({ shots: mappedShots, assets: mappedAssets })
        };

        fetch('/api/v1/ai/confirm-save-extraction', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        })
        .then(function(r){ return r.json(); })
        .then(function(res) {
            if (btn) {
                btn.disabled = false;
                btn.textContent = '导入并保存';
            }
            if (res.success) {
                showToast('分镜导入保存成功！', 'success');
                hideModal('importShotsModal');
                window.loadShotsForChapter(window.currentChapterIdx, true).then(function() { renderShotsTab(); });
            } else {
                alert('保存失败：' + (res.message || '未知错误'));
            }
        })
        .catch(function(err){
            if (btn) {
                btn.disabled = false;
                btn.textContent = '导入并保存';
            }
            alert('网络错误：' + err.message);
        });
    }

    function showShotExtractionModal() {
        showShotAssetExtractionModal();
    }

    function showShotAssetExtractionModal() {
        if (window.chapters.length === 0 || window.currentChapterIdx === -1) return;
        window.shotState[window.currentChapterIdx] = { shots: [], loading: true };

        if (window._shotAssetExtractPoller) { window._shotAssetExtractPoller.stop(); window._shotAssetExtractPoller = null; }

        var ch = window.chapters[window.currentChapterIdx];
        var shotAssetExtractChapterContent = document.getElementById('shotAssetExtractChapterContent');
        if (shotAssetExtractChapterContent) shotAssetExtractChapterContent.textContent = ch.content || '暂无内容';
        var shotAssetExtractPrompt = document.getElementById('shotAssetExtractPrompt');
        if (shotAssetExtractPrompt) shotAssetExtractPrompt.value = '加载提示词模板中...';
        var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
        if (shotAssetExtractResult) {
            shotAssetExtractResult.style.display = 'none';
            shotAssetExtractResult.innerHTML = '';
        }
        var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
        if (shotAssetExtractBtn) {
            shotAssetExtractBtn.textContent = '提取';
            shotAssetExtractBtn.disabled = true;
        }

        var sel = document.getElementById('shotAssetExtractProvider');
        if (sel) sel.innerHTML = '<option value="">加载中...</option>';
        var shotAssetExtractAssetInfo = document.getElementById('shotAssetExtractAssetInfo');
        if (shotAssetExtractAssetInfo) shotAssetExtractAssetInfo.innerHTML = '<div style="text-align:center;color:var(--text3);padding:20px;">加载资产中...</div>';

        loadShotAssetExtractProviders();
        loadShotAssetExtractPromptTemplate();
        loadShotAssetExtractAssetInfo();

        showModal('shotAssetExtractModal');
    }

    function loadShotAssetExtractProviders() {
        fetch('/api/v1/providers/list')
            .then(function(r){ return r.json(); })
            .then(function(res){
                window._allProviders = (res.data || []).filter(function(p){ return p.capability === 1; });
                var sel = document.getElementById('shotAssetExtractProvider');
                if (sel) {
                    var html = '<option value="">请选择提供者</option>';
                    window._allProviders.forEach(function(p){
                        html += '<option value="' + p.id + '">' + p.name + ' [' + (p.model || '') + ']</option>';
                    });
                    sel.innerHTML = html;
                }
                var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
                if (shotAssetExtractBtn) shotAssetExtractBtn.disabled = false;
            })
            .catch(function(){
                var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
                if (shotAssetExtractBtn) shotAssetExtractBtn.disabled = false;
            });
    }

    function loadShotAssetExtractPromptTemplate() {
        fetch('/Story/templates')
            .then(function(r){ return r.json(); })
            .then(function(res){
                if (res.success && res.data) {
                    var store = {};
                    res.data.forEach(function(t){ store[t.templateType] = t.content; });
                    var prompt = store['ShotAssetExtraction'] || '';
                    var shotAssetExtractPrompt = document.getElementById('shotAssetExtractPrompt');
                    if (shotAssetExtractPrompt) shotAssetExtractPrompt.value = prompt || '请分析章节内容，提取分镜脚本和资产信息。';
                } else {
                    var shotAssetExtractPrompt = document.getElementById('shotAssetExtractPrompt');
                    if (shotAssetExtractPrompt) shotAssetExtractPrompt.value = '请分析章节内容，提取分镜脚本和资产信息。';
                }
            })
            .catch(function(){
                var shotAssetExtractPrompt = document.getElementById('shotAssetExtractPrompt');
                if (shotAssetExtractPrompt) shotAssetExtractPrompt.value = '请分析章节内容，提取分镜脚本和资产信息。';
            });
    }

    function loadShotAssetExtractAssetInfo() {
        fetch('/Assets/ListByProject?projectId=' + window.projectId)
            .then(function(r){ return r.json(); })
            .then(function(res){
                var items = res.data || [];
                var container = document.getElementById('shotAssetExtractAssetInfo');
                if (!container) return;
                if (items.length === 0) {
                    container.innerHTML = '<div style="text-align:center;color:var(--text3);font-size:13px;">暂无资产</div>';
                } else {
                    var typeIcons = { 'Actor':'\uD83D\uDCC8', 'Scene':'\uD83C\uDFD0', 'Bgm':'\uD83C\uDFA5', 'Prop':'\uD83D\uDD25', 'VoiceVoice':'\uD83C\uDFA4' };
                    var html = '<div style="font-size:11px;color:var(--text3);margin-bottom:6px;">共 ' + items.length + ' 个资产</div>';
                    items.forEach(function(a) {
                        var emoji = typeIcons[a.assetType] || '\uD83D\uDCC4';
                        html += '<div style="display:flex;align-items:center;gap:4px;padding:3px 6px;font-size:12px;border-bottom:1px solid var(--border);">'
                            + '<span>' + emoji + '</span>'
                            + '<strong>' + a.name + '</strong>'
                            + '<span style="color:var(--text3);font-size:11px;">[' + (a.assetType || '') + ']</span>'
                            + '</div>';
                    });
                    container.innerHTML = html;
                }
            })
            .catch(function(){
                var shotAssetExtractAssetInfo = document.getElementById('shotAssetExtractAssetInfo');
                if (shotAssetExtractAssetInfo) shotAssetExtractAssetInfo.innerHTML = '<div style="color:var(--danger);padding:10px;font-size:13px;">加载失败</div>';
            });
    }

    function doShotAssetExtraction() {
        var providerId = document.getElementById('shotAssetExtractProvider').value;
        if (!providerId) { alert('请选择大模型'); return; }

        var customPrompt = document.getElementById('shotAssetExtractPrompt').value.trim();
        if (!customPrompt) { alert('提示词不能为空'); return; }

        var btn = document.getElementById('shotAssetExtractBtn');
        if (btn) {
            btn.textContent = '提取中...';
            btn.disabled = true;
        }

        var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
        if (shotAssetExtractResult) {
            shotAssetExtractResult.style.display = 'block';
            shotAssetExtractResult.innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">AI 正在提取分镜和资产，请稍候...</p></div>';
        }

        var requestBody = {
            ProjectId: window.projectId,
            ChapterIdx: window.currentChapterIdx,
            ProviderId: parseInt(providerId),
            CustomPrompt: customPrompt
        };

        fetch('/api/v1/ai/extract-shots-and-assets', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        })
        .then(function(r){ return r.json(); })
        .then(function(res) {
            // console.log('API Response:', res);
            if (res.success) {
                if (res.isComfyui) {
                    window._extractionAiResponse = null;
                    window._extractionPreviewData = null;
                    pollShotAssetExtractionResult(res.promptId, res.workflowType, res.chapterIdx, res.projectId);
                } else {
                    window._extractionAiResponse = res.rawResponse || '';
                    showExtractionPreview(res.data);
                }
            } else {
                var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
                if (shotAssetExtractResult) shotAssetExtractResult.innerHTML = '<div style="padding:12px;background:var(--danger-bg);border-radius:8px;margin-top:12px;color:var(--danger);font-weight:600;">&#10007; 提取失败：' + (res.message || '未知错误') + '</div>';
                var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
                if (shotAssetExtractBtn) {
                    shotAssetExtractBtn.textContent = '提取';
                    shotAssetExtractBtn.disabled = false;
                }
            }
        })
        .catch(function(err){
            var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
            if (shotAssetExtractResult) shotAssetExtractResult.innerHTML = '<div style="padding:12px;background:var(--danger-bg);border-radius:8px;margin-top:12px;color:var(--danger);font-weight:600;">请求失败：' + err.message + '</div>';
            var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
            if (shotAssetExtractBtn) {
                shotAssetExtractBtn.textContent = '提取';
                shotAssetExtractBtn.disabled = false;
            }
        });
    }

    function pollShotAssetExtractionResult(promptId, workflowType, chapterIdx, pId) {
        var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
        if (shotAssetExtractResult) shotAssetExtractResult.innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">ComfyUI 正在处理中，请耐心等待...</p><p style="margin-top:6px;color:var(--text3);font-size:11px;">任务ID: ' + promptId + '</p></div>';

        window._shotAssetExtractPoller = createPoller({
            promptId: promptId,
            workflowType: workflowType,
            resultType: 'text',
            intervalMs: 10000,
            timeoutMs: 600000,
            onSuccess: function(data) {
                var aiResponse = data.text || '';
                if (!aiResponse) {
                    var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
                    if (shotAssetExtractResult) shotAssetExtractResult.innerHTML = '<div style="padding:12px;background:var(--danger-bg);border-radius:8px;margin-top:12px;color:var(--danger);">AI 返回结果为空</div>';
                    var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
                    if (shotAssetExtractBtn) {
                        shotAssetExtractBtn.textContent = '提取';
                        shotAssetExtractBtn.disabled = false;
                    }
                    return;
                }

                window._extractionAiResponse = aiResponse;

                var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
                if (shotAssetExtractResult) shotAssetExtractResult.innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">正在解析提取结果...</p></div>';

                fetch('/api/v1/ai/parse-extraction-result', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        projectId: pId,
                        chapterIdx: chapterIdx,
                        aiResponse: aiResponse
                    })
                })
                .then(function(r){ return r.json(); })
                .then(function(parseRes) {
                    if (parseRes.success) {
                        showExtractionPreview(parseRes.data);
                    } else {
                        var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
                        if (shotAssetExtractResult) shotAssetExtractResult.innerHTML = '<div style="padding:12px;background:var(--danger-bg);border-radius:8px;margin-top:12px;color:var(--danger);">解析结果失败</div>';
                        var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
                        if (shotAssetExtractBtn) {
                            shotAssetExtractBtn.textContent = '提取';
                            shotAssetExtractBtn.disabled = false;
                        }
                    }
                })
                .catch(function(){
                    var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
                    if (shotAssetExtractResult) shotAssetExtractResult.innerHTML = '<div style="padding:12px;background:var(--danger-bg);border-radius:8px;margin-top:12px;color:var(--danger);">解析请求失败</div>';
                    var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
                    if (shotAssetExtractBtn) {
                        shotAssetExtractBtn.textContent = '提取';
                        shotAssetExtractBtn.disabled = false;
                    }
                });
            },
            onTimeout: function() {
                var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
                if (shotAssetExtractResult) shotAssetExtractResult.innerHTML = '<div style="padding:12px;background:var(--danger-bg);border-radius:8px;margin-top:12px;color:var(--danger);">等待超时（10分钟），任务仍在后台执行。任务ID: ' + promptId + '</div>';
                var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
                if (shotAssetExtractBtn) {
                    shotAssetExtractBtn.textContent = '提取';
                    shotAssetExtractBtn.disabled = false;
                }
            },
            onError: function(msg) {
                var shotAssetExtractResult = document.getElementById('shotAssetExtractResult');
                if (shotAssetExtractResult) shotAssetExtractResult.innerHTML = '<div style="padding:12px;background:var(--danger-bg);border-radius:8px;margin-top:12px;color:var(--danger);">' + (msg || '任务执行失败') + '</div>';
                var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
                if (shotAssetExtractBtn) {
                    shotAssetExtractBtn.textContent = '提取';
                    shotAssetExtractBtn.disabled = false;
                }
            }
        });
    }

    function showExtractionPreview(data) {
        window._extractionPreviewData = data;
        hideModal('shotAssetExtractModal');

        var typeIcons = { 'Actor':'\uD83D\uDCC8', 'Scene':'\uD83C\uDFD0', 'Bgm':'\uD83C\uDFA5', 'Prop':'\uD83D\uDD25', 'VoiceVoice':'\uD83C\uDFA4' };

        var html = '<div style="padding:16px;">';

        html += '<div style="background:var(--ok-bg);border-radius:8px;padding:14px;margin-bottom:16px;">'
            + '<p style="font-size:15px;font-weight:700;color:var(--ok);">&#10003; 提取完成</p></div>';

        html += '<div style="margin-bottom:16px;">'
            + '<p><strong>分镜数量：</strong>' + (data.shotCount || 0) + ' 个</p>'
            + '<p><strong>分镜帧数：</strong>' + (data.frameCount || 0) + ' 个</p>'
            + '<p><strong>新增资产：</strong>' + (data.newAssetCount || 0) + ' 个';
        if (data.newAssets && data.newAssets.length > 0) {
            html += '<div style="margin-top:8px;max-height:150px;overflow-y:auto;">';
            data.newAssets.forEach(function(a) {
                var emoji = typeIcons[a.assetType] || '\uD83D\uDCC4';
                html += '<div style="display:flex;align-items:center;gap:4px;padding:4px 8px;font-size:13px;border-bottom:1px solid var(--border);">'
                    + '<span>' + emoji + '</span>'
                    + '<strong>' + a.name + '</strong>'
                    + '<span style="color:var(--text3);font-size:11px;">[' + (a.assetType || '') + ']</span>'
                    + '</div>';
            });
            html += '</div>';
        }
        html += '</p>';

        if (data.shots && data.shots.length > 0) {
            html += '<div style="margin-bottom:16px;"><strong>分镜详情：</strong></div>';
            html += '<div style="max-height:300px;overflow-y:auto;display:flex;flex-direction:column;gap:8px;">';
            data.shots.forEach(function(shot, i) {
                var frameCount = (shot.frames || []).length;
                var assetStr = (shot.assetRefs || []).join(', ');
                html += '<div style="background:var(--surface);border:1px solid var(--border);border-radius:6px;padding:10px;font-size:13px;">'
                    + '<div style="font-weight:700;margin-bottom:4px;">分镜 ' + (i+1) + ': ' + (shot.shotName || '') + '</div>'
                    + '<div style="color:var(--text2);font-size:12px;">'
                    + '景别: ' + (shot.shotSize || '-') + ' | 运镜: ' + (shot.cameraMovement || '-') + ' | 时长: ' + (shot.duration || '-') + 's'
                    + '</div>'
                    + '<div style="color:var(--text2);font-size:12px;margin-top:2px;">帧数: ' + frameCount + '</div>'
                    + (assetStr ? '<div style="color:var(--text2);font-size:12px;margin-top:2px;">资产: ' + assetStr + '</div>' : '')
                    + '</div>';
            });
            html += '</div>';
        }

        html += '</div>';

        var rawJson = window._extractionAiResponse || JSON.stringify(window._extractionPreviewData, null, 2);
        var previewHtml = '<div style="width:860px;max-height:85vh;overflow:hidden;display:flex;flex-direction:column;background:#fff;border-radius:12px;box-shadow:0 20px 60px rgba(0,0,0,0.15);">'
            + '<div class="modal-header" style="flex-shrink:0;display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-bottom:1px solid var(--border);background:#fafafa;border-radius:12px 12px 0 0;">'
            + '<h3 style="margin:0;font-size:16px;font-weight:600;color:#111;">&#128214; 提取结果预览</h3>'
            + '<div style="display:flex;align-items:center;gap:6px;">'
            + '<button class="btn btn-ghost btn-sm" onclick="copyExtractionRawJson()" title="复制原始 JSON" style="padding:6px 12px;font-size:12px;border-radius:6px;">&#128203; 复制 JSON</button>'
            + '<button class="modal-close" onclick="hideModal(\'extractionPreviewModal\')" style="width:32px;height:32px;border:none;background:#f0f0f0;border-radius:8px;font-size:18px;color:#666;cursor:pointer;display:flex;align-items:center;justify-content:center;transition:all 0.2s;" onmouseover="this.style.background=\'#e0e0e0\';this.style.color=\'#333\'" onmouseout="this.style.background=\'#f0f0f0\';this.style.color=\'#666\'">&times;</button>'
            + '</div></div>'
            + '<div class="modal-body" style="flex:1;overflow-y:auto;padding:16px 20px;">' + html + '</div>'
            + '<div class="modal-footer" style="flex-shrink:0;padding:14px 20px;border-top:1px solid var(--border);display:flex;justify-content:flex-end;gap:10px;background:#fafafa;border-radius:0 0 12px 12px;">'
            + '<button class="btn btn-ghost" onclick="hideModal(\'extractionPreviewModal\')" style="padding:8px 18px;">取消</button>'
            + '<button class="btn btn-primary" id="confirmSaveBtn" onclick="confirmSaveExtraction()" style="padding:8px 22px;">确认保存</button>'
            + '</div></div>';

        var existingOverlay = document.getElementById('extractionPreviewModal');
        if (!existingOverlay) {
            var overlay = document.createElement('div');
            overlay.className = 'modal-overlay';
            overlay.id = 'extractionPreviewModal';
            overlay.innerHTML = previewHtml;
            document.body.appendChild(overlay);
        } else {
            existingOverlay.innerHTML = previewHtml;
        }

        var shotAssetExtractBtn = document.getElementById('shotAssetExtractBtn');
        if (shotAssetExtractBtn) {
            shotAssetExtractBtn.textContent = '提取';
            shotAssetExtractBtn.disabled = false;
        }
        showModal('extractionPreviewModal');
    }

    function copyExtractionRawJson() {
        var rawJson = window._extractionAiResponse || JSON.stringify(window._extractionPreviewData, null, 2);
        navigator.clipboard.writeText(rawJson).then(function() {
            showToast('已复制原始 JSON 到剪贴板', 'success');
        }).catch(function() {
            var ta = document.createElement('textarea');
            ta.value = rawJson;
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
            showToast('已复制原始 JSON 到剪贴板', 'success');
        });
    }

    function confirmSaveExtraction() {
        if (!window._extractionPreviewData) return;
        var btn = document.getElementById('confirmSaveBtn');
        if (btn) {
            btn.textContent = '保存中...';
            btn.disabled = true;
        }

        var body = {
            projectId: window.projectId,
            chapterIdx: window.currentChapterIdx,
            aiResponse: window._extractionAiResponse || JSON.stringify(window._extractionPreviewData)
        };

        fetch('/api/v1/ai/confirm-save-extraction', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        })
        .then(function(r){ return r.json(); })
        .then(function(res) {
            if (res.success) {
                if (btn) btn.textContent = '&#10003; 保存成功';
                showToast('分镜和资产保存成功！', 'success');
                setTimeout(function(){
                    hideModal('extractionPreviewModal');
                    loadShotsForChapter(window.currentChapterIdx).then(function() {
                        render();
                    });
                }, 1500);
            } else {
                alert('保存失败：' + (res.message || '未知错误'));
                if (btn) {
                    btn.textContent = '确认保存';
                    btn.disabled = false;
                }
            }
        })
        .catch(function(err){
            alert('请求失败：' + err.message);
            if (btn) {
                btn.textContent = '确认保存';
                btn.disabled = false;
            }
        });
    }

    // Expose globally
    window.showExtractAssetModal = showExtractAssetModal;
    window.loadExtractProviders = loadExtractProviders;
    window.loadExtractTemplate = loadExtractTemplate;
    window.renderExtractChapterList = renderExtractChapterList;
    window.toggleExtractChapter = toggleExtractChapter;
    window.updateExtractChapterCount = updateExtractChapterCount;
    window.updateExtractPreview = updateExtractPreview;
    window.copyExtractPreview = copyExtractPreview;
    window.doExtractAsset = doExtractAsset;
    window.pollExtractAssetResult = pollExtractAssetResult;
    window.retryFetchExtractResult = retryFetchExtractResult;
    window.showMergeView = showMergeView;
    window.renderMergeList = renderMergeList;
    window.updateMergeCount = updateMergeCount;
    window.copyMergeJson = copyMergeJson;
    window.saveMergeConfirm = saveMergeConfirm;
    window.showImportAssetModal = showImportAssetModal;
    window.doImportAsset = doImportAsset;
    window.showImportMergeView = showImportMergeView;
    window.saveImportMerge = saveImportMerge;
    window.showImportShotsModal = showImportShotsModal;
    window.previewImportShots = previewImportShots;
    window.saveImportShots = saveImportShots;
    window.showShotExtractionModal = showShotExtractionModal;
    window.showShotAssetExtractionModal = showShotAssetExtractionModal;
    window.loadShotAssetExtractProviders = loadShotAssetExtractProviders;
    window.loadShotAssetExtractPromptTemplate = loadShotAssetExtractPromptTemplate;
    window.loadShotAssetExtractAssetInfo = loadShotAssetExtractAssetInfo;
    window.doShotAssetExtraction = doShotAssetExtraction;
    window.pollShotAssetExtractionResult = pollShotAssetExtractionResult;
    window.showExtractionPreview = showExtractionPreview;
    window.copyExtractionRawJson = copyExtractionRawJson;
    window.confirmSaveExtraction = confirmSaveExtraction;

    // ---- Shot Asset Binding Modal ----
    // ============ 提取分镜资产（全量资产选择+AI提取+确认合并） ============
    var _sfaeShotIdx = -1;
    var _sfaeSelectedExisting = null;  // user-chosen existing asset names
    var _sfaeNewAssets = null;         // AI-extracted new assets
    var _sfaeShotAssets = null;        // shot's current bound assets (for pre-check)

    function showShotFrameAssetExtractModal(shotIdx) {
        _sfaeShotIdx = shotIdx;
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[shotIdx];
        if (!shot) return;

        var shotDesc = shot.description || '';
        var frameDescs = (shot.frames || []).map(function(f) { return f.description || ''; });

        // Build modal overlay once
        var overlay = document.getElementById('sfaeMainModal');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'modal-overlay';
            overlay.id = 'sfaeMainModal';
            overlay.innerHTML = '<div class="modal" style="width:860px;max-height:94vh;">'
                + '<div class="modal-header">'
                + '<h3>🔍 提取分镜资产</h3>'
                + '<button class="modal-close" onclick="hideModal(\'sfaeMainModal\')">&times;</button>'
                + '</div>'
                + '<div class="modal-body" style="padding:16px;">'
                + '<div class="form-group"><label>选择大模型</label><select id="sfaeProviderSelect" style="width:100%;padding:8px;border:1px solid var(--border);border-radius:6px;background:var(--surface2);color:var(--text);font-size:14px;"><option value="">加载中...</option></select></div>'
                + '<div style="display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:12px;">'
                + '<div><div class="form-group" style="margin:0;"><label>分镜描述</label><div id="sfaeShotDesc" style="padding:8px 10px;background:var(--bg);border-radius:6px;max-height:80px;overflow-y:auto;font-size:13px;line-height:1.5;color:var(--text2);"></div></div></div>'
                + '<div><div class="form-group" style="margin:0;"><label>帧描述（共 <span id="sfaeFrameCount">0</span> 帧）</label><div id="sfaeFrameDescs" style="padding:8px 10px;background:var(--bg);border-radius:6px;max-height:120px;overflow-y:auto;font-size:13px;line-height:1.5;"></div></div></div>'
                + '</div>'
                + '<div class="form-group" style="margin-bottom:4px;"><label>选择已有资产作为参考（勾选的将一同绑定到分镜帧） <span id="sfaeAssetCount" style="font-weight:400;color:var(--text3);font-size:12px;"></span></label>'
                + '<div style="display:flex;gap:8px;margin-bottom:6px;"><button class="btn btn-xs btn-ghost" onclick="sfaeToggleAll(true)" style="font-size:11px;">✅ 全选</button><button class="btn btn-xs btn-ghost" onclick="sfaeToggleAll(false)" style="font-size:11px;">⬜ 取消全选</button></div>'
                + '<div id="sfaeAssetList" style="display:grid;grid-template-columns:repeat(auto-fill,minmax(180px,1fr));gap:6px;max-height:220px;overflow-y:auto;border:1.5px solid var(--border);border-radius:var(--radius-sm);padding:8px;"></div></div>'
                + '</div>'
                + '<div class="modal-footer">'
                + '<button class="btn btn-ghost" onclick="hideModal(\'sfaeMainModal\')">取消</button>'
                + '<button class="btn btn-primary" id="sfaeGoBtn" onclick="doSfaeExtract()">提取</button>'
                + '</div></div>';
            document.body.appendChild(overlay);
        }

        document.getElementById('sfaeShotDesc').textContent = shotDesc || '（无描述）';
        var frameHtml = frameDescs.map(function(d, i) {
            return '<div style="padding:3px 0;font-size:12px;border-bottom:1px solid var(--border);">'
                + '<strong style="color:var(--text3);">帧' + i + '：</strong>' + (d || '（无描述）') + '</div>';
        }).join('');
        document.getElementById('sfaeFrameDescs').innerHTML = frameHtml || '<div style="color:var(--text3);font-size:12px;">（无帧描述）</div>';
        document.getElementById('sfaeFrameCount').textContent = frameDescs.length;

        var goBtn = document.getElementById('sfaeGoBtn');
        goBtn.style.display = '';
        goBtn.disabled = false;
        goBtn.textContent = '提取';

        // Load providers
        var sel = document.getElementById('sfaeProviderSelect');
        var saved = localStorage.getItem('sfaeExtract_providerId');
        fetch('/api/v1/providers/list')
            .then(function(r){ return r.json(); })
            .then(function(res){
                var list = (res.data || []).filter(function(p){ return p.capability === 1; });
                var html = '';
                var preSelected = saved || (list.length > 0 ? list[0].id : null);
                list.forEach(function(p) {
                    var s = preSelected && preSelected == p.id ? ' selected' : '';
                    html += '<option value="' + p.id + '"' + s + '>' + p.name + ' [' + (p.model || '') + ']</option>';
                });
                sel.innerHTML = html || '<option value="">未找到提供者</option>';
            })
            .catch(function(){
                sel.innerHTML = '<option value="">加载失败</option>';
            });

        // Load ALL project assets + current shot assets
        Promise.all([
            fetch('/Assets/ListByProject?projectId=' + window.projectId)
                .then(function(r){ return r.json(); })
                .catch(function(){ return { success: false, data: [] }; }),
            fetch('/api/v1/assets/shot-frame-assets/' + shot.id)
                .then(function(r){ return r.json(); })
                .catch(function(){ return { data: [] }; })
        ])
        .then(function(results) {
            var allAssets = results[0] && results[0].success !== false ? (results[0].data || []) : [];
            var shotAssetsData = results[1].data || [];
            _sfaeShotAssets = shotAssetsData;

            var shotNames = new Set(shotAssetsData.map(function(a){ return (a.name || '').toLowerCase(); }));
            var typeIcons = { 'Actor':'👤', 'Scene':'🏞️', 'Bgm':'🎵', 'Prop':'📦', 'VoiceVoice':'🎤' };
            var typeNames = { 'Actor':'角色', 'Scene':'场景', 'Bgm':'BGM', 'Prop':'道具', 'VoiceVoice':'音色' };

            var html = allAssets.map(function(a) {
                var name = a.name || '';
                var isBound = shotNames.has(name.toLowerCase());
                var icon = typeIcons[a.assetType] || '📄';
                var typeLabel = typeNames[a.assetType] || a.assetType || '';
                return '<label style="display:flex;align-items:center;gap:6px;padding:6px 8px;background:var(--surface);border:1.5px solid ' + (isBound ? 'var(--primary)' : 'var(--border)') + ';border-radius:6px;cursor:pointer;font-size:12px;">'
                    + '<input type="checkbox" class="sfae-asset-chk" value="' + escapeHtml(name) + '" data-type="' + (a.assetType || '') + '" ' + (isBound ? 'checked' : '') + ' style="accent-color:var(--primary);width:15px;height:15px;">'
                    + '<span style="font-size:15px;">' + icon + '</span>'
                    + '<span style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">' + escapeHtml(name) + '</span>'
                    + '<span style="color:var(--text3);font-size:10px;">' + typeLabel + '</span>'
                    + '</label>';
            }).join('');
            document.getElementById('sfaeAssetList').innerHTML = html || '<div style="grid-column:1/-1;text-align:center;color:var(--text3);padding:16px;">暂无资产</div>';
            var totalAssets = allAssets.length;
            var checkedCount = document.querySelectorAll('#sfaeAssetList .sfae-asset-chk:checked').length;
            document.getElementById('sfaeAssetCount').textContent = '（已选 ' + checkedCount + '/' + totalAssets + '）';
            document.querySelectorAll('#sfaeAssetList .sfae-asset-chk').forEach(function(c) {
                c.addEventListener('change', function() {
                    var ch = document.querySelectorAll('#sfaeAssetList .sfae-asset-chk:checked').length;
                    document.getElementById('sfaeAssetCount').textContent = '（已选 ' + ch + '/' + totalAssets + '）';
                });
            });
        })
        .catch(function() {
            document.getElementById('sfaeAssetList').innerHTML = '<div style="grid-column:1/-1;text-align:center;color:var(--danger);padding:16px;">加载资产失败</div>';
        });

        showModal('sfaeMainModal');
    }

    function doSfaeExtract() {
        var providerId = document.getElementById('sfaeProviderSelect').value;
        if (!providerId) { alert('请选择 AI 提供者'); return; }
        localStorage.setItem('sfaeExtract_providerId', providerId);

        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[_sfaeShotIdx];
        if (!shot) return;

        var chks = document.querySelectorAll('#sfaeAssetList .sfae-asset-chk:checked');
        var selectedNames = Array.from(chks).map(function(c) { return c.value; });
        _sfaeSelectedExisting = selectedNames;

        var goBtn = document.getElementById('sfaeGoBtn');
        goBtn.disabled = true;
        goBtn.textContent = '正在提取...';

        fetch('/api/v1/ai/extract-shot-frame-assets', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                providerId: parseInt(providerId),
                projectId: window.projectId,
                shotId: shot.id,
                shotDescription: shot.description || '',
                frameDescriptions: (shot.frames || []).map(function(f) { return f.description || ''; }),
                selectedAssetNames: selectedNames
            })
        })
        .then(function(r) { return r.json(); })
        .then(function(res) {
            goBtn.disabled = false;
            goBtn.textContent = '提取';

            if (!res.success) {
                alert('提取失败：' + (res.message || '未知错误'));
                return;
            }

            var text = res.data || '';
            var raw = text;
            var m = raw.match(/```(?:json)?\s*([\s\S]*?)```/);
            if (m) raw = m[1].trim();
            var parsed;
            try { parsed = JSON.parse(raw); } catch(e) {}
            var newAssets = Array.isArray(parsed) ? parsed : (parsed && parsed.assets ? parsed.assets : []);

            _sfaeNewAssets = newAssets || [];

            hideModal('sfaeMainModal');
            showSfaeConfirmMerge(selectedNames, newAssets);
        })
        .catch(function(err) {
            goBtn.disabled = false;
            goBtn.textContent = '提取';
            alert('请求失败：' + err.message);
        });
    }

    function showSfaeConfirmMerge(selectedNames, newAssets) {
        var hasExisting = selectedNames.length > 0;
        var hasNew = newAssets && newAssets.length > 0;

        if (!hasExisting && !hasNew) {
            alert('没有可保存的资产（未选择已有资产，AI 也未提取到新资产）');
            return;
        }

        var overlay = document.getElementById('sfaeConfirmModal');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'modal-overlay';
            overlay.id = 'sfaeConfirmModal';
            overlay.innerHTML = '<div class="modal" style="width:860px;max-height:90vh;">'
                + '<div class="modal-header">'
                + '<h3>✅ 确认保存资产并绑定到分镜帧</h3>'
                + '<button class="modal-close" onclick="hideModal(\'sfaeConfirmModal\')">&times;</button>'
                + '</div>'
                + '<div class="modal-body">'
                + '<div id="sfaeConfirmExistingSection" style="margin-bottom:12px;display:none;">'
                + '<div style="font-size:12px;font-weight:600;color:var(--text2);margin-bottom:6px;">📦 已选的已有资产（将绑定到该分镜的每一帧）</div>'
                + '<div id="sfaeConfirmExistingList" style="max-height:150px;overflow-y:auto;border:1.5px solid var(--border);border-radius:var(--radius-sm);padding:6px 8px;font-size:12px;"></div>'
                + '</div>'
                + '<div id="sfaeConfirmNewSection" style="display:none;">'
                + '<div style="font-size:12px;font-weight:600;color:var(--text2);margin-bottom:6px;">🔍 AI 提取的新资产（将绑定到指定帧）</div>'
                + '<div id="sfaeConfirmNewList" style="max-height:250px;overflow-y:auto;border:1.5px solid var(--border);border-radius:var(--radius-sm);padding:6px 8px;font-size:12px;"></div>'
                + '</div>'
                + '</div>'
                + '<div class="modal-footer">'
                + '<button class="btn btn-ghost" onclick="hideModal(\'sfaeConfirmModal\')">取消</button>'
                + '<button class="btn btn-primary" id="sfaeConfirmSaveBtn" onclick="doSfaeSave()">确认保存</button>'
                + '</div></div>';
            document.body.appendChild(overlay);
        }

        // Show existing selected assets
        var existingSection = document.getElementById('sfaeConfirmExistingSection');
        var existingList = document.getElementById('sfaeConfirmExistingList');
        if (hasExisting) {
            existingSection.style.display = '';
            existingList.innerHTML = selectedNames.map(function(n) {
                return '<div style="padding:3px 0;font-size:12px;">✅ ' + escapeHtml(n) + '</div>';
            }).join('');
        } else {
            existingSection.style.display = 'none';
        }

        // Show new extracted assets
        var newSection = document.getElementById('sfaeConfirmNewSection');
        var newList = document.getElementById('sfaeConfirmNewList');
        var typeIcons = { 'Actor':'👤', 'Scene':'🏞️', 'Prop':'📦' };
        var typeNames = { 'Actor':'角色', 'Scene':'场景', 'Prop':'道具' };
        if (hasNew) {
            newSection.style.display = '';
            newList.innerHTML = newAssets.map(function(a) {
                var icon = typeIcons[a.assetType] || '📄';
                var typeLabel = typeNames[a.assetType] || a.assetType || '';
                var frameLabel = a.belongFrame !== undefined && a.belongFrame >= 0 ? '帧' + a.belongFrame : '分镜';
                return '<div style="padding:3px 0;font-size:12px;border-bottom:1px solid var(--border);">'
                    + icon + ' <strong>' + escapeHtml(a.name || '') + '</strong>'
                    + ' <span style="color:var(--text3);font-size:10px;">[' + typeLabel + ']</span>'
                    + ' <span style="color:var(--sec);font-size:11px;">→ ' + frameLabel + '</span>'
                    + '</div>';
            }).join('');
        } else {
            newSection.style.display = 'none';
        }

        showModal('sfaeConfirmModal');
    }

    function doSfaeSave() {
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[_sfaeShotIdx];
        if (!shot || !shot.id) { alert('分镜数据异常'); return; }

        var btn = document.getElementById('sfaeConfirmSaveBtn');
        btn.disabled = true;
        btn.textContent = '保存中...';

        fetch('/api/v1/assets/save-shot-frame-assets', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                projectId: window.projectId,
                shotId: shot.id,
                selectedAssetNames: _sfaeSelectedExisting || [],
                newAssets: _sfaeNewAssets || []
            })
        })
        .then(function(r) { return r.json(); })
        .then(function(res) {
            btn.disabled = false;
            btn.textContent = '确认保存';
            if (res.success) {
                showToast(res.message, 'success');
                hideModal('sfaeConfirmModal');
                // Reload shot data from server to get frame-level assets
                var idx = window.currentChapterIdx;
                window.loadShotsForChapter(idx, true).then(function() {
                    renderShotsTab();
                });
            } else {
                alert('保存失败：' + (res.message || '未知错误'));
            }
        })
        .catch(function(err) {
            btn.disabled = false;
            btn.textContent = '确认保存';
            alert('请求失败：' + err.message);
        });
    }

    function sfaeToggleAll(checked) {
        document.querySelectorAll('#sfaeAssetList .sfae-asset-chk').forEach(function(c) {
            c.checked = checked;
        });
        var total = document.querySelectorAll('#sfaeAssetList .sfae-asset-chk').length;
        var ch = document.querySelectorAll('#sfaeAssetList .sfae-asset-chk:checked').length;
        document.getElementById('sfaeAssetCount').textContent = '（已选 ' + ch + '/' + total + '）';
    }

    // ============ 绑定分镜帧资产（手风琴折叠模式） ============
    var _bindFrameShotIdx = -1;
    var _bindActiveFrame = 0;

    function showShotFrameAssetBindModal(shotIdx) {
        _bindFrameShotIdx = shotIdx;
        _bindActiveFrame = 0;
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[shotIdx];
        if (!shot) return;

        var frames = shot.frames || [];

        var overlay = document.getElementById('sfaeBindModal');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'modal-overlay';
            overlay.id = 'sfaeBindModal';
            overlay.innerHTML = '<div class="modal" style="width:860px;max-height:94vh;">'
                + '<div class="modal-header">'
                + '<h3>🏷️ 绑定分镜帧资产</h3>'
                + '<button class="modal-close" onclick="hideModal(\'sfaeBindModal\')">&times;</button>'
                + '</div>'
                + '<div class="modal-body" style="padding:16px;">'
                + '<div style="font-size:12px;color:var(--text2);margin-bottom:6px;">点击展开各帧，按帧勾选需要绑定的资产，未勾选的将从该帧移除。</div>'
                + '<div style="font-size:12px;color:var(--text);padding:8px 10px;background:var(--bg);border-radius:6px;margin-bottom:10px;max-height:80px;overflow-y:auto;line-height:1.5;"><strong>分镜描述：</strong>' + escapeHtml(shot.description || '（无）') + '</div>'
                + '<div id="sfaeBindBody" style="max-height:65vh;overflow-y:auto;display:flex;flex-direction:column;gap:4px;"></div>'
                + '</div>'
                + '<div class="modal-footer">'
                + '<button class="btn btn-ghost" onclick="hideModal(\'sfaeBindModal\')">取消</button>'
                + '<button class="btn btn-primary" id="sfaeBindSaveBtn" onclick="doSfaeBindSave()">保存</button>'
                + '</div></div>';
            document.body.appendChild(overlay);
        }

        var bodyEl = document.getElementById('sfaeBindBody');
        bodyEl.innerHTML = '<div style="text-align:center;padding:20px;color:var(--text3);">加载中...</div>';

        fetch('/Assets/ListByProject?projectId=' + window.projectId)
            .then(function(r){ return r.json(); })
            .then(function(res) {
                var allAssets = (res.data || []).filter(function(a){ return a.name; });
                if (allAssets.length === 0) {
                    bodyEl.innerHTML = '<div style="text-align:center;padding:30px;color:var(--text3);">暂无项目资产，请先创建资产</div>';
                    return;
                }

                var typeIcons = { 'Actor':'👤', 'Scene':'🏞️', 'Bgm':'🎵', 'Prop':'📦', 'VoiceVoice':'🎤' };
                var typeNames = { 'Actor':'角色', 'Scene':'场景', 'Bgm':'BGM', 'Prop':'道具', 'VoiceVoice':'音色' };
                function getLabel(t) { return typeNames[t] || (t === 1 ? '角色' : t === 3 ? '场景' : t === 4 ? 'BGM' : t === 5 ? '道具' : t === 2 ? '音色' : t) || '未知'; }
                function getIcon(t) { return typeIcons[t] || (t === 1 ? '👤' : t === 3 ? '🏞️' : t === 4 ? '🎵' : t === 5 ? '📦' : t === 2 ? '🎤' : '📄'); }

                var fullHtml = frames.map(function(f, fi) {
                    var ft = f.frameType || '';
                    var label = ft === 'First' ? '首帧' : ft === 'Last' ? '末帧' : '中帧' + (fi);
                    var frameAssets = f.assets || [];
                    var boundNames = new Set(frameAssets.map(function(af){ return (af.name || '').toLowerCase(); }));
                    var desc = f.description || '';

                    var chkHtml = allAssets.map(function(a) {
                        var name = a.name || '';
                        var checked = boundNames.has(name.toLowerCase()) ? 'checked' : '';
                        var icon = getIcon(a.assetType);
                        var typeLabel = getLabel(a.assetType);
                        return '<label style="display:flex;align-items:center;gap:5px;padding:4px 6px;border-radius:4px;cursor:pointer;font-size:12px;background:var(--surface);border:1px solid ' + (checked ? 'var(--primary)' : 'var(--border)') + ';">'
                            + '<input type="checkbox" class="sfae-frm-chk" data-frame="' + fi + '" data-asset="' + escapeHtml(name) + '" ' + checked + ' style="accent-color:var(--primary);width:14px;height:14px;">'
                            + '<span style="font-size:13px;">' + icon + '</span>'
                            + '<span style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">' + escapeHtml(name) + '</span>'
                            + '<span style="color:var(--text3);font-size:9px;">' + typeLabel + '</span>'
                            + '</label>';
                    }).join('');

                    var isActive = fi === _bindActiveFrame;
                    return '<div style="border:1.5px solid var(--border);border-radius:8px;overflow:hidden;">'
                        + '<div class="sfae-accordion-header" data-idx="' + fi + '" onclick="sfaeToggleFrame(' + fi + ')" style="display:flex;align-items:center;justify-content:space-between;padding:8px 12px;background:var(--bg);cursor:pointer;user-select:none;">'
                        + '<div style="display:flex;align-items:center;gap:8px;flex:1;min-width:0;">'
                        + '<span id="sfae-arrow-' + fi + '" style="font-size:10px;transition:transform .2s;' + (isActive ? 'transform:rotate(90deg);' : '') + '">▶</span>'
                        + '<strong style="font-size:13px;white-space:nowrap;">🎬 ' + label + '</strong>'
                        + '</div>'
                        + '<div style="display:flex;gap:6px;flex-shrink:0;" onclick="event.stopPropagation();">'
                        + '<button class="btn btn-xs btn-ghost" onclick="sfaeFrameSelectAll(' + fi + ', true)" style="font-size:10px;">全选</button>'
                        + '<button class="btn btn-xs btn-ghost" onclick="sfaeFrameSelectAll(' + fi + ', false)" style="font-size:10px;">清空</button>'
                        + '</div></div>'
                        + '<div id="sfae-frame-desc-' + fi + '" style="display:' + (isActive ? 'block' : 'none') + ';padding:4px 12px 8px;font-size:12px;color:var(--text2);line-height:1.5;max-height:60px;overflow-y:auto;background:var(--surface);border-bottom:1px solid var(--border);">' + escapeHtml(desc) + '</div>'
                        + '<div id="sfae-frame-assets-' + fi + '" style="display:' + (isActive ? 'grid' : 'none') + ';grid-template-columns:repeat(auto-fill,minmax(160px,1fr));gap:4px;padding:8px;">'
                        + chkHtml
                        + '</div></div>';
                }).join('');

                bodyEl.innerHTML = fullHtml;
                document.getElementById('sfaeBindSaveBtn').textContent = '保存';
            })
            .catch(function() {
                bodyEl.innerHTML = '<div style="text-align:center;padding:30px;color:var(--danger);">加载资产失败</div>';
            });

        showModal('sfaeBindModal');
    }

    function sfaeToggleFrame(idx) {
        // If clicking the already-open frame, close it
        if (idx === _bindActiveFrame) {
            var target = document.getElementById('sfae-frame-assets-' + idx);
            if (target) target.style.display = 'none';
            var desc = document.getElementById('sfae-frame-desc-' + idx);
            if (desc) desc.style.display = 'none';
            var arrow = document.getElementById('sfae-arrow-' + idx);
            if (arrow) arrow.style.transform = 'rotate(0deg)';
            _bindActiveFrame = -1;
            return;
        }
        // Close all
        document.querySelectorAll('[id^="sfae-frame-assets-"]').forEach(function(el) {
            el.style.display = 'none';
        });
        document.querySelectorAll('[id^="sfae-frame-desc-"]').forEach(function(el) {
            el.style.display = 'none';
        });
        document.querySelectorAll('[id^="sfae-arrow-"]').forEach(function(el) {
            el.style.transform = 'rotate(0deg)';
        });
        // Open selected
        var target = document.getElementById('sfae-frame-assets-' + idx);
        if (target) target.style.display = 'grid';
        var desc = document.getElementById('sfae-frame-desc-' + idx);
        if (desc) desc.style.display = 'block';
        var arrow = document.getElementById('sfae-arrow-' + idx);
        if (arrow) arrow.style.transform = 'rotate(90deg)';
        _bindActiveFrame = idx;
    }

    function sfaeFrameSelectAll(frameIdx, checked) {
        var container = document.getElementById('sfae-frame-assets-' + frameIdx);
        if (!container) return;
        container.querySelectorAll('.sfae-frm-chk').forEach(function(c) { c.checked = checked; });
    }

    function doSfaeBindSave() {
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[_bindFrameShotIdx];
        if (!shot || !shot.id) { alert('分镜数据异常'); return; }

        // Collect per-frame asset names
        var frameAssets = [];
        var frames = shot.frames || [];
        frames.forEach(function(f, fi) {
            var container = document.getElementById('sfae-frame-assets-' + fi);
            if (!container) return;
            var names = [];
            container.querySelectorAll('.sfae-frm-chk:checked').forEach(function(c) {
                names.push(c.getAttribute('data-asset'));
            });
            frameAssets.push({ frameIdx: fi, assetNames: names });
        });

        var btn = document.getElementById('sfaeBindSaveBtn');
        btn.disabled = true;
        btn.textContent = '保存中...';

        fetch('/api/v1/assets/replace-frame-assets', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                projectId: window.projectId,
                shotId: shot.id,
                frameAssets: frameAssets
            })
        })
        .then(function(r) { return r.json(); })
        .then(function(res) {
            btn.disabled = false;
            btn.textContent = '保存';
            if (res.success) {
                showToast(res.message, 'success');
                hideModal('sfaeBindModal');
                var idx = window.currentChapterIdx;
                window.loadShotsForChapter(idx, true).then(function() {
                    renderShotsTab();
                });
            } else {
                alert('保存失败：' + (res.message || '未知错误'));
            }
        })
        .catch(function(err) {
            btn.disabled = false;
            btn.textContent = '保存';
            alert('请求失败：' + err.message);
        });
    }

    // Expose globally
    window.showShotFrameAssetExtractModal = showShotFrameAssetExtractModal;
    window.doSfaeExtract = doSfaeExtract;
    window.showSfaeConfirmMerge = showSfaeConfirmMerge;
    window.doSfaeSave = doSfaeSave;
    window.sfaeToggleAll = sfaeToggleAll;
    window.showShotFrameAssetBindModal = showShotFrameAssetBindModal;
    window.sfaeToggleFrame = sfaeToggleFrame;
    window.sfaeFrameSelectAll = sfaeFrameSelectAll;
    window.doSfaeBindSave = doSfaeBindSave;
})();