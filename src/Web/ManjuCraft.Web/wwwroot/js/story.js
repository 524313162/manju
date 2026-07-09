var _summaryEditable = false;

(function(){ init(); })();

function init() {
    var savedProviderId = localStorage.getItem('story_providerId');
    loadChapters();
    render();
    loadProviderList(savedProviderId);
    loadTemplates();
}

function loadChapters() {
    fetch('/Story/GetChapters?storyId=' + storyId)
        .then(function(r){ return r.json(); })
        .then(function(res){
            if (res && res.success) {
                chapters = (res.data || []).map(function(c){
                    if (typeof c.chapterName === 'string') { c.chapterName = c.chapterName.replace(/\\u[\dA-F]{4}/gi, function(m){ return String.fromCharCode(parseInt(m.replace(/\\u/, ''), 16)); }); var d=document.createElement('div'); d.innerHTML=c.chapterName; c.chapterName=d.textContent; }
                    if (typeof c.content === 'string') { c.content = c.content.replace(/\\u[\dA-F]{4}/gi, function(m){ return String.fromCharCode(parseInt(m.replace(/\\u/, ''), 16)); }); var d=document.createElement('div'); d.innerHTML=c.content; c.content=d.textContent; }
                    return c;
                });
            } else {
                chapters = [];
            }
            render();
        })
        .catch(function(){ chapters = []; render(); });
}

function loadProviderList(savedProviderId) {
    fetch('/api/v1/providers/list')
        .then(function(r){ return r.json(); })
        .then(function(res){
            var list = res.data || [];
            allProviders = list;
            var sel = document.getElementById('providerSelect');
            var textProviders = list.filter(function(p){ return p.capability === 1; });
            if (textProviders.length === 0) {
                sel.innerHTML = '<option value="">未找到文本生成提供者，请到设置 → API 管理中添加</option>';
                return;
            }
            var html = '';
            var preSelected = savedProviderId || (currentProvider && currentProvider.id);
            textProviders.forEach(function(p) {
                var selAttr = preSelected && preSelected == p.id ? ' selected' : '';
                html += '<option value="' + p.id + '">' + p.name + ' [' + (p.model || '') + ']</option>';
            });
            sel.innerHTML = html;
            sel.addEventListener('change', onProviderChange);
            onProviderChange();
        })
        .catch(function(){
            if (currentProvider) {
                document.getElementById('providerSelect').innerHTML = '<option value="' + currentProvider.id + '" selected>' + currentProvider.name + '</option>';
                onProviderChange();
            }
        });
}

function onProviderChange() {
    var sel = document.getElementById('providerSelect');
    var val = sel.value;
    var p = allProviders.find(function(x){ return x.id == val; });
    if (!p) return;
    currentProvider = p;
    localStorage.setItem('story_providerId', p.id);
    document.getElementById('providerInfo').style.display = 'block';
    document.getElementById('providerName').textContent = p.name;
    if (p.model) document.getElementById('providerModel').textContent = '模型: ' + p.model;
}

function render() {
    document.getElementById('pCount').textContent = chapters.length + ' 章节';
    if (chapters.length === 0) {
        document.getElementById('chapterList').innerHTML = '<div style="padding:20px;text-align:center;color:#94a3b8;font-size:13px;">暂无章节</div>';
        document.getElementById('area').innerHTML = '<div class="empty-state"><div class="empty-state-icon">&#128214;</div><p>点击右上角「AI 创作」或「添加章节」创建剧本</p></div>';
        document.getElementById('title').textContent = '剧本创作';
        return;
    }
    var h = '';
    chapters.forEach(function(c, i) {
        var num = '第' + (c.chapterNumber || (i+1)) + '章';
        h += '<div class="chapter-item' + (i === ci ? ' active' : '') + '" onclick="sel(' + i + ')">'
            + '<span class="chapter-num">' + num + '</span>'
            + '<div class="chapter-info"><div class="chapter-name">' + c.chapterName + '</div>'
            + '<div class="chapter-meta">' + (c.content || '').length + ' 字</div></div></div>';
    });
    document.getElementById('chapterList').innerHTML = h;
    if (ci < 0 || ci >= chapters.length) ci = 0;
    var c = chapters[ci];
    if (!c) return;
    var num2 = '第' + (c.chapterNumber || (ci+1)) + '章';
    document.getElementById('title').textContent = num2 + ': ' + c.chapterName;
    var html = '<div style="background:var(--surface);border:1px solid var(--border);border-radius:var(--radius);padding:24px;">'
        + '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">'
        + '<h3 style="font-size:16px;font-weight:700;">' + num2 + ': ' + c.chapterName + '</h3>'
        + '<div style="display:flex;gap:6px;">'
        + '<button class="btn btn-info btn-sm" onclick="showRewriteModal(' + ci + ')">&#9889; AI 改写/扩写</button>'
        + '<button class="btn btn-ghost btn-sm" onclick="showChapterModal(' + ci + ')">&#9998; 编辑</button>'
        + '<button class="btn btn-ghost btn-sm" style="color:var(--danger);" onclick="delChapter(' + ci + ')">&#128465; 删除</button>'
        + '</div></div>'
        + '<div class="content-body">' + formatMarkdown(c.content || '') + '</div>'
        + '</div>';
    document.getElementById('area').innerHTML = html;
}

