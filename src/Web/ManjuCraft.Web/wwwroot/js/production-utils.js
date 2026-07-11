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

    // Frame generation
    function generateFrameImage(shotIdx, frameIdx) {
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[shotIdx];
        if (!shot) return;

        var targetFrame = frameIdx >= 0 ? shot.frames[frameIdx] : null;
        var prompt = targetFrame ? targetFrame.description : (shot.frames && shot.frames[0] ? shot.frames[0].description : '');
        if (!prompt) { alert('无法获取提示词'); return; }

        showToast('正在生成帧图片...', 'info');
        window.shotState[window.currentChapterIdx].shots[shotIdx].generatingFrame = true;
        renderShotsTab();

        fetch('/api/comfyui/zimage/text-to-image', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ prompt: prompt, width: 1024, height: 576 })
        })
            .then(function(r){ return r.json(); })
            .then(function(res) {
                if (res && res.promptId) {
                    var promptId = res.promptId;
                    pollAiResultForShot(promptId, 'zimage-text-to-image', shotIdx, 'frame', frameIdx);
                } else {
                    window.shotState[window.currentChapterIdx].shots[shotIdx].generatingFrame = false;
                    renderShotsTab();
                    alert('图片生成失败: ' + (res && res.message ? res.message : '未知错误'));
                }
            })
            .catch(function(err) {
                window.shotState[window.currentChapterIdx].shots[shotIdx].generatingFrame = false;
                renderShotsTab();
                alert('请求失败: ' + err.message);
            });
    }

    // Shot video generation
    function generateShotVideo(shotIdx) {
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[shotIdx];
        if (!shot) return;

        showToast('正在生成分镜视频...', 'info');
        window.shotState[window.currentChapterIdx].shots[shotIdx].generatingVideo = true;
        renderShotsTab();

        var systemPrompt = '你是一个视频生成助手。';
        var userMessage = shot.frames ? shot.frames.map(function(f){ return f.description; }).join('\n') : '';

        fetch('/api/comfyui/ltx/text-to-video', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ prompt: userMessage })
        })
            .then(function(r){ return r.json(); })
            .then(function(res) {
                if (res && res.promptId) {
                    var promptId = res.promptId;
                    pollAiResultForShot(promptId, 'ltx-text-to-video', shotIdx, 'video');
                } else {
                    window.shotState[window.currentChapterIdx].shots[shotIdx].generatingVideo = false;
                    renderShotsTab();
                    alert('视频生成失败: ' + (res && res.message ? res.message : '未知错误'));
                }
            })
            .catch(function(err) {
                window.shotState[window.currentChapterIdx].shots[shotIdx].generatingVideo = false;
                renderShotsTab();
                alert('请求失败: ' + err.message);
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
    function showImagePreview(src) {
        if (!src) return;
        window.open(src, '_blank');
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

    function showToast(msg, type) {
        var toast = document.createElement('div');
        var color = type === 'success' ? 'var(--ok)' : type === 'error' ? 'var(--danger)' : 'var(--info)';
        toast.style.cssText = 'position:fixed;top:20px;right:20px;padding:12px 20px;background:' + color + ';color:#fff;border-radius:8px;z-index:9999;font-size:13px;font-weight:600;animation:fadeIn .3s;';
        toast.textContent = msg;
        document.body.appendChild(toast);
        setTimeout(function(){ toast.remove(); }, 3000);
    }

    function showModal(id) {
        var el = document.getElementById(id);
        if (el) el.style.display = 'flex';
    }

    function hideModal(id) {
        var el = document.getElementById(id);
        if (el) el.style.display = 'none';
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
    window.generateFrameImage = generateFrameImage;
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