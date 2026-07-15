// Production Utils - Common utilities
(function() {
    // Poller helper
    function createPoller(options) {
        var pollCount = 0;
        var timer = null;
        var stopped = false;

        function poll() {
            if (stopped) return;
            pollCount++;
            var resultType = options.resultType === 'text' ? 'text' : options.resultType;
            fetch('/api/v1/comfyui/result/' + options.promptId + '/' + resultType + '?workflowType=' + encodeURIComponent(options.workflowType))
                .then(function(r) { return r.json(); })
                .then(function(res) {
                    if (stopped) return;
                    if (res.success && res.data) {
                        if (options.onSuccess) options.onSuccess(res.data);
                        return;
                    }
                    if (!res.pending) {
                        if (options.onError) options.onError(res.message || '任务执行失败');
                        return;
                    }
                    if (pollCount >= (options.maxPolls || 120)) {
                        if (options.onTimeout) options.onTimeout();
                        return;
                    }
                    timer = setTimeout(poll, options.intervalMs || 10000);
                })
                .catch(function() {
                    if (stopped) return;
                    if (pollCount >= (options.maxPolls || 120)) {
                        if (options.onTimeout) options.onTimeout();
                    } else {
                        timer = setTimeout(poll, options.intervalMs || 10000);
                    }
                });
        }

        timer = setTimeout(poll, options.intervalMs || 10000);

        return {
            stop: function() {
                stopped = true;
                if (timer) clearTimeout(timer);
            }
        };
    }

    // Poller for AI results
    function pollAiResultForShot(promptId, workflowType, shotIdx, resultType, frameIdx) {
        if (frameIdx === undefined) frameIdx = 0;
        var pollCount = 0;
        var maxPolls = 120;
        var interval = 5000;

        var mapResultType = function(rt) {
            if (rt === 'video') return 'video';
            if (rt === 'frame') return 'image';
            if (rt === 'storyboard') return 'image';
            return 'image';
        };

        var timer = setInterval(function() {
            pollCount++;
            fetch('/api/v1/comfyui/result/' + promptId + '/' + mapResultType(resultType) + '?workflowType=' + encodeURIComponent(workflowType))
                .then(function(r) { return r.json(); })
                .then(function(res) {
                    if (res.success && res.data) {
                        clearInterval(timer);
                        var url = null;
                        var data = res.data;
                        if (data.imageUrls && data.imageUrls.length > 0) {
                            url = data.imageUrls[0];
                        } else if (data.videoUrls && data.videoUrls.length > 0) {
                            url = data.videoUrls[0];
                        } else if (data.audioUrls && data.audioUrls.length > 0) {
                            url = data.audioUrls[0];
                        }
                        if (url) {
                            if (resultType === 'video') {
                                window.shotState[window.currentChapterIdx].shots[shotIdx].videoUrl = url;
                            } else if (resultType === 'frame') {
                                window.shotState[window.currentChapterIdx].shots[shotIdx].frames[frameIdx].imagePath = url;
                                window.shotState[window.currentChapterIdx].shots[shotIdx].frames[frameIdx].hasImage = true;
                                if (frameIdx === 0) {
                                    window.shotState[window.currentChapterIdx].shots[shotIdx].hasFirstFrame = true;
                                }
                            } else if (resultType === 'storyboard') {
                                window.shotState[window.currentChapterIdx].shots[shotIdx].storyboardUrl = url;
                            }
                            window.shotState[window.currentChapterIdx].shots[shotIdx]['generating' + resultType.charAt(0).toUpperCase() + resultType.slice(1)] = false;
                            renderShotsTab();
                            if (resultType === 'video') {
                                showToast('视频生成完成！', 'success');
                            } else if (resultType === 'frame') {
                                showToast('帧图片生成完成！', 'success');
                            } else {
                                showToast('分镜图生成完成！', 'success');
                            }
                        }
                    } else if (!res.pending) {
                        clearInterval(timer);
                        alert(res.message || '任务执行失败');
                    }
                    if (pollCount >= maxPolls) {
                        clearInterval(timer);
                        renderShotsTab();
                        alert('生成超时，promptId: ' + promptId);
                    }
                })
                .catch(function() {
                    if (pollCount >= maxPolls) clearInterval(timer);
                });
        }, interval);
    }

    // Frame image provider dialog
    var _frameGenShotIdx = -1;
    var _frameGenFrameIdx = -1;
    var _frameGenModels = [];
    var _frameGenImageUrl = null;

    function showFrameImageProviderDialog(shotIdx, frameIdx) {
        _frameGenShotIdx = shotIdx;
        _frameGenFrameIdx = frameIdx;
        _frameGenImageUrl = null;

        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[shotIdx];
        var targetFrame = frameIdx >= 0 ? shot.frames[frameIdx] : null;
        var frameDesc = targetFrame ? targetFrame.description : '';

        var overlay = document.getElementById('frameProviderModal');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'modal-overlay';
            overlay.id = 'frameProviderModal';
            document.body.appendChild(overlay);
        }
        overlay.innerHTML = '<div class="modal" style="width:700px;">'
            + '<div class="modal-header">'
            + '<h3>📷 生成帧图片</h3>'
            + '<button class="modal-close" onclick="hideModal(\'frameProviderModal\')">&times;</button>'
            + '</div>'
            + '<div class="modal-body" style="padding:16px;">'
            + '<div class="form-group"><label>选择生成模型 *</label>'
            + '<select id="frameProviderSelect" style="width:100%;padding:8px;border:1px solid var(--border);border-radius:6px;background:var(--surface2);color:var(--text);font-size:14px;"><option value="">加载中...</option></select></div>'
            + '<div class="form-group" id="frameTemplateGroup" style="display:none;">'
            + '<label>系统模板提示词</label>'
            + '<textarea id="frameTemplatePromptDisplay" rows="6" style="width:100%;padding:8px;border:1px solid var(--border);border-radius:6px;background:var(--surface2);color:var(--text);font-size:12px;font-family:monospace;resize:vertical;" readonly></textarea>'
            + '</div>'
            + '<div class="form-group">'
            + '<label>帧描述提示词 *</label>'
            + '<textarea id="frameGenPrompt" rows="4" style="width:100%;padding:8px;border:1px solid var(--border);border-radius:6px;background:var(--surface2);color:var(--text);font-size:13px;resize:vertical;font-family:inherit;" placeholder="帧描述内容...">' + escapeHtml(frameDesc) + '</textarea>'
            + '</div>'
            + '<div class="form-group" id="framePreviewGroup" style="display:none;">'
            + '<label>生成预览</label>'
            + '<div style="text-align:center;padding:16px;background:#fafafa;border-radius:8px;border:1px solid var(--border);min-height:200px;display:flex;align-items:center;justify-content:center;">'
            + '<div id="framePreviewPlaceholder" style="color:#999;">生成后将在此显示预览</div>'
            + '<img id="framePreviewImg" src="" style="max-width:100%;max-height:400px;object-fit:contain;border-radius:8px;display:none;box-shadow:0 4px 16px rgba(0,0,0,0.1);" />'
            + '</div></div>'
            + '<div id="frameGenStatus" style="margin-top:12px;padding:12px;border-radius:8px;display:none;"></div>'
            + '</div>'
            + '<div class="modal-footer">'
            + '<button class="btn btn-ghost" onclick="hideModal(\'frameProviderModal\')">取消</button>'
            + '<button class="btn btn-primary" id="frameGenGoBtn" onclick="doGenerateFrameImage()">生成</button>'
            + '<button class="btn btn-success" id="frameGenSaveBtn" onclick="saveFrameGeneratedImage()" style="display:none;">保存到帧</button>'
            + '</div></div>';

        var sel = document.getElementById('frameProviderSelect');
        var saved = localStorage.getItem('frameGen_providerId');
        fetch('/api/v1/providers/list')
            .then(function(r){ return r.json(); })
            .then(function(res){
                var list = (res.data || []).filter(function(p){ return p.capability === 8 || p.capability === 'ImageToImage'; });
                _frameGenModels = list;
                var html = '';
                var preSelected = saved || (list.length > 0 ? list[0].id : null);
                list.forEach(function(p) {
                    var s = preSelected && preSelected == p.id ? ' selected' : '';
                    html += '<option value="' + p.id + '"' + s + '>' + p.name + ' [' + (p.model || '') + ']</option>';
                });
                sel.innerHTML = html || '<option value="">未找到 ImageToImage 提供者</option>';
            })
            .catch(function(){
                sel.innerHTML = '<option value="">加载失败</option>';
            });

        fetch('/api/v1/production/template?type=FrameImageGeneration')
            .then(function(r){ return r.json(); })
            .then(function(res){
                if (res.success && res.content) {
                    document.getElementById('frameTemplatePromptDisplay').value = res.content;
                    document.getElementById('frameTemplateGroup').style.display = '';
                } else {
                    document.getElementById('frameTemplateGroup').style.display = 'none';
                }
            })
            .catch(function(){
                document.getElementById('frameTemplateGroup').style.display = 'none';
            });

        showModal('frameProviderModal');
    }

    var _frameGenPoller = null;
    var _frameGenPromptId = null;
    var _frameGenWorkflowType = null;
    var _frameGenStartTime = null;

    async function doGenerateFrameImage() {
        var providerId = document.getElementById('frameProviderSelect').value;
        if (!providerId) { alert('请选择提供者'); return; }
        var userPrompt = document.getElementById('frameGenPrompt').value.trim();
        if (!userPrompt) { alert('请输入帧描述提示词'); return; }

        var templateContent = document.getElementById('frameTemplatePromptDisplay').value.trim();
        var combinedPrompt = templateContent ? templateContent.replace('{prompt}', userPrompt) : userPrompt;
        if (templateContent && combinedPrompt === templateContent) {
            combinedPrompt = templateContent + '\n\n' + userPrompt;
        }

        localStorage.setItem('frameGen_providerId', providerId);

        var goBtn = document.getElementById('frameGenGoBtn');
        var saveBtn = document.getElementById('frameGenSaveBtn');
        var statusEl = document.getElementById('frameGenStatus');
        var previewGroup = document.getElementById('framePreviewGroup');
        var previewImg = document.getElementById('framePreviewImg');
        var previewPlaceholder = document.getElementById('framePreviewPlaceholder');

        goBtn.disabled = true;
        goBtn.textContent = '生成中...';
        statusEl.style.display = '';
        statusEl.className = 'status-info';
        statusEl.innerHTML = '🔄 正在提交生成任务...';

        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[_frameGenShotIdx];
        if (!shot) { alert('分镜数据不存在'); return; }

        try {
            var res = await fetch('/api/v1/ai/generate-frame-image-with-assets', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ shotId: shot.id, frameIdx: _frameGenFrameIdx, providerId: parseInt(providerId), prompt: combinedPrompt })
            });
            var data = await res.json();

            if (!data || !data.promptId) {
                throw new Error(data.message || '提交任务失败');
            }

            _frameGenPromptId = data.promptId;
            _frameGenWorkflowType = data.workflowType || 'hidream-storyboard';
            _frameGenStartTime = Date.now();

            updateFrameGenStatus('pending');
            startFrameGenPolling();
        } catch (err) {
            statusEl.className = 'status-error';
            statusEl.innerHTML = '❌ 生成失败: ' + err.message;
            goBtn.disabled = false;
            goBtn.textContent = '重新生成';
        }
    }

    function updateFrameGenStatus(state) {
        var statusEl = document.getElementById('frameGenStatus');
        var goBtn = document.getElementById('frameGenGoBtn');
        var saveBtn = document.getElementById('frameGenSaveBtn');
        var previewGroup = document.getElementById('framePreviewGroup');
        var previewImg = document.getElementById('framePreviewImg');
        var previewPlaceholder = document.getElementById('framePreviewPlaceholder');

        if (state === 'pending') {
            var elapsed = Math.floor((Date.now() - _frameGenStartTime) / 1000);
            var min = Math.floor(elapsed / 60);
            var sec = elapsed % 60;
            statusEl.style.display = '';
            statusEl.className = 'status-info';
            statusEl.innerHTML = '⏳ 任务已提交，正在生成中... (' + min + '分' + sec + '秒)'
                + '<br><span style="font-size:11px;color:var(--text3);">promptId: ' + _frameGenPromptId + '</span>'
                + '<br><button class="btn btn-xs btn-ghost" style="margin-top:6px;" onclick="manualFetchFrameResult()">🔍 手动获取</button>';
        } else if (state === 'success') {
            statusEl.className = 'status-success';
            statusEl.innerHTML = '✅ 帧图片生成成功！';
            goBtn.style.display = 'none';
            saveBtn.style.display = '';
            previewGroup.style.display = '';
            previewImg.style.display = '';
            previewImg.src = _frameGenImageUrl;
            previewPlaceholder.style.display = 'none';
        } else if (state === 'error') {
            statusEl.className = 'status-error';
            statusEl.innerHTML = '❌ ' + (arguments[1] || '生成失败');
            goBtn.disabled = false;
            goBtn.textContent = '重新生成';
        } else if (state === 'preview') {
            previewImg.src = _frameGenImageUrl;
        }
    }

    function startFrameGenPolling() {
        if (_frameGenPoller != null) { clearTimeout(_frameGenPoller); _frameGenPoller = null; }

        var pollCount = 0;
        var maxPolls = 360;
        var pollInterval = 5000;

        function doPoll() {
            if (pollCount >= maxPolls) {
                updateFrameGenStatus('error', '生成超时（已等待30分钟）');
                return;
            }
            pollCount++;
            fetch('/api/v1/comfyui/result/' + _frameGenPromptId + '/image?workflowType=' + encodeURIComponent(_frameGenWorkflowType))
                .then(function(r){ return r.json(); })
                .then(function(data) {
                    if (data.success && data.data) {
                        var urls = data.data.imageUrls || [];
                        if (urls.length > 0) {
                            _frameGenImageUrl = urls[0];
                            updateFrameGenStatus('success');
                            return;
                        }
                    }
                    updateFrameGenStatus('pending');
                    _frameGenPoller = setTimeout(doPoll, pollInterval);
                })
                .catch(function() {
                    updateFrameGenStatus('pending');
                    _frameGenPoller = setTimeout(doPoll, pollInterval);
                });
        }
        doPoll();
    }

    function manualFetchFrameResult() {
        if (!_frameGenPromptId) { showToast('没有正在进行的任务', 'error'); return; }
        if (_frameGenPoller) { clearTimeout(_frameGenPoller); _frameGenPoller = null; }

        fetch('/api/v1/comfyui/result/' + _frameGenPromptId + '/image?workflowType=' + encodeURIComponent(_frameGenWorkflowType))
            .then(function(r){ return r.json(); })
            .then(function(data) {
                if (data.success && data.data) {
                    var urls = data.data.imageUrls || [];
                    if (urls.length > 0) {
                        _frameGenImageUrl = urls[0];
                        updateFrameGenStatus('success');
                        return;
                    }
                }
                updateFrameGenStatus('pending');
                showToast('还未生成完成，继续等待...', 'info');
                startFrameGenPolling();
            })
            .catch(function() {
                updateFrameGenStatus('pending');
                startFrameGenPolling();
            });
    }

    async function saveFrameGeneratedImage() {
        var imageUrl = _frameGenImageUrl;
        if (!imageUrl) { alert('没有可保存的图片'); return; }

        var saveBtn = document.getElementById('frameGenSaveBtn');
        saveBtn.disabled = true;
        saveBtn.textContent = '保存中...';

        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[_frameGenShotIdx];
        var targetFrame = _frameGenFrameIdx >= 0 ? shot.frames[_frameGenFrameIdx] : null;
        if (!targetFrame || !targetFrame.id) {
            alert('帧数据不存在');
            saveBtn.disabled = false;
            saveBtn.textContent = '保存到帧';
            return;
        }

        try {
            var res = await fetch('/api/v1/production/save-frame-image', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ frameId: targetFrame.id, imageUrl: imageUrl })
            });
            var data = await res.json();
            if (data.success) {
                showToast('帧图片已保存！', 'success');
                hideModal('frameProviderModal');
                loadShotsForChapter(window.currentChapterIdx, true).then(function() { renderShotsTab(); });
            } else {
                throw new Error(data.message || '保存失败');
            }
        } catch (err) {
            alert('保存失败: ' + err.message);
            saveBtn.disabled = false;
            saveBtn.textContent = '保存到帧';
        }
    }

    // Shot video generation
    function generateShotVideo(shotIdx) {
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[shotIdx];
        if (!shot) return;

        _shotVideoShotIdx = shotIdx;

        var firstFrame = shot.frames && shot.frames.length > 0 ? shot.frames[0] : null;
        var frameDescs = shot.frames ? shot.frames.map(function(f, i){
            var label = i === 0 ? '首帧' : i === shot.frames.length - 1 ? '末帧' : '中帧';
            return '<div style="padding:6px 10px;border-left:3px solid var(--primary);background:var(--surface2);border-radius:4px;margin-bottom:6px;"><strong style="font-size:12px;color:var(--text3);">[' + label + ']</strong><div style="font-size:13px;color:var(--text);margin-top:2px;">' + escapeHtml(f.description || '') + '</div></div>';
        }).join('') : '';

        var overlay = document.getElementById('shotVideoModal');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'modal-overlay';
            overlay.id = 'shotVideoModal';
            document.body.appendChild(overlay);
        }

        var firstFrameHtml = firstFrame && firstFrame.hasImage && firstFrame.imagePath
            ? '<div style="margin-bottom:12px;"><label style="font-size:13px;font-weight:600;color:var(--text);margin-bottom:6px;display:block;">首帧参考图</label><img src="' + firstFrame.imagePath + '" style="width:100%;max-height:200px;object-fit:contain;border-radius:8px;border:1px solid var(--border);background:#000;"></div>'
            : '<div style="padding:16px;text-align:center;background:var(--surface2);border-radius:8px;margin-bottom:12px;color:var(--text3);font-size:13px;">⚠️ 首帧未生成图片，视频将基于文字描述生成</div>';

        overlay.innerHTML = '<div class="modal" style="width:680px;">'
            + '<div class="modal-header"><h3>🎬 生成分镜视频</h3><button class="modal-close" onclick="hideModal(\'shotVideoModal\')">&times;</button></div>'
            + '<div class="modal-body" style="padding:16px;max-height:60vh;overflow-y:auto;">'
            + '<div class="form-group"><label>选择模型 *</label>'
            + '<select id="shotVideoProviderSelect" style="width:100%;padding:8px;border:1px solid var(--border);border-radius:6px;background:var(--surface2);color:var(--text);font-size:14px;"><option value="">加载中...</option></select></div>'
            + '<div class="form-group"><label>分镜描述</label><div style="font-size:13px;color:var(--text);padding:8px 10px;background:var(--surface2);border-radius:6px;line-height:1.5;">' + escapeHtml(shot.description || '') + '</div></div>'
            + '<div class="form-group"><label>帧描述（' + (shot.frames ? shot.frames.length : 0) + '帧）</label>' + frameDescs + '</div>'
            + firstFrameHtml
            + '<div id="shotVideoGenStatus" style="margin-top:12px;padding:12px;border-radius:8px;display:none;"></div>'
            + '</div>'
            + '<div class="modal-footer">'
            + '<button class="btn btn-ghost" onclick="hideModal(\'shotVideoModal\')">取消</button>'
            + '<button class="btn btn-primary" id="shotVideoGoBtn" onclick="doGenerateShotVideo()">生成视频</button>'
            + '</div></div>';

        showModal('shotVideoModal');

        fetch('/api/v1/providers/list')
            .then(function(r){ return r.json(); })
            .then(function(res){
                var list = (res.data || []).filter(function(p){ return p.capability === 5 || p.capability === 'ImageToVideo' || p.capability === 4 || p.capability === 'TextToVideo'; });
                var html = '';
                list.forEach(function(p) { html += '<option value="' + p.id + '">' + p.name + ' [' + (p.model || '') + ']</option>'; });
                document.getElementById('shotVideoProviderSelect').innerHTML = html || '<option value="">未找到视频生成提供者</option>';
            })
            .catch(function(){
                document.getElementById('shotVideoProviderSelect').innerHTML = '<option value="">加载失败</option>';
            });
    }

    var _shotVideoShotIdx = -1;
    var _shotVideoPromptId = null;
    var _shotVideoTimer = null;
    var _shotVideoStartTime = null;

    function doGenerateShotVideo() {
        var providerId = document.getElementById('shotVideoProviderSelect').value;
        if (!providerId) { alert('请选择提供者'); return; }

        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[_shotVideoShotIdx];
        if (!shot) return;

        var firstFrame = shot.frames && shot.frames.length > 0 ? shot.frames[0] : null;
        var imagePath = firstFrame && firstFrame.hasImage && firstFrame.imagePath ? firstFrame.imagePath : null;
        var userMessage = shot.frames ? shot.frames.map(function(f){ return f.description || ''; }).join('\n') : '';

        var goBtn = document.getElementById('shotVideoGoBtn');
        var statusEl = document.getElementById('shotVideoGenStatus');
        goBtn.disabled = true;
        goBtn.textContent = '提交中...';
        statusEl.style.display = '';
        statusEl.className = 'status-info';
        statusEl.innerHTML = '🔄 正在提交生成任务...';

        fetch('/api/v1/production/generate-video', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ prompt: userMessage, imagePath: imagePath, providerId: parseInt(providerId) })
        })
            .then(function(r){ return r.json(); })
            .then(function(res) {
                if (!res.success) {
                    goBtn.disabled = false;
                    goBtn.textContent = '生成视频';
                    statusEl.className = 'status-error';
                    statusEl.innerHTML = '❌ 提交失败: ' + (res.message || '未知错误');
                    return;
                }
                if (res.isComfyui && res.promptId) {
                    _shotVideoPromptId = res.promptId;
                    _shotVideoStartTime = Date.now();
                    goBtn.textContent = '生成中...';
                    startShotVideoPolling();
                } else if (res.data) {
                    window.shotState[window.currentChapterIdx].shots[_shotVideoShotIdx].videoUrl = res.data;
                    window.shotState[window.currentChapterIdx].shots[_shotVideoShotIdx].generatingVideo = false;
                    hideModal('shotVideoModal');
                    renderShotsTab();
                    showToast('视频生成完成！', 'success');
                }
            })
            .catch(function(err) {
                goBtn.disabled = false;
                goBtn.textContent = '生成视频';
                statusEl.className = 'status-error';
                statusEl.innerHTML = '❌ 请求失败: ' + err.message;
            });
    }

    function resetShotVideoGoBtn() {
        var goBtn = document.getElementById('shotVideoGoBtn');
        if (goBtn) { goBtn.disabled = false; goBtn.textContent = '生成视频'; }
    }

    function showShotVideoError(msg) {
        resetShotVideoGoBtn();
        var statusEl = document.getElementById('shotVideoGenStatus');
        var elapsed = _shotVideoStartTime ? Math.floor((Date.now() - _shotVideoStartTime) / 1000) : 0;
        var min = Math.floor(elapsed / 60);
        var sec = elapsed % 60;
        statusEl.className = 'status-error';
        statusEl.innerHTML = '❌ ' + msg
            + (_shotVideoPromptId ? '<br><span style="font-size:11px;color:var(--text3);">promptId: ' + _shotVideoPromptId + ' | 已等待 ' + min + '分' + sec + '秒</span>' : '')
            + (_shotVideoPromptId ? '<br><button class="btn btn-xs btn-ghost" style="margin-top:6px;" onclick="manualFetchShotVideoResult()">🔍 手动获取</button>' : '')
            + '<br><button class="btn btn-xs btn-ghost" style="margin-top:6px;" onclick="hideModal(\'shotVideoModal\');generateShotVideo(' + _shotVideoShotIdx + ')">🔄 重新提交</button>';
    }

    function startShotVideoPolling() {
        var statusEl = document.getElementById('shotVideoGenStatus');

        function updateStatus(msg) {
            var elapsed = Math.floor((Date.now() - _shotVideoStartTime) / 1000);
            var min = Math.floor(elapsed / 60);
            var sec = elapsed % 60;
            statusEl.className = 'status-info';
            statusEl.innerHTML = msg
                + '<br><span style="font-size:11px;color:var(--text3);">promptId: ' + _shotVideoPromptId + ' | 已等待 ' + min + '分' + sec + '秒</span>'
                + '<br><button class="btn btn-xs btn-ghost" style="margin-top:6px;" onclick="manualFetchShotVideoResult()">🔍 手动获取</button>';
        }

        var pollCount = 0;
        var maxPolls = 120;
        var interval = 5000;

        updateStatus('⏳ 任务已提交，正在生成中...');

        _shotVideoTimer = setInterval(function() {
            pollCount++;
            fetch('/api/v1/comfyui/result/' + _shotVideoPromptId + '/video?workflowType=' + encodeURIComponent('ltx-image-to-video'))
                .then(function(r){ return r.json(); })
                .then(function(res) {
                    if (res.success && res.data) {
                        var urls = res.data.videoUrls || [];
                        if (urls.length > 0) {
                            clearInterval(_shotVideoTimer);
                            window.shotState[window.currentChapterIdx].shots[_shotVideoShotIdx].videoUrl = urls[0];
                            window.shotState[window.currentChapterIdx].shots[_shotVideoShotIdx].generatingVideo = false;
                            hideModal('shotVideoModal');
                            renderShotsTab();
                            showToast('视频生成完成！', 'success');
                            return;
                        }
                    }
                    if (pollCount >= maxPolls) {
                        clearInterval(_shotVideoTimer);
                        showShotVideoError('生成超时');
                        return;
                    }
                    if (res.success === false && res.pending === false) {
                        clearInterval(_shotVideoTimer);
                        showShotVideoError(res.message || '生成失败');
                        return;
                    }
                    updateStatus('⏳ 任务已提交，正在生成中...');
                })
                .catch(function() {
                    if (pollCount >= maxPolls) {
                        clearInterval(_shotVideoTimer);
                        showShotVideoError('请求超时');
                    }
                });
        }, interval);
    }

    function manualFetchShotVideoResult() {
        if (!_shotVideoPromptId) { return; }

        var statusEl = document.getElementById('shotVideoGenStatus');
        statusEl.innerHTML = '🔄 手动获取中...';

        fetch('/api/v1/comfyui/result/' + _shotVideoPromptId + '/video?workflowType=' + encodeURIComponent('ltx-image-to-video'))
            .then(function(r){ return r.json(); })
            .then(function(res) {
                if (res.success && res.data) {
                    var urls = res.data.videoUrls || [];
                    if (urls.length > 0) {
                        window.shotState[window.currentChapterIdx].shots[_shotVideoShotIdx].videoUrl = urls[0];
                        window.shotState[window.currentChapterIdx].shots[_shotVideoShotIdx].generatingVideo = false;
                        if (_shotVideoTimer) { clearInterval(_shotVideoTimer); _shotVideoTimer = null; }
                        hideModal('shotVideoModal');
                        renderShotsTab();
                        showToast('视频生成完成！', 'success');
                        return;
                    }
                }
                if (res.success === false && res.pending === false) {
                    if (_shotVideoTimer) { clearInterval(_shotVideoTimer); _shotVideoTimer = null; }
                    showShotVideoError(res.message || '获取失败');
                } else {
                    statusEl.className = 'status-info';
                    statusEl.innerHTML = '⏳ 还未生成完成，继续等待...'
                        + '<br><span style="font-size:11px;color:var(--text3);">promptId: ' + _shotVideoPromptId + '</span>'
                        + '<br><button class="btn btn-xs btn-ghost" style="margin-top:6px;" onclick="manualFetchShotVideoResult()">🔍 手动获取</button>';
                    if (!_shotVideoTimer) startShotVideoPolling();
                }
            })
            .catch(function() {
                showShotVideoError('请求失败');
            });
    }

    function saveShotVideo(shotIdx) {
        var state = window.shotState[window.currentChapterIdx];
        var shot = state && state.shots[shotIdx];
        if (!shot || !shot.videoUrl) { showToast('没有可保存的视频', 'error'); return; }
        var shotId = shot.id;
        if (!shotId) { showToast('分镜ID不存在', 'error'); return; }

        fetch('/api/v1/production/save-shot-video', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ shotId: shotId, videoUrl: shot.videoUrl })
        })
        .then(function(r){ return r.json(); })
        .then(function(res) {
            if (res.success) {
                showToast('视频已保存到分镜！', 'success');
                var idx = window.currentChapterIdx;
                window.loadShotsForChapter(idx, true).then(function() { renderShotsTab(); });
            } else {
                showToast('保存失败: ' + (res.message || ''), 'error');
            }
        })
        .catch(function(err) {
            showToast('保存失败: ' + err.message, 'error');
        });
    }

    // Storyboard generation
    function generateStoryboardShot(shotIdx) {
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[shotIdx];
        if (!shot) return;

        showToast('正在生成分镜图...', 'info');
        window.shotState[window.currentChapterIdx].shots[shotIdx].generatingStoryboard = true;
        renderShotsTab();

        var description = '';
        if (shot.frames && shot.frames.length > 0) {
            description = shot.frames.map(function(f){ return f.description || ''; }).join('; ');
        }
        if (!description) description = shot.shotName || shot.shotNumber || '';

        fetch('/api/comfyui/hidream/storyboard', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ prompt: description, width: 1024, height: 576 })
        })
            .then(function(r){ return r.json(); })
            .then(function(res) {
                if (res && res.promptId) {
                    var promptId = res.promptId;
                    pollAiResultForShot(promptId, 'hidream-storyboard', shotIdx, 'storyboard');
                } else {
                    window.shotState[window.currentChapterIdx].shots[shotIdx].generatingStoryboard = false;
                    renderShotsTab();
                    alert('分镜图生成失败: ' + (res && res.message ? res.message : '未知错误'));
                }
            })
            .catch(function(err) {
                window.shotState[window.currentChapterIdx].shots[shotIdx].generatingStoryboard = false;
                renderShotsTab();
                alert('请求失败: ' + err.message);
            });
    }

    // Frame templates
    function showFrameTemplates() {
        if (window.currentChapterIdx === -1) return;
        var ch = window.chapters[window.currentChapterIdx];
        var h = '<div style="margin-bottom:12px;"><strong>章节内容：</strong></div>'
            + '<div style="background:var(--bg);padding:12px;border-radius:6px;margin-bottom:16px;max-height:150px;overflow-y:auto;">'
            + (ch.content || '暂无内容') + '</div>';
        h += '<div style="margin-bottom:8px;"><strong>默认分帧模板：</strong></div>';
        window._frameTemplates.forEach(function(t, i) {
            h += '<div style="padding:8px 12px;background:var(--bg);border-radius:6px;margin-bottom:4px;font-size:13px;">'
                + '<div>模板 ' + (i+1) + '</div>'
                + '<div style="color:var(--text2);font-size:12px;margin-top:4px;">首帧：' + t.first + '</div>'
                + '<div style="color:var(--text2);font-size:12px;">中帧：' + t.middle + '</div>'
                + '<div style="color:var(--text2);font-size:12px;">末帧：' + t.last + '</div>'
                + '</div>';
        });
        var frameListBody = document.getElementById('frameListBody');
        if (frameListBody) frameListBody.innerHTML = h;
        var frameListTitle = document.getElementById('frameListTitle');
        if (frameListTitle) frameListTitle.textContent = '分帧模板';
        showModal('frameListModal');
    }

    // Shot regeneration - show extraction modal
    function regenerateShots() {
        showShotAssetExtractionModal();
    }

    function generateDemoShots(chapter) {
        var shots = [];
        var t = window._frameTemplates[Math.floor(Math.random() * window._frameTemplates.length)];
        for (var i = 0; i < 3; i++) {
            shots.push({
                shotNumber: 'SH' + String(i+1).padStart(3,'0'),
                shotName: '分镜 ' + (i+1),
                shotSize: ['全景','中景','特写'][i % 3],
                cameraMovement: ['固定','前推','平移'][i % 3],
                duration: 5 + Math.floor(Math.random() * 5),
                frames: [
                    { frameType: 'First', description: t.first, startTime: 0, duration: 2, order: 0, hasImage: false },
                    { frameType: 'Middle', description: t.middle, startTime: 2, duration: 2.5, order: 1, hasImage: false },
                    { frameType: 'Last', description: t.last, startTime: 4.5, duration: 3.5, order: 2, hasImage: false }
                ],
                hasFirstFrame: false, hasVideo: false
            });
        }
        window.shotState[window.currentChapterIdx] = { shots: shots };
        render();
    }

    // Utility functions
    function showImagePreview(src, assetIdOrShotIdx, nameOrFrameIdx, assetType, description) {
        if (!src) return;
        var overlay = document.createElement('div');
        overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.85);z-index:10000;display:flex;align-items:center;justify-content:center;';
        overlay.onclick = function(e) { if (e.target === overlay) overlay.remove(); };
        var img = document.createElement('img');
        img.src = src;
        img.style.cssText = 'max-width:90%;max-height:85%;object-fit:contain;border-radius:8px;box-shadow:0 8px 40px rgba(0,0,0,0.5);';
        overlay.appendChild(img);

        var btn = document.createElement('button');
        btn.style.cssText = 'position:fixed;bottom:40px;left:50%;transform:translateX(-50%);padding:10px 24px;background:var(--primary);color:white;border:none;border-radius:8px;font-size:14px;font-weight:600;cursor:pointer;z-index:10001;box-shadow:0 4px 16px rgba(0,0,0,0.3);';
        if (assetIdOrShotIdx !== undefined && typeof assetIdOrShotIdx === 'number') {
            var frameIdx = nameOrFrameIdx;
            btn.textContent = '🔄 重新生成';
            btn.onclick = function() { overlay.remove(); showFrameImageProviderDialog(assetIdOrShotIdx, frameIdx); };
        } else if (assetIdOrShotIdx) {
            btn.textContent = '🔄 重新生成';
            btn.onclick = function() { overlay.remove(); showSingleAssetGenModalFromFrame(assetIdOrShotIdx, nameOrFrameIdx || '', assetType || '', description || ''); };
        } else {
            btn.textContent = '✕ 关闭';
            btn.onclick = function() { overlay.remove(); };
        }
        overlay.appendChild(btn);

        document.body.appendChild(overlay);
    }

    function formatMarkdown(text) {
        if (!text) return '';
        return text
            .replace(/^### (.*$)/gim, '<h3>$1</h3>')
            .replace(/^## (.*$)/gim, '<h2>$1</h2>')
            .replace(/^# (.*$)/gim, '<h1>$1</h1>')
            .replace(/\*\*(.*)\*\*/gim, '<strong>$1</strong>')
            .replace(/\*(.*)\*/gim, '<em>$1</em>')
            .replace(/\n/gim, '<br>');
    }

    function showModal(id) {
        var el = document.getElementById(id);
        if (el) el.classList.add('show');
    }

    function hideModal(id) {
        var el = document.getElementById(id);
        if (el) el.classList.remove('show');
    }

    function showToast(msg, type) {
        var toast = document.createElement('div');
        var color = type === 'success' ? 'var(--ok)' : type === 'error' ? 'var(--danger)' : 'var(--info)';
        toast.style.cssText = 'position:fixed;top:20px;right:20px;padding:12px 20px;background:' + color + ';color:#fff;border-radius:8px;z-index:9999;font-size:13px;font-weight:600;animation:fadeIn .3s;';
        toast.textContent = msg;
        document.body.appendChild(toast);
        setTimeout(function(){ toast.remove(); }, 3000);
    }

    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Expose globally
    window.createPoller = createPoller;
    window.pollAiResultForShot = pollAiResultForShot;
    window.showFrameImageProviderDialog = showFrameImageProviderDialog;
    window.doGenerateFrameImage = doGenerateFrameImage;
    window.generateShotVideo = generateShotVideo;
    window.generateStoryboardShot = generateStoryboardShot;
    window.showFrameTemplates = showFrameTemplates;
    window.regenerateShots = regenerateShots;
    window.generateDemoShots = generateDemoShots;
    window.showImagePreview = showImagePreview;
    window.formatMarkdown = formatMarkdown;
    window.showToast = showToast;
    window.showModal = showModal;
    window.hideModal = hideModal;
    window.saveFrameGeneratedImage = saveFrameGeneratedImage;
    window.manualFetchFrameResult = manualFetchFrameResult;
    window.doGenerateShotVideo = doGenerateShotVideo;
    window.escapeHtml = escapeHtml;
    window._frameTemplates = window._frameTemplates || [
        { first: '晨曦微露，薄雾笼罩的村落全景', middle: '镜头缓缓扫过村庄，石板路上有早起的居民开始劳作', last: '视线逐渐拉远，远景渐入云海' },
        { first: '主角从村口大步走来，身披斗篷腰佩长剑', middle: '中景跟拍主角穿过市集，周围村民纷纷侧目低语', last: '主角在镜头前停步，缓缓转身' },
        { first: '面部大特写——主角瞳孔微颤，额头渗出细密汗珠', middle: '镜头缓慢前推至眼部特写', last: '特写维持在眉眼之间，一滴泪珠从眼角滑落' },
        { first: '过肩镜头——主角肩部占据画面右下角', middle: '正反打切换至精灵少女近景', last: '镜头回到过肩视角，精灵少女收起长弓' },
        { first: '一枚古朴的金色宝石悬浮于祭坛之上', middle: '镜头围绕宝石缓缓旋转，符文光纹逐渐变亮', last: '一只手从画面右侧缓缓伸入' },
        { first: '全景俯拍——角色在古老的森林中穿行', middle: '镜头平移跟拍角色在森林中的奔跑', last: '角色冲出树林到达悬崖边缘，豁然开朗' },
        { first: '超大远景——古战场遗迹横亘在荒原之上', middle: '镜头缓缓拉远，荒原上星罗棋布的篝火逐渐化为光点', last: '画面升向高空穿破云层' }
    ];
})();