function sel(i) { ci = i; render(); }

var allTemplateStore = {};

function loadTemplates() {
    return fetch('/Story/templates')
        .then(function(r) { return r.json(); })
        .then(function(res) {
            if (res.warning) {
                console.warn('模板警告:', res.warning);
                if (res.data && res.data.length > 0) {
                    allTemplateStore = {};
                    res.data.forEach(function(t){ allTemplateStore[t.templateType] = t.content; });
                    document.getElementById('genTemplate').value = allTemplateStore['StoryGeneration'] || res.data[0].content || '';
                } else {
                    document.getElementById('genTemplate').value = '[提示: 数据库中没有任何提示词模板。请检查种子数据是否正确写入。]';
                }
                return;
            }
            if (res.success && res.data) {
                allTemplateStore = {};
                res.data.forEach(function(t){ allTemplateStore[t.templateType] = t.content; });
                document.getElementById('genTemplate').value = allTemplateStore['StoryGeneration'] || '';
            } else {
                document.getElementById('genTemplate').value = '';
            }
        })
        .catch(function() {
            document.getElementById('genTemplate').value = '';
        });
}

function showGenModal() {
    if (_genPoller) { _genPoller.stop(); _genPoller = null; }
    document.getElementById('storyTitle').value = currentStoryTitle || (typeof projectName !== 'undefined' ? projectName : '');
    document.getElementById('storyPrompt').value = currentStorySummary || '';
    document.getElementById('genResult').style.display = 'none';
    document.getElementById('genResult').innerHTML = '';
    document.getElementById('genGoBtn').style.display = '';
    document.getElementById('genGoBtn').disabled = false;
    document.getElementById('genGoBtn').textContent = '开始生成';
    document.getElementById('genCancelBtn').style.display = '';
    document.getElementById('genSaveBtn').style.display = 'none';
    document.getElementById('genRetryBtn').style.display = 'none';
    document.getElementById('genStopBtn').style.display = 'none';
    document.getElementById('genManualBtn').style.display = 'none';
    loadTemplates().then(function() {
        document.getElementById('genTemplate').value = allTemplateStore['StoryGeneration'] || '';
    });
    showModal('genModal');
}

function doAI() {
    var title = document.getElementById('storyTitle').value.trim();
    var prompt = document.getElementById('storyPrompt').value.trim();
    var template = document.getElementById('genTemplate').value.trim();
    if (!title) { alert('请输入剧本标题'); return; }
    if (!prompt) { alert('请输入故事内容'); return; }
    if (!currentProvider || !currentProvider.id) {
        alert('请先选择 AI 提供者');
        return;
    }

    var formData = new FormData();
    formData.append('storyId', storyId);
    formData.append('title', title);
    formData.append('summary', prompt);
    fetch('/Story/update-summary', { method: 'POST', body: formData });

    document.getElementById('genGoBtn').disabled = true;
    document.getElementById('genGoBtn').textContent = '生成中...';
    document.getElementById('genResult').style.display = 'block';
    document.getElementById('genResult').innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">AI 正在生成剧本，请稍候...</p></div>';

    var params = new URLSearchParams();
    params.append('title', title);
    params.append('prompt', prompt);
    params.append('template', template || '');
    params.append('providerId', currentProvider.id);

    fetch('/Story/generate', { method: 'POST', body: params })
        .then(function(r) { return r.json(); })
        .then(function(res) {
            if (res.success) {
                if (res.isComfyui) {
                    pollAiResult(res.promptId, res.workflowType);
                } else {
                    showAiResult(res.data);
                }
            } else {
                document.getElementById('genResult').innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;">'
                    + '<p style="font-size:13px;font-weight:700;color:var(--danger);">生成失败：' + (res.message || '未知错误') + '</p></div>';
                document.getElementById('genGoBtn').style.display = '';
                document.getElementById('genGoBtn').disabled = false;
                document.getElementById('genGoBtn').textContent = '重新生成';
                document.getElementById('genCancelBtn').style.display = '';
            }
        })
        .catch(function(err) {
            document.getElementById('genResult').innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;">'
                + '<p style="font-size:13px;font-weight:700;color:var(--danger);">请求失败：' + err.message + '</p></div>';
            document.getElementById('genGoBtn').style.display = '';
            document.getElementById('genGoBtn').disabled = false;
            document.getElementById('genGoBtn').textContent = '重新生成';
            document.getElementById('genCancelBtn').style.display = '';
        });
}

