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

        fetch('/api/v1/production/extract-asset-info', {
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
            if (!shots.length) throw new Error('无分镜数据');
            _importShotsData = shots;

            var typeIcons = { 'Actor':'\uD83D\uDCC8', 'Scene':'\uD83C\uDFD0', 'Bgm':'\uD83C\uDFA5', 'Prop':'\uD83D\uDD25', 'VoiceVoice':'\uD83C\uDFA4' };
            var html = '<div style="font-size:13px;color:var(--ok);margin-bottom:8px;">✓ 解析成功，共 ' + shots.length + ' 个分镜</div>';
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
        if (!_importShotsData || _importShotsData.length === 0) {
            alert('请先粘贴并预览有效的分镜 JSON');
            return;
        }
        var btn = document.getElementById('importShotsGoBtn');
        if (btn) {
            btn.disabled = true;
            btn.textContent = '保存中...';
        }

        var shots = _importShotsData.map(function(s) {
            return {
                shotName: s.shotName || '',
                shotNumber: s.shotNumber || '',
                shotSize: s.shotSize || '',
                cameraMovement: s.cameraMovement || '',
                duration: s.duration || 5,
                assetRefs: s.assetRefs || [],
                frames: (s.frames || []).map(function(f, fi) {
                    return {
                        frameType: f.frameType || (fi === 0 ? 'First' : fi === (s.frames?.length - 1 || 0) ? 'Last' : 'Middle'),
                        description: f.description || '',
                        order: f.order ?? fi,
                        startTime: f.startTime,
                        duration: f.duration
                    };
                })
            };
        });

        var body = {
            projectId: window.projectId,
            chapterIdx: window.currentChapterIdx,
            aiResponse: JSON.stringify({ shots: shots, newAssets: [] })
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
                loadChapters();
                render();
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
    var _shotAssetBindShotIdx = -1;

    function showShotAssetBindModal(shotIdx) {
        _shotAssetBindShotIdx = shotIdx;
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[shotIdx];

        var bindAssetModal = document.getElementById('bindAssetModal');
        if (!bindAssetModal) {
            var overlay = document.createElement('div');
            overlay.className = 'modal-overlay';
            overlay.id = 'bindAssetModal';
            overlay.innerHTML = '<div class="modal" style="width:720px;max-height:90vh;">'
                + '<div class="modal-header">'
                + '<h3>🏷️ 绑定分镜资产 - Shot ' + (shotIdx + 1) + '</h3>'
                + '<button class="modal-close" onclick="hideModal(\'bindAssetModal\')">&times;</button>'
                + '</div>'
                + '<div class="modal-body" style="padding:16px;max-height:60vh;overflow-y:auto;">'
                + '<div style="margin-bottom:12px;padding:10px;background:var(--bg);border-radius:6px;font-size:13px;color:var(--text2);">'
                + '<strong>分镜：</strong>' + (shot.shotName || '未命名') + ' | <strong>已绑定：</strong><span id="bindAssetCurrent" style="color:var(--text);"></span>'
                + '</div>'
                + '<div class="form-group">'
                + '<label>搜索资产</label>'
                + '<input type="text" id="bindAssetSearch" placeholder="输入名称筛选..." style="width:100%;padding:8px;border:1px solid var(--border);border-radius:6px;background:var(--surface2);color:var(--text);font-size:13px;">'
                + '</div>'
                + '<div id="bindAssetList" style="display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:8px;max-height:400px;overflow-y:auto;"></div>'
                + '</div>'
                + '<div class="modal-footer">'
                + '<button class="btn btn-ghost" onclick="hideModal(\'bindAssetModal\')">取消</button>'
                + '<button class="btn btn-primary" onclick="saveShotAssetBinding()">保存绑定</button>'
                + '</div></div>';
            document.body.appendChild(overlay);
        }

        loadBindAssetList();
        showModal('bindAssetModal');
    }

function loadBindAssetList() {
        fetch('/Assets/ListByProject?projectId=' + window.projectId)
            .then(function(r) { return r.json(); })
            .then(function(res) {
                var assets = res.data || [];
                var state = window.shotState[window.currentChapterIdx];
                var shot = state.shots[_shotAssetBindShotIdx];
                
                // Use new shot.assets array (from ShotAsset join table)
                var currentAssets = shot.assets || [];
                var currentNames = currentAssets.map(function(a) { return a.name || a.Name || ''; }).filter(Boolean);
                var currentRoles = currentAssets.reduce(function(map, a) { 
                    if (a.name) map[a.name] = a.role || a.Role || ''; 
                    return map; 
                }, {});

                var currentEl = document.getElementById('bindAssetCurrent');
                if (currentEl) currentEl.textContent = currentNames.length ? currentNames.join(', ') : '无';

                var typeIcons = { 'Actor':'👤', 'Scene':'🏞️', 'Bgm':'🎵', 'Prop':'📦', 'VoiceVoice':'🎤' };
                var typeNames = { 'Actor':'角色', 'Scene':'场景', 'Bgm':'BGM', 'Prop':'道具', 'VoiceVoice':'音色' };

                var html = assets.map(function(a) {
                    var isBound = currentNames.includes(a.name);
                    var icon = typeIcons[a.assetType] || '📄';
                    var typeLabel = typeNames[a.assetType] || a.assetType || '未知';
                    return '<label style="display:flex;align-items:center;gap:8px;padding:10px;background:var(--surface);border:1.5px solid ' + (isBound ? 'var(--primary)' : 'var(--border)') + ';border-radius:8px;cursor:pointer;transition:.15s;" onmouseover="this.style.background=\'var(--bg)\'" onmouseout="this.style.background=\'var(--surface)\'">'
                        + '<input type="checkbox" class="bind-asset-chk" value="' + a.name + '" data-type="' + (a.assetType || '') + '" data-role="' + (currentRoles[a.name] || '') + '" ' + (isBound ? 'checked' : '') + ' style="accent-color:var(--primary);width:18px;height:18px;">'
                        + '<span style="font-size:20px;">' + icon + '</span>'
                        + '<div style="flex:1;min-width:0;">'
                        + '<div style="font-weight:600;font-size:13px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">' + (a.name || '') + '</div>'
                        + '<div style="font-size:11px;color:var(--text3);">' + typeLabel + '</div>'
                        + '</div>'
                        + '</label>';
                }).join('');

                var listEl = document.getElementById('bindAssetList');
                if (listEl) listEl.innerHTML = html || '<div style="grid-column:1/-1;text-align:center;color:var(--text3);padding:20px;">暂无资产，请先在资产管理中创建</div>';

// Search filter
                var searchEl = document.getElementById('bindAssetSearch');
                if (searchEl) {
                    searchEl.oninput = function() {
                        var q = this.value.toLowerCase();
                        document.querySelectorAll('#bindAssetList label').forEach(function(l) {
                            var name = l.querySelector('div').textContent.toLowerCase();
                            l.style.display = name.includes(q) ? 'flex' : 'none';
                        });
                    };
                }
            });
    }

    function saveShotAssetBinding() {
        var chks = document.querySelectorAll('#bindAssetList .bind-asset-chk:checked');
        var selectedNames = Array.from(chks).map(function(c){ return c.value; });
        var selectedTypes = Array.from(chks).map(function(c){ return c.getAttribute('data-type'); });
        var selectedRoles = Array.from(chks).map(function(c){ return c.getAttribute('data-role') || ''; });

        if (selectedNames.length === 0) {
            showToast('请至少选择一个资产', 'error');
            return;
        }

        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[_shotAssetBindShotIdx];

        // Update local state
        shot.assetRefs = selectedNames.join(',');
        shot.assets = selectedNames.map(function(name, i) {
            return { name: name, assetType: selectedTypes[i] || '', role: selectedRoles[i] || '' };
        });

        // Save to backend
        var btn = document.querySelector('#bindAssetModal .btn-primary');
        if (btn) { btn.disabled = true; btn.textContent = '保存中...'; }

        fetch('/api/v1/production/shots/' + shot.id + '/assets', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ assetRefs: selectedNames, roles: selectedRoles })
        })
        .then(function(r) { return r.json(); })
        .then(function(res) {
            if (btn) { btn.disabled = false; btn.textContent = '保存绑定'; }
            if (res.success) {
                showToast('资产绑定保存成功', 'success');
                hideModal('bindAssetModal');
                renderShotsTab();
            } else {
                alert('保存失败：' + (res.message || '未知错误'));
            }
        })
        .catch(function(err) {
            if (btn) { btn.disabled = false; btn.textContent = '保存绑定'; }
            alert('请求失败：' + err.message);
        });
    }

    // Expose globally
    window.showShotAssetBindModal = showShotAssetBindModal;
    window.loadBindAssetList = loadBindAssetList;
    window.saveShotAssetBinding = saveShotAssetBinding;
})();