var _lastAiResult = null;

function showAiResult(rawResponse) {
    _lastAiResult = rawResponse;
    var displayText = rawResponse;
    try {
        var text = rawResponse;
        var backtickMatch = text.match(/```(?:json)?\s*([\s\S]*?)```/);
        if (backtickMatch) text = backtickMatch[1].trim();
        var parsed = JSON.parse(text);
        displayText = JSON.stringify(parsed, null, 2);
    } catch (e) {
        displayText = rawResponse;
    }

    document.getElementById('genResult').innerHTML = '<div class="form-group"><label>AI 返回结果（可编辑，确认后可提交保存）</label>'
        + '<textarea id="aiResultJson" rows="14" style="width:100%;font-family:monospace;font-size:12px;">' + escapeHtml(displayText) + '</textarea></div>';

    document.getElementById('genGoBtn').style.display = 'none';
    document.getElementById('genCancelBtn').style.display = '';
    document.getElementById('genSaveBtn').style.display = '';
    document.getElementById('genRetryBtn').style.display = '';
    document.getElementById('genStopBtn').style.display = 'none';
    document.getElementById('genManualBtn').style.display = 'none';
}

function submitAiResult() {
    var jsonStr = document.getElementById('aiResultJson').value.trim();
    if (!jsonStr) { alert('内容为空'); return; }
    saveScript(jsonStr, function() {
        hideModal('genModal');
    });
}

function escapeHtml(str) {
    var div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

function cancelGenModal() {
    if (_genPoller) { _genPoller.stop(); _genPoller = null; }
    hideModal('genModal');
}

function cancelRewriteModal() {
    if (_rewritePoller) { _rewritePoller.stop(); _rewritePoller = null; }
    hideModal('rewriteModal');
}

var _genPoller = null;

function pollAiResult(promptId, workflowType) {
    document.getElementById('genResult').style.display = 'block';
    document.getElementById('genResult').innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">AI 正在生成中，请耐心等待...</p><p style="margin-top:6px;color:var(--text3);font-size:11px;">任务ID: ' + promptId + '</p></div>';

    document.getElementById('genGoBtn').style.display = 'none';
    document.getElementById('genCancelBtn').style.display = 'none';
    document.getElementById('genSaveBtn').style.display = 'none';
    document.getElementById('genRetryBtn').style.display = 'none';
    document.getElementById('genStopBtn').style.display = '';
    document.getElementById('genManualBtn').style.display = '';

    var stopBtn = document.getElementById('genStopBtn');
    var manualBtn = document.getElementById('genManualBtn');
    stopBtn.onclick = function() {
        if (_genPoller) _genPoller.stop();
        stopBtn.style.display = 'none';
        manualBtn.style.display = 'none';
        document.getElementById('genGoBtn').style.display = '';
        document.getElementById('genGoBtn').disabled = false;
        document.getElementById('genGoBtn').textContent = '重新生成';
        document.getElementById('genCancelBtn').style.display = '';
        document.getElementById('genResult').innerHTML = '<div style="padding:14px;background:var(--surface2);border-radius:8px;margin-top:12px;"><p style="font-size:13px;color:var(--text2);">已停止自动获取。任务ID: ' + promptId + '</p></div>';
    };
    manualBtn.onclick = function() {
        if (!_genPoller) return;
        document.getElementById('genResult').innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">正在手动获取结果...</p></div>';
        _genPoller.fetchNow();
    };

    _genPoller = createPoller({
        promptId: promptId,
        workflowType: workflowType,
        resultType: 'text',
        intervalMs: 10000,
        timeoutMs: 600000,
        onSuccess: function(data) {
            stopBtn.style.display = 'none';
            manualBtn.style.display = 'none';
            showAiResult(data.text || '');
        },
        onTimeout: function() {
            stopBtn.style.display = 'none';
            manualBtn.style.display = 'none';
            document.getElementById('genGoBtn').style.display = '';
            document.getElementById('genGoBtn').disabled = false;
            document.getElementById('genGoBtn').textContent = '重新生成';
            document.getElementById('genCancelBtn').style.display = '';
            document.getElementById('genResult').innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;font-weight:700;color:var(--danger);">等待超时（10分钟），任务仍在后台执行。任务ID: ' + promptId + '</p></div>';
        },
        onFetchEmpty: function() {
            document.getElementById('genResult').innerHTML = '<div style="padding:14px;background:var(--surface2);border-radius:8px;margin-top:12px;"><p style="font-size:13px;color:var(--text2);">尚未生成完毕，请稍后再试。任务ID: ' + promptId + '</p></div>';
        },
        onFetchError: function(msg) {
            document.getElementById('genResult').innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;color:var(--danger);">' + (msg || '请求失败') + '</p></div>';
        },
        onError: function(msg) {
            stopBtn.style.display = 'none';
            manualBtn.style.display = 'none';
            document.getElementById('genGoBtn').style.display = '';
            document.getElementById('genGoBtn').disabled = false;
            document.getElementById('genGoBtn').textContent = '重新生成';
            document.getElementById('genCancelBtn').style.display = '';
            document.getElementById('genResult').innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;font-weight:700;color:var(--danger);">' + (msg || '生成失败') + '</p></div>';
        }
    });
}

var _rewritePoller = null;

function pollRewriteResult(promptId, workflowType) {
    document.getElementById('rewriteResult').style.display = 'block';
    document.getElementById('rewriteResult').innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">AI 正在改写中，请耐心等待...</p></div>';

    document.getElementById('rewriteGoBtn').disabled = true;
    document.getElementById('rewriteGoBtn').textContent = '等待中...';

    var actions = document.getElementById('rewriteActions');
    if (!actions) {
        actions = document.querySelector('#rewriteModal .form-actions');
    }
    var stopBtn = document.getElementById('rewriteStopBtn');
    var manualBtn = document.getElementById('rewriteManualBtn');
    if (!stopBtn) {
        stopBtn = document.createElement('button');
        stopBtn.className = 'btn btn-danger';
        stopBtn.id = 'rewriteStopBtn';
        stopBtn.textContent = '立即停止获取';
        stopBtn.onclick = function() {
            if (_rewritePoller) _rewritePoller.stop();
            stopBtn.style.display = 'none';
            manualBtn.style.display = 'none';
            document.getElementById('rewriteGoBtn').disabled = false;
            document.getElementById('rewriteGoBtn').textContent = '重试';
            document.getElementById('rewriteResult').innerHTML = '<div style="padding:14px;background:var(--surface2);border-radius:8px;margin-top:12px;"><p style="font-size:13px;color:var(--text2);">已停止自动获取。任务ID: ' + promptId + '</p></div>';
        };
        actions.insertBefore(stopBtn, actions.firstChild);
    }
    if (!manualBtn) {
        manualBtn = document.createElement('button');
        manualBtn.className = 'btn btn-info';
        manualBtn.id = 'rewriteManualBtn';
        manualBtn.textContent = '手动获取';
        manualBtn.style.marginLeft = 'auto';
        manualBtn.onclick = function() {
            if (!_rewritePoller) return;
            document.getElementById('rewriteResult').innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">正在手动获取结果...</p></div>';
            _rewritePoller.fetchNow();
        };
        actions.insertBefore(manualBtn, actions.firstChild);
    }
    stopBtn.style.display = '';
    manualBtn.style.display = '';

    _rewritePoller = createPoller({
        promptId: promptId,
        workflowType: workflowType,
        resultType: 'text',
        intervalMs: 10000,
        timeoutMs: 600000,
        onSuccess: function(data) {
            stopBtn.style.display = 'none';
            manualBtn.style.display = 'none';

            var newContent = data.text || '';
            var previewHtml = '<div style="background:var(--ok-bg);border-radius:8px;padding:14px;margin-bottom:12px;">'
                + '<p style="font-size:13px;font-weight:700;color:var(--ok);">&#10003; 改写完成，请确认后点击保存</p>'
                + '</div>'
                + '<div class="form-group"><label>改写后内容（可编辑）</label>'
                + '<textarea id="rewritePreviewContent" rows="12" style="width:100%;font-family:inherit;font-size:13px;">' + escapeHtml(newContent) + '</textarea></div>';

            document.getElementById('rewriteResult').innerHTML = previewHtml;
            document.getElementById('rewriteGoBtn').textContent = '确认保存';
            document.getElementById('rewriteGoBtn').disabled = false;
            document.getElementById('rewriteGoBtn').onclick = function() {
                var idx = parseInt(document.getElementById('rewriteChapterIdx').value);
                var c = chapters[idx];
                var finalContent = document.getElementById('rewritePreviewContent').value.trim();
                if (!finalContent) { alert('内容不能为空'); return; }
                document.getElementById('rewriteGoBtn').disabled = true;
                document.getElementById('rewriteGoBtn').textContent = '保存中...';

                fetch('/Story/EditChapter', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: c.id, chapterName: c.chapterName, content: finalContent })
                })
                .then(function(r2){ return r2.json(); })
                .then(function(saveRes) {
                    if (saveRes.success) {
                        chapters[idx].content = finalContent;
                        document.getElementById('rewriteResult').innerHTML = '<div style="padding:14px;background:var(--ok-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;font-weight:700;color:var(--ok);">&#10003; 保存成功</p></div>';
                        setTimeout(function() {
                            hideModal('rewriteModal');
                            loadChapters();
                            render();
                        }, 1000);
                    } else {
                        alert('保存失败');
                        document.getElementById('rewriteGoBtn').disabled = false;
                        document.getElementById('rewriteGoBtn').textContent = '确认保存';
                    }
                })
                .catch(function(){ alert('网络错误'); });
            };
        },
        onTimeout: function() {
            stopBtn.style.display = 'none';
            manualBtn.style.display = 'none';
            document.getElementById('rewriteGoBtn').disabled = false;
            document.getElementById('rewriteGoBtn').textContent = '重试';
            document.getElementById('rewriteResult').innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;font-weight:700;color:var(--danger);">改写超时，任务仍在后台执行。任务ID: ' + promptId + '</p></div>';
        },
        onFetchEmpty: function() {
            document.getElementById('rewriteResult').innerHTML = '<div style="padding:14px;background:var(--surface2);border-radius:8px;margin-top:12px;"><p style="font-size:13px;color:var(--text2);">尚未改写完毕，请稍后再试。任务ID: ' + promptId + '</p></div>';
        },
        onFetchError: function(msg) {
            document.getElementById('rewriteResult').innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;color:var(--danger);">' + (msg || '请求失败') + '</p></div>';
        }
    });
}

function showChapterModal(idx) {
    document.getElementById('editChapterIdx').value = idx;
    if (idx >= 0) {
        var c = chapters[idx];
        document.getElementById('editChapterId').value = c.id;
        document.getElementById('chapterModalTitle').textContent = '编辑章节';
        document.getElementById('chName').value = c.chapterName || '';
        document.getElementById('chContent').value = c.content || '';
    } else {
        document.getElementById('editChapterId').value = '';
        document.getElementById('chapterModalTitle').textContent = '添加章节';
        document.getElementById('chName').value = '';
        document.getElementById('chContent').value = '';
    }
    showModal('chapterModal');
}

function saveChapter() {
    var idx = parseInt(document.getElementById('editChapterIdx').value);
    var name = document.getElementById('chName').value.trim();
    var content = document.getElementById('chContent').value.trim();
    if (!name) { alert('请输入章节名称'); return; }
    if (!content) { alert('请输入章节内容'); return; }

    if (idx >= 0) {
        var chapterId = parseInt(document.getElementById('editChapterId').value);
        fetch('/Story/EditChapter', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ id: chapterId, chapterName: name, content: content })
        })
        .then(function(r){ return r.json(); })
        .then(function(res) {
            if (res.success) {
                chapters[idx].chapterName = name;
                chapters[idx].content = content;
                hideModal('chapterModal');
                render();
            } else {
                alert('保存失败：' + (res.message || '未知错误'));
            }
        })
        .catch(function(){ alert('网络错误'); });
    } else {
        fetch('/Story/AddChapter', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ storyId: storyId, chapterName: name, content: content })
        })
        .then(function(r){ return r.json(); })
        .then(function(res) {
            if (res.success) {
                chapters.push(res.data);
                hideModal('chapterModal');
                loadChapters();
            } else {
                alert('添加失败：' + (res.message || '未知错误'));
            }
        })
        .catch(function(){ alert('网络错误'); });
    }
}

function delChapter(idx) {
    if (!confirm('确认删除「' + chapters[idx].chapterName + '」？')) return;
    var chapterId = chapters[idx].id;
    fetch('/Story/DeleteChapter', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: chapterId })
    })
    .then(function(r){ return r.json(); })
    .then(function(res) {
        if (res.success) {
            chapters.splice(idx, 1);
            chapters.forEach(function(c, i) { c.chapterNumber = i + 1; });
            if (chapters.length === 0) ci = -1;
            else ci = Math.min(ci, chapters.length - 1);
            render();
        } else {
            alert('删除失败：' + (res.message || '未知错误'));
        }
    })
    .catch(function(){ alert('网络错误'); });
}

function showSummaryModal() {
    document.getElementById('summaryTitleView').textContent = currentStoryTitle || '暂无内容';
    document.getElementById('summaryContentView').textContent = currentStorySummary || '暂无故事简介';
    if (_summaryEditable) cancelSummaryEdit();
    showModal('summaryModal');
}

function toggleSummaryEdit() {
    _summaryEditable = !_summaryEditable;
    var titleView = document.getElementById('summaryTitleView');
    var titleInput = document.getElementById('summaryTitleInput');
    var contentView = document.getElementById('summaryContentView');
    var contentInput = document.getElementById('summaryContentInput');
    var editBtn = document.getElementById('summaryEditBtn');
    var saveBtn = document.getElementById('summarySaveBtn');
    var cancelBtn = document.getElementById('summaryCancelBtn');

    if (_summaryEditable) {
        titleView.style.display = 'none';
        titleInput.style.display = '';
        titleInput.value = titleView.textContent;
        contentView.style.display = 'none';
        contentInput.style.display = '';
        contentInput.value = contentView.textContent;
        editBtn.style.display = 'none';
        saveBtn.style.display = '';
        cancelBtn.style.display = '';
    } else {
        revertSummaryDisplay();
    }
}

function revertSummaryDisplay() {
    _summaryEditable = false;
    document.getElementById('summaryTitleView').style.display = '';
    document.getElementById('summaryTitleInput').style.display = 'none';
    document.getElementById('summaryContentView').style.display = '';
    document.getElementById('summaryContentInput').style.display = 'none';
    document.getElementById('summaryEditBtn').style.display = '';
    document.getElementById('summarySaveBtn').style.display = 'none';
    document.getElementById('summaryCancelBtn').style.display = 'none';
}

function cancelSummaryEdit() {
    revertSummaryDisplay();
}

function saveSummary() {
    var titleInput = document.getElementById('summaryTitleInput');
    var contentInput = document.getElementById('summaryContentInput');
    var title = titleInput.value.trim();
    var summary = contentInput.value.trim();
    if (!title) { alert('请输入故事名称'); return; }

    var formData = new FormData();
    formData.append('storyId', storyId);
    formData.append('title', title);
    formData.append('summary', summary);

    fetch('/Story/update-summary', { method: 'POST', body: formData })
        .then(function(r){ return r.json(); })
        .then(function(res) {
            if (res.success) {
                currentStoryTitle = title;
                currentStorySummary = summary;
                revertSummaryDisplay();
                document.getElementById('pName').textContent = title;
                document.getElementById('summaryTitleView').textContent = title;
                document.getElementById('summaryContentView').textContent = summary;
            } else {
                alert('保存失败：' + (res.message || '未知错误'));
            }
        })
        .catch(function(err){ alert('网络错误：' + err.message); });
}

function showImportModal() {
    document.getElementById('importJson').value = '';
    document.getElementById('importResult').style.display = 'none';
    document.getElementById('importResult').innerHTML = '';
    document.getElementById('importGoBtn').disabled = false;
    document.getElementById('importGoBtn').textContent = '导入';
    showModal('importModal');
}

function doImport() {
    var jsonStr = document.getElementById('importJson').value.trim();
    if (!jsonStr) { alert('请输入 JSON 内容'); return; }

    var resultEl = document.getElementById('importResult');
    var goBtn = document.getElementById('importGoBtn');

    goBtn.disabled = true;
    goBtn.textContent = '导入中...';
    resultEl.style.display = 'block';
    resultEl.innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">正在导入...</p></div>';

    saveScript(jsonStr, function() {
        setTimeout(function(){ hideModal('importModal'); }, 1500);
    }, function(errMsg) {
        resultEl.innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;font-weight:700;color:var(--danger);">' + errMsg + '</p></div>';
        goBtn.disabled = false;
        goBtn.textContent = '重试';
    });
}

function saveScript(jsonStr, onSuccess, onError) {
    var text = jsonStr;
    var backtickMatch = text.match(/```(?:json)?\s*([\s\S]*?)```/);
    if (backtickMatch) text = backtickMatch[1].trim();

    var parsed;
    try { parsed = JSON.parse(text); } catch (e) {
        if (onError) { onError('JSON 格式错误：' + e.message); } else { alert('JSON 格式错误，请检查后重试'); }
        return;
    }
    parsed.storyId = storyId;
    parsed.projectId = projectId;

    var parts = [];
    if (parsed.chapters) parts.push('章节 ' + parsed.chapters.length + ' 个');
    var confirmMsg = '警告：提交后将替换所有现有章节，此操作不可撤销。\n\n';
    if (parts.length > 0) confirmMsg += '将保存：' + parts.join('、') + '。\n\n确定提交？';
    else confirmMsg += '确认提交保存？';
    if (!confirm(confirmMsg)) {
        if (onError) onError('已取消');
        return;
    }

    fetch('/Story/bulk-save-ai-result', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(parsed)
    })
    .then(function(r) { return r.json(); })
    .then(function(res) {
        if (res.success) {
            loadChapters();
            render();
            if (onSuccess) onSuccess();
        } else {
            if (onError) onError('保存失败：' + (res.message || '未知错误'));
        }
    })
    .catch(function() {
        if (onError) onError('网络请求失败');
    });
}

function showRewriteModal(idx) {
    if (_rewritePoller) { _rewritePoller.stop(); _rewritePoller = null; }
    var c = chapters[idx];
    document.getElementById('rewriteChapterIdx').value = idx;
    document.getElementById('rewriteOriginal').value = c.content || '';
    tryLoadRewriteTemplate();
    document.getElementById('rewriteResult').style.display = 'none';
    document.getElementById('rewriteResult').innerHTML = '';
    document.getElementById('rewriteGoBtn').disabled = false;
    document.getElementById('rewriteGoBtn').textContent = '开始改写';
    loadRewriteProviderList();
    showModal('rewriteModal');
}

function loadRewriteProviderList() {
    var sel = document.getElementById('rewriteProviderSelect');
    if (sel.options.length > 1) return;
    var saved = localStorage.getItem('rewrite_providerId');
    var textProviders = allProviders.filter(function(p){ return p.capability === 1; });
    if (textProviders.length === 0) {
        sel.innerHTML = '<option value="">未找到文本生成提供者</option>';
        return;
    }
    var html = '';
    var preSelected = saved || (currentProvider && currentProvider.id);
    textProviders.forEach(function(p) {
        var s = preSelected && preSelected == p.id ? ' selected' : '';
        html += '<option value="' + p.id + '"' + s + '>' + p.name + ' [' + (p.model || '') + ']</option>';
    });
    sel.innerHTML = html;
}

function tryLoadRewriteTemplate() {
    // Use cached templates from loadTemplates() if available
    if (allTemplateStore['RewriteStory']) {
        document.getElementById('rewriteTemplate').value = allTemplateStore['RewriteStory'];
        return;
    }
    // Fallback: fetch again if cache not available
    fetch('/Story/templates')
        .then(function(r) { return r.json(); })
        .then(function(res) {
            if (res.success && res.data) {
                res.data.forEach(function(t){
                    allTemplateStore[t.templateType] = t.content;
                    if (t.templateType === 'RewriteStory') {
                        document.getElementById('rewriteTemplate').value = t.content;
                    }
                });
            } else {
                document.getElementById('rewriteTemplate').value = '';
            }
        });
}

function doRewrite() {
    var idx = parseInt(document.getElementById('rewriteChapterIdx').value);
    var c = chapters[idx];
    var content = document.getElementById('rewriteOriginal').value.trim();
    var template = document.getElementById('rewriteTemplate').value.trim();
    var mode = document.getElementById('rewriteMode').value;
    var rewriteProviderSelect = document.getElementById('rewriteProviderSelect');
    var rewriteProviderId = rewriteProviderSelect ? rewriteProviderSelect.value : null;
    if (!content) { alert('请输入要改写的内容'); return; }
    if (!rewriteProviderId) {
        alert('请先选择 AI 提供者');
        return;
    }
    localStorage.setItem('rewrite_providerId', rewriteProviderId);

    document.getElementById('rewriteGoBtn').disabled = true;
    document.getElementById('rewriteGoBtn').textContent = '改写中...';
    document.getElementById('rewriteResult').style.display = 'block';
    document.getElementById('rewriteResult').innerHTML = '<div style="text-align:center;padding:20px;"><div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div><p style="margin-top:10px;color:var(--text2);font-size:13px;">AI 正在改写，请稍候...</p></div>';

    var params = new URLSearchParams();
    params.append('content', content);
    params.append('template', template || '');
    params.append('providerId', rewriteProviderId);
    params.append('mode', mode);

    fetch('/Story/rewrite', {
        method: 'POST',
        body: params
    })
    .then(function(r) { return r.json(); })
    .then(function(res) {
        if (res.success) {
            if (res.isComfyui) {
                pollRewriteResult(res.promptId, res.workflowType);
                return;
            }
            var newContent = res.data;

            var previewHtml = '<div style="background:var(--ok-bg);border-radius:8px;padding:14px;margin-bottom:12px;">'
                + '<p style="font-size:13px;font-weight:700;color:var(--ok);">&#10003; 改写完成，请确认后点击保存</p>'
                + '</div>'
                + '<div class="form-group"><label>改写后内容（可编辑）</label>'
                + '<textarea id="rewritePreviewContent" rows="12" style="width:100%;font-family:inherit;font-size:13px;">' + escapeHtml(newContent) + '</textarea></div>';

            document.getElementById('rewriteResult').innerHTML = previewHtml;
            document.getElementById('rewriteGoBtn').textContent = '确认保存';
            document.getElementById('rewriteGoBtn').disabled = false;
            document.getElementById('rewriteGoBtn').onclick = function() {
                var finalContent = document.getElementById('rewritePreviewContent').value.trim();
                if (!finalContent) { alert('内容不能为空'); return; }
                document.getElementById('rewriteGoBtn').disabled = true;
                document.getElementById('rewriteGoBtn').textContent = '保存中...';

                fetch('/Story/EditChapter', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: c.id, chapterName: c.chapterName, content: finalContent })
                })
                .then(function(r2){ return r2.json(); })
                .then(function(saveRes) {
                    if (saveRes.success) {
                        chapters[idx].content = finalContent;
                        document.getElementById('rewriteResult').innerHTML = '<div style="padding:14px;background:var(--ok-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;font-weight:700;color:var(--ok);">&#10003; 保存成功</p></div>';
                        setTimeout(function() {
                            hideModal('rewriteModal');
                            loadChapters();
                            render();
                        }, 1000);
                    } else {
                        alert('保存失败');
                        document.getElementById('rewriteGoBtn').disabled = false;
                        document.getElementById('rewriteGoBtn').textContent = '确认保存';
                    }
                })
                .catch(function(){ alert('网络错误'); });
            };
        } else {
            document.getElementById('rewriteResult').innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;font-weight:700;color:var(--danger);">改写失败：' + (res.message || '未知错误') + '</p></div>';
            document.getElementById('rewriteGoBtn').disabled = false;
            document.getElementById('rewriteGoBtn').textContent = '重试';
        }
    })
    .catch(function(err) {
        document.getElementById('rewriteResult').innerHTML = '<div style="padding:14px;background:var(--danger-bg);border-radius:8px;margin-top:12px;"><p style="font-size:13px;font-weight:700;color:var(--danger);">请求失败：' + err.message + '</p></div>';
        document.getElementById('rewriteGoBtn').disabled = false;
        document.getElementById('rewriteGoBtn').textContent = '重试';
    });
}
