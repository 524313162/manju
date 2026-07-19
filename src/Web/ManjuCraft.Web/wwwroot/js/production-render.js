// Production Render - Shot rendering functions (compact, video-friendly layout)
(function() {

// ============ 核心渲染：Shots Tab ============
    function renderShotsTab() {
        const area = document.getElementById('storyboardArea');
        if (!area) return;

        if (window.chapters.length === 0 || window.currentChapterIdx === -1) {
            area.innerHTML = `
                <div class="no-content" style="text-align:center;padding:60px 20px;color:var(--text3);">
                    <div class="icon" style="font-size:40px;margin-bottom:12px;">📖</div>
                    <p>请先编辑剧本添加章节</p>
                    <p style="font-size:12px;color:var(--text3);margin-top:8px;">
                        <a href="/Story?projectId=${window.projectId}" style="color:var(--info);text-decoration:none;">📖 去编辑剧本</a>
                    </p>
                </div>`;
            return;
        }

        const ch = window.chapters[window.currentChapterIdx];
        if (!window.shotState[window.currentChapterIdx]) window.shotState[window.currentChapterIdx] = { shots: [], loading: false };

        const state = window.shotState[window.currentChapterIdx];

        if (state.loading) {
            const msg = state.loadingFromServer ? '正在加载分镜数据...' : '正在调用 AI 生成分镜...';
            area.innerHTML = `
                <div style="text-align:center;padding:20px;">
                    <div class="spinner" style="display:inline-block;width:24px;height:24px;border:2px solid var(--border);border-top-color:var(--info);border-radius:50%;animation:spin .6s linear infinite;"></div>
                    <p style="margin-top:10px;color:var(--text2);font-size:13px;">${msg}</p>
                </div>`;
            return;
        }

        if (state.shots && state.shots.length > 0) {
            const videoWidth = 444; // 250 * 16/9

            let h = `
                <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:12px;gap:12px;flex-wrap:wrap;">
                    <div style="display:flex;align-items:center;gap:8px;">
                        <strong>${ch.chapterName}</strong>
                    </div>
                    <div style="display:flex;gap:8px;">
                        <button class="btn btn-primary btn-sm" onclick="regenerateShots()">🔄 重新提取分镜</button>
                        <button class="btn btn-ghost btn-sm" onclick="showFrameTemplates()">📋 分帧模板</button>
                    </div>
                </div>`;

            h += '<div style="display:flex;flex-direction:column;gap:10px;">';

            state.shots.forEach((shot, si) => {
                const frames = shot.frames || [];
                const shotDuration = shot.duration || 0;
                const totalFrameDuration = frames.reduce((sum, f) => sum + (f.duration || 0), 0);
                const shotHasVideo = shot.hasVideo && shot.videoUrl;
                const isGeneratingVideo = shot.generatingVideo;

                h += `<div class="shot-item" id="shot-${si}" style="background:var(--surface);border:1px solid var(--border);border-radius:10px;overflow:hidden;">`;

                // Header
                h += `
                    <div style="display:flex;justify-content:space-between;align-items:center;padding:10px 12px;background:var(--bg);border-bottom:1px solid var(--border);flex-wrap:wrap;gap:8px;">
                        <div style="display:flex;align-items:center;gap:8px;flex-wrap:wrap;">
                            <span class="shot-badge" style="font-size:11px;font-weight:600;padding:2px 8px;background:var(--primary);color:white;border-radius:4px;">SHOT ${si+1}</span>
                            ${shot.shotName ? `<span style="font-size:13px;font-weight:500;color:var(--text);">${shot.shotName}</span>` : ''}
                        </div>
                        <div style="display:flex;align-items:center;gap:10px;font-size:11px;color:var(--text3);">
                            ${shot.shotSize ? `<span>📐 ${shot.shotSize}</span>` : ''}
                            ${shot.cameraMovement ? `<span>🎥 ${shot.cameraMovement}</span>` : ''}
                            <span>⏱️ ${shotDuration}s</span>
                            ${totalFrameDuration > 0 ? `<span>帧累计: ${totalFrameDuration.toFixed(1)}s</span>` : ''}
                            ${shotHasVideo ? '<span style="color:var(--ok);font-weight:500;">▶ 视频就绪</span>' : (isGeneratingVideo ? '<span style="color:var(--info);font-weight:500;"><span class="spinner-inline"></span> 生成中...</span>' : '')}
                        </div>
                        <button class="shot-toggle" onclick="toggleShotFrames(${si})" style="width:24px;height:24px;border-radius:50%;background:var(--bg);border:1px solid var(--border);color:var(--text3);font-size:12px;cursor:pointer;display:flex;align-items:center;justify-content:center;line-height:1;transition:all .15s;flex-shrink:0;" title="展开/收起帧" onmouseover="this.style.background='var(--primary)';this.style.color='white';this.style.borderColor='var(--primary)'" onmouseout="this.style.background='var(--bg)';this.style.color='var(--text3)';this.style.borderColor='var(--border)'">▼</button>
                    </div>`;

                // Collapsible content wrapper (includes action bar, description, frames, divider, assets)
                h += `
                    <div id="shot-content-${si}">`;

                // Shot Description (if exists) - inside collapsible wrapper
                if (shot.description) {
                    const shotDesc = shot.description;
                    const isLongShotDesc = shotDesc.length > 200;
                    h += `
                        <div style="padding:8px 12px;background:var(--surface);border-bottom:1px solid var(--border);">
                            <div class="shot-desc" data-full="${shotDesc.replace(/"/g, '"')}" data-collapsed="${isLongShotDesc ? 'true' : 'false'}" style="font-size:12px;color:var(--text2);line-height:1.6;${isLongShotDesc ? 'display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;' : ''}">${shotDesc}</div>
                            ${isLongShotDesc ? `<button class="shot-desc-toggle" onclick="toggleShotDesc('shot-${si}')" style="margin-top:4px;width:24px;height:24px;border-radius:50%;background:var(--bg);border:1px solid var(--border);color:var(--text3);font-size:11px;cursor:pointer;display:flex;align-items:center;justify-content:center;line-height:1;transition:all .15s;" title="展开/收起" onmouseover="this.style.background='var(--primary)';this.style.color='white';this.style.borderColor='var(--primary)'" onmouseout="this.style.background='var(--bg)';this.style.color='var(--text3)';this.style.borderColor='var(--border)'">▼</button>` : ''}
                        </div>`;
                }

                // Action bar
                h += `
                        <div style="display:flex;gap:6px;padding:8px 12px;flex-wrap:wrap;align-items:center;">
                            ${shotHasVideo || isGeneratingVideo
                                ? `<button class="btn btn-primary btn-xs" onclick="playShotVideo(${si})" style="font-size:11px;">▶ 打开视频</button>`
                                : `<button class="btn btn-primary btn-xs" onclick="generateShotVideo(${si})" ${isGeneratingVideo ? 'disabled' : ''} style="font-size:11px;">🎬 生成视频</button>`
                            }
                            <button class="btn btn-ghost btn-xs" onclick="showShotFrameAssetBindModal(${si})" style="font-size:11px;">🏷️ 绑定分镜帧资产</button>
                            <button class="btn btn-ghost btn-xs" onclick="showShotFrameAssetExtractModal(${si})" style="font-size:11px;">🔍 提取分镜资产</button>
                        </div>

                        <!-- Generation status -->
                        ${shot.generatingFrame ? `<div style="padding:6px 12px;background:var(--bg);color:var(--text2);font-size:11px;"><span class="spinner-inline"></span> 生成帧中...</div>` : ''}

                        <!-- Frames + Video area -->
                        <div id="shot-frames-${si}" style="display:flex;gap:12px;align-items:flex-start;padding:12px;">
                            <!-- LEFT: Frames vertical scroll -->
                            <div style="flex:1;min-width:0;max-height:300px;overflow-y:auto;padding:4px 0;display:flex;flex-direction:column;gap:8px;">
                                ${frames.map((f, fi) => renderFrameCard(f, si, fi, fi === 0, shot.assets)).join('')}
                            </div>

                            <!-- RIGHT: Video fixed width 444px (16:9 at 250px height) -->
                            <div style="flex:0 0 ${videoWidth}px;max-width:${videoWidth}px;display:flex;flex-direction:column;">
                                ${renderVideoArea(shot, si, shotHasVideo, isGeneratingVideo)}
                            </div>
                        </div>
                    </div>
                </div>`;
            });

            h += '</div>';
            area.innerHTML = h;
        } else {
            area.innerHTML = `
                <div style="text-align:center;padding:40px 20px;color:var(--text3);">
                    <div class="icon" style="font-size:40px;margin-bottom:12px;">🎬</div>
                    <p>该章节暂无分镜数据</p>
                    <p style="font-size:12px;color:var(--text3);margin-top:8px;">
                        <button class="btn btn-primary btn-sm" onclick="showShotAssetExtractionModal()">🔄 提取分镜和资产</button>
                    </p>
                </div>`;
        }
    }

// ============ Frame Assets (one row per asset below each frame) ============
    function renderFrameAssets(assets, shotIdx) {
        if (!assets || assets.length === 0) return '';

        const typeIcons = { 'Actor': '👤', 'Scene': '🏞️', 'Bgm': '🎵', 'Prop': '📦', 'VoiceVoice': '🎤' };
        const typeNames = { 'Actor': '角色', 'Scene': '场景', 'Bgm': 'BGM', 'Prop': '道具', 'VoiceVoice': '音色' };

        function getTypeLabel(t) {
            return typeNames[t] || (t === 1 ? '角色' : t === 3 ? '场景' : t === 4 ? 'BGM' : t === 5 ? '道具' : t === 2 ? '音色' : t) || '未知';
        }

        function getTypeIcon(t) {
            return typeIcons[t] || (t === 1 ? '👤' : t === 3 ? '🏞️' : t === 4 ? '🎵' : t === 5 ? '📦' : t === 2 ? '🎤' : '📄');
        }

        return '<div style="margin-top:8px;padding:6px 12px 8px;border-top:1px dashed var(--border);display:flex;flex-direction:column;gap:2px;">'
            + assets.map(function(asset) {
                var typeName = getTypeLabel(asset.assetType);
                var icon = getTypeIcon(asset.assetType);
                var name = asset.name || asset.Name || '';
                var description = asset.description || '';
                var assetId = asset.id || asset.Id || '';
                var imgUrl = asset.resourceFilePath || asset.ResourceFilePath || '';
                var typeStr = typeof asset.assetType === 'number' ? asset.assetType.toString() : (asset.assetType || '');
                var safeName = name.replace(/'/g, "\\'");
                var safeType = typeStr.replace(/'/g, "\\'");
                var safeDesc = description.replace(/'/g, "\\'");
                var imgHtml = imgUrl
                    ? '<img src="' + imgUrl + '" style="width:40px;height:40px;object-fit:cover;border-radius:4px;cursor:pointer;flex-shrink:0;" onclick="showImagePreview(\'' + imgUrl + '\',\'' + assetId + '\',\'' + safeName + '\',\'' + safeType + '\',\'' + safeDesc + '\');event.stopPropagation();" title="点击放大">'
                    : '<span style="font-size:24px;cursor:pointer;flex-shrink:0;" onclick="showSingleAssetGenModalFromFrame(\'' + assetId + '\',\'' + safeName + '\',\'' + safeType + '\',\'' + safeDesc + '\')" title="点击生成资产图片">' + icon + '</span>';
                return '<div style="display:flex;align-items:center;gap:8px;padding:4px 0;border-bottom:1px solid var(--border);">'
                    + imgHtml
                    + '<div style="flex:1;min-width:0;display:flex;flex-direction:column;gap:1px;">'
                    + '<div style="display:flex;align-items:center;gap:6px;">'
                    + '<span style="font-weight:500;font-size:13px;">' + escapeHtml(name) + '</span>'
                    + '<span style="color:var(--text3);font-size:11px;white-space:nowrap;">[' + typeName + ']</span>'
                    + '</div>'
                    + (description ? '<span style="font-size:11px;color:var(--text2);line-height:1.4;">' + escapeHtml(description) + '</span>' : '')
                    + '</div>'
                    + '<button class="btn btn-xs btn-ghost" onclick="removeFrameAssetFromShot(' + shotIdx + ', \'' + assetId + '\')" style="color:var(--danger);font-size:14px;padding:2px 6px;flex-shrink:0;" title="移除资产">✕</button>'
                    + '</div>';
            }).join('')
            + '</div>';
    }

    function removeFrameAssetFromShot(shotIdx, assetId) {
        if (!assetId) { showToast('资产ID为空', 'error'); return; }
        if (!confirm('确认从该分镜所有帧中移除此资产？')) return;
        var state = window.shotState[window.currentChapterIdx];
        var shot = state.shots[shotIdx];
        if (!shot || !shot.id) return;

        fetch('/api/v1/assets/remove-frame-asset', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ shotId: shot.id, assetId: assetId })
        })
        .then(function(r) { return r.json(); })
        .then(function(res) {
            if (res.success) {
                showToast('已移除', 'success');
                var idx = window.currentChapterIdx;
                window.loadShotsForChapter(idx, true).then(function() {
                    renderShotsTab();
                });
            } else {
                showToast('移除失败：' + (res.message || ''), 'error');
            }
        });
    }

    // ============ Video Area ============
    function renderVideoArea(shot, shotIdx, hasVideo, isGenerating) {
        return `
            <div style="flex:1;display:flex;flex-direction:column;min-width:0;">
                <div style="position:relative;background:#000;border-radius:8px;overflow:hidden;aspect-ratio:16/9;max-height:220px;width:100%;">
                    ${hasVideo && shot.videoUrl
                        ? `<video src="${shot.videoUrl}" controls style="width:100%;height:100%;object-fit:contain;background:#000;"></video>
                           <div style="position:absolute;top:8px;right:8px;display:flex;gap:6px;flex-wrap:wrap;justify-content:flex-end;">
                                <button class="btn btn-ghost btn-xs" onclick="generateShotVideo(${shotIdx})" style="background:rgba(0,0,0,0.7);color:#fff;border:none;">🔄 重新生成</button>
                           </div>`
                        : isGenerating
                            ? `<div style="width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;color:var(--text3);">
                                   <span class="spinner" style="width:32px;height:32px;"></span>
                                   <div style="font-size:13px;font-weight:500;">视频生成中...</div>
                                   <div style="font-size:11px;color:var(--text3);">请耐心等待，通常需要 1-3 分钟</div>
                               </div>`
                            : `<div style="width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;color:var(--text3);cursor:pointer;" onclick="generateShotVideo(${shotIdx})" title="点击生成视频">
                                   <span style="font-size:40px;">🎬</span>
                                   <div style="font-size:14px;font-weight:600;">点击生成视频</div>
                                   <div style="font-size:11px;color:var(--text3);">基于所有帧描述生成分镜视频</div>
                               </div>`
                    }
                </div>
            </div>`;
    }

    // ============ Frame Card (Vertical Row Layout) ============
    function renderFrameCard(frame, shotIdx, frameIdx, isFirstFrame, shotAssets) {
        if (!frame) return '';

        const frameType = frame.frameType || (frameIdx === 0 ? 'First' : 'Last');
        const typeLabel = frameType === 'First' ? '首帧' : frameType === 'Last' ? '末帧' : '中帧';
        const typeColor = frameType === 'First' ? 'var(--info)' : frameType === 'Last' ? 'var(--danger)' : 'var(--ok)';
        const hasImage = frame.hasImage && frame.imagePath;
        const isGenerating = frame.generating;
        const narrativeDesc = frame.narrativeDescription || frame.description || '暂无描述';
        const shotSize = frame.shotSize || '';
        const frameId = `frame-${shotIdx}-${frameIdx}`;

        const imageHtml = hasImage
            ? `<div style="width:100%;height:100%;position:relative;">
                 <img src="${frame.imagePath}" style="width:100%;height:100%;object-fit:cover;cursor:zoom-in;" onclick="showImagePreview('${frame.imagePath}', ${shotIdx}, ${frameIdx})" title="点击放大">
                 <button onclick="event.stopPropagation();showFrameImageProviderDialog(${shotIdx}, ${frameIdx})" style="position:absolute;bottom:4px;right:4px;padding:3px 8px;background:rgba(0,0,0,0.6);color:white;border:none;border-radius:4px;font-size:11px;cursor:pointer;white-space:nowrap;" title="重新生成此帧">🔄 重新生成</button>
               </div>`
            : `<div style="width:100%;height:100%;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;color:var(--text3);cursor:pointer;" onclick="showFrameImageProviderDialog(${shotIdx}, ${frameIdx})" title="点击生成此帧">
                    <span style="font-size:28px;">📷</span>
                    <span style="font-size:12px;font-weight:500;">点击生成</span>
               </div>`;

        const description = narrativeDesc;
        const dialogue = frame.dialogue || '';
        const isLongDesc = description.length > 120;

        return `
            <div class="frame-card" id="${frameId}" style="background:var(--surface);border:1px solid var(--border);border-radius:8px;overflow:hidden;display:flex;gap:12px;min-height:120px;">
                <!-- Left: Image (16:9, fixed width ~213px for 120px height) -->
                <div style="flex:0 0 213px;min-width:213px;position:relative;background:var(--surface2);overflow:hidden;display:flex;align-items:center;justify-content:center;">
                    <div style="width:100%;height:120px;position:relative;">
                        ${imageHtml}
                        ${isGenerating ? `<div style="position:absolute;inset:0;background:rgba(0,0,0,0.5);display:flex;align-items:center;justify-content:center;color:white;font-size:12px;"><span class="spinner-inline" style="margin-right:6px;"></span>生成中...</div>` : ''}
                        <div style="position:absolute;top:6px;left:6px;background:${typeColor};color:white;padding:2px 8px;border-radius:3px;font-size:10px;font-weight:600;">${typeLabel}</div>
                    </div>
                </div>

                <!-- Middle: Description + Dialogue (flexible) -->
                <div style="flex:1;min-width:0;padding:12px 16px;display:flex;flex-direction:column;justify-content:center;position:relative;">
                    <div class="frame-desc" data-full="${description.replace(/"/g, '"')}" data-collapsed="${isLongDesc}" style="font-size:13px;color:var(--text);font-weight:500;line-height:1.6;${isLongDesc ? 'display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden;' : ''}">${description}</div>
                    ${dialogue ? `<div style="margin-top:4px;font-size:12px;color:var(--primary);font-weight:600;">🎤 台词：${dialogue}</div>` : ''}
                    ${isLongDesc ? `<button class="frame-toggle" onclick="toggleFrameDesc('${frameId}')" style="position:absolute;top:8px;right:8px;width:24px;height:24px;border-radius:50%;background:var(--bg);border:1px solid var(--border);color:var(--text3);font-size:12px;cursor:pointer;display:flex;align-items:center;justify-content:center;line-height:1;transition:all .15s;" title="展开/收起" onmouseover="this.style.background='var(--primary)';this.style.color='white';this.style.borderColor='var(--primary)'" onmouseout="this.style.background='var(--bg)';this.style.color='var(--text3)';this.style.borderColor='var(--border)'">▼</button>` : ''}
                </div>

                <!-- Right: Time info -->
                <div style="flex:0 0 160px;min-width:160px;padding:12px 16px;display:flex;flex-direction:column;justify-content:center;align-items:flex-end;gap:6px;font-size:11px;color:var(--text3);">
                    ${frame.startTime !== undefined && frame.startTime !== null ? `<span>🕐 ${frame.startTime}s</span>` : ''}
                    ${frame.duration ? `<span>⏱️ ${frame.duration}s</span>` : ''}
                    <span>#${frame.order ?? frameIdx}</span>
                </div>
            </div>
            <!-- Frame Assets -->
            ${renderFrameAssets(frame.assets || [], shotIdx)}`;
    }

    // ============ Video Player Refresh ============
    function playShotVideo(shotIdx) {
        const state = window.shotState[window.currentChapterIdx];
        if (!state || !state.shots[shotIdx]) return;
        const shot = state.shots[shotIdx];
        const area = document.getElementById('videoArea_' + shotIdx);
        if (!area) return;
        area.innerHTML = renderVideoArea(shot, shotIdx, shot.hasVideo && !!shot.videoUrl, shot.generatingVideo);
    }

    function regenerateShotVideo(shotIdx) {
        generateShotVideo(shotIdx);
    }

    // Toggle Frame Description expand/collapse (using data-collapsed attribute)
    function toggleFrameDesc(frameId) {
        const card = document.getElementById(frameId);
        if (!card) return;
        const descEl = card.querySelector('.frame-desc');
        const toggleBtn = card.querySelector('.frame-toggle');
        if (!descEl || !toggleBtn) return;

        const isCollapsed = descEl.getAttribute('data-collapsed') === 'true';
        if (isCollapsed) {
            descEl.style.display = 'block';
            descEl.style.webkitLineClamp = 'unset';
            descEl.style.overflow = 'visible';
            descEl.setAttribute('data-collapsed', 'false');
            toggleBtn.textContent = '▲';
        } else {
            descEl.style.display = '-webkit-box';
            descEl.style.webkitLineClamp = '3';
            descEl.style.webkitBoxOrient = 'vertical';
            descEl.style.overflow = 'hidden';
            descEl.setAttribute('data-collapsed', 'true');
            toggleBtn.textContent = '▼';
        }
    }

    // ============ Shot Frames Toggle ============
    function toggleShotFrames(shotIdx) {
        const contentWrapper = document.getElementById(`shot-content-${shotIdx}`);
        const btn = document.querySelector(`#shot-${shotIdx} .shot-toggle`);
        if (!contentWrapper || !btn) return;

        const isCollapsed = contentWrapper.style.display === 'none';
        if (isCollapsed) {
            contentWrapper.style.display = 'block';
            btn.textContent = '▼';
            btn.title = '收起帧';
        } else {
            contentWrapper.style.display = 'none';
            btn.textContent = '▶';
            btn.title = '展开帧';
        }
    }

    function renderContentTab() {
        const area = document.getElementById('contentArea');
        if (!area) return;

        if (window.chapters.length === 0 || window.currentChapterIdx === -1) {
            area.innerHTML = `
                <div class="no-content" style="text-align:center;padding:60px 20px;color:var(--text3);">
                    <div class="icon" style="font-size:40px;margin-bottom:12px;">📖</div>
                    <p>暂无章节内容</p>
                </div>`;
            return;
        }

        const ch = window.chapters[window.currentChapterIdx];
        area.innerHTML = `
            <div style="background:var(--surface);border:1px solid var(--border);border-radius:8px;padding:24px;">
                <h3 style="font-size:18px;font-weight:700;margin-bottom:4px;">${ch.chapterNumber} — ${ch.chapterName}</h3>
                <div class="content-body" style="margin-top:16px;">${formatMarkdown(ch.content || '')}</div>
            </div>`;
    }

    // Toggle Shot Description expand/collapse (using data-collapsed attribute)
    function toggleShotDesc(shotId) {
        const descEl = document.querySelector(`#${shotId} .shot-desc`);
        const toggleBtn = document.querySelector(`#${shotId} .shot-desc-toggle`);
        if (!descEl || !toggleBtn) return;

        const isCollapsed = descEl.getAttribute('data-collapsed') === 'true';
        if (isCollapsed) {
            descEl.style.display = 'block';
            descEl.style.webkitLineClamp = 'unset';
            descEl.style.overflow = 'visible';
            descEl.setAttribute('data-collapsed', 'false');
            toggleBtn.textContent = '▲';
        } else {
            descEl.style.display = '-webkit-box';
            descEl.style.webkitLineClamp = '3';
            descEl.style.webkitBoxOrient = 'vertical';
            descEl.style.overflow = 'hidden';
            descEl.setAttribute('data-collapsed', 'true');
            toggleBtn.textContent = '▼';
        }
    }

    // ============ Frame Asset Image Generation (port from assets-render.js) ============
    var _sagModels = [];
    var _assetGenTemplates = null;

    function loadAssetGenTemplates() {
        if (_assetGenTemplates) return Promise.resolve(_assetGenTemplates);
        return fetch('/Assets/GetGenerationTemplates')
            .then(function(r) { return r.json(); })
            .then(function(data) { _assetGenTemplates = data.success ? data.data : {}; return _assetGenTemplates; })
            .catch(function() { _assetGenTemplates = {}; return {}; });
    }

    async function loadSingleAssetGenModels(assetType) {
        var select = document.getElementById('sagModelSelect');
        select.innerHTML = '<option value="">加载中...</option>';
        select.disabled = true;
        try {
            var res = await fetch('/api/v1/providers/image-models');
            var data = await res.json();
            var imageProviders = data.success ? data.data : [];
            if (imageProviders.length === 0) {
                select.innerHTML = '<option value="">暂无可用的文生图模型，请先配置 ComfyUI</option>';
                select.disabled = true;
                return;
            }
            _sagModels = imageProviders;
            select.innerHTML = '';
            imageProviders.forEach(function(p, idx) {
                var opt = document.createElement('option');
                opt.value = p.id;
                opt.textContent = p.name + ' (' + (p.model || 'default') + ')';
                if (idx === 0) opt.selected = true;
                select.appendChild(opt);
            });
            select.disabled = false;
        } catch (err) {
            select.innerHTML = '<option value="">加载失败: ' + err.message + '</option>';
            select.disabled = true;
        }
    }

    function showSingleAssetGenModalFromFrame(assetId, assetName, assetType, assetDesc) {
        document.getElementById('sagAssetId').value = assetId;
        var typeKey = ({ '1': 'Actor', '2': 'VoiceVoice', '3': 'Scene', '4': 'Bgm', '5': 'Prop' })[assetType] || assetType;
        document.getElementById('sagAssetType').value = typeKey;
        document.getElementById('sagAssetName').value = assetName;
        document.getElementById('sagGeneratedImageUrl').value = '';

        var typeNames = { 'Actor': '角色', 'Scene': '场景', 'Prop': '道具' };
        var typeName = typeNames[typeKey] || typeKey;
        document.getElementById('singleAssetGenTitle').textContent = '单个资产 AI 生成图片: ' + assetName + ' (' + typeName + ')';

        loadAssetGenTemplates().then(function(templates) {
            var templateKeyMap = { 'Actor': '角色档案生成提示词', 'Scene': '场景档案生成提示词', 'Prop': '道具档案生成提示词' };
            var templateContent = templates[templateKeyMap[typeKey]] || '';
            var prompts = {
                'Actor': '示例：穿着红色长裙的女性角色，正面视角，白背景，动漫风格，高质量',
                'Scene': '示例：古代京城街道，青石板路，两侧店铺林立，晨光穿过屋檐，赛博朋克风格',
                'Prop': '示例：精致的青瓷碗，热气腾腾，白瓷质感，特写镜头，产品摄影风格'
            };
            var promptEl = document.getElementById('sagPrompt');
            promptEl.placeholder = prompts[typeKey] || '输入描述词...';
            document.getElementById('sagPromptHint').textContent = prompts[typeKey] || '';
            promptEl.value = assetDesc ? '名称：' + assetName + '\n' + assetDesc + '\n' : '名称：' + assetName + '\n';

            var templateDisplay = document.getElementById('sagTemplatePromptDisplay');
            if (templateContent) {
                templateDisplay.value = templateContent;
                document.getElementById('sagTemplateGroup').style.display = '';
            } else {
                document.getElementById('sagTemplateGroup').style.display = 'none';
            }
        });

        document.getElementById('sagPreviewGroup').style.display = 'none';
        document.getElementById('sagPreviewImg').style.display = 'none';
        document.getElementById('sagPreviewPlaceholder').style.display = '';
        document.getElementById('sagGenerateBtn').style.display = '';
        document.getElementById('sagSaveBtn').style.display = 'none';
        document.getElementById('sagCloseBtn').textContent = '关闭';
        document.getElementById('sagStatus').style.display = 'none';
        document.getElementById('sagStatus').innerHTML = '';

        var genBtn = document.getElementById('sagGenerateBtn');
        genBtn.disabled = false;
        genBtn.textContent = '生成';
        genBtn.style.display = '';
        document.getElementById('sagSaveBtn').style.display = 'none';
        document.getElementById('sagCloseBtn').disabled = false;
        document.getElementById('sagStatus').className = '';
        document.getElementById('sagStatus').innerHTML = '';

        loadSingleAssetGenModels(assetType);
        showModal('singleAssetGenModal');
    }

    async function doSingleAssetGenerate() {
        var assetId = document.getElementById('sagAssetId').value;
        var assetType = document.getElementById('sagAssetType').value;
        var modelId = document.getElementById('sagModelSelect').value;
        var prompt = document.getElementById('sagPrompt').value.trim();
        var templateContent = document.getElementById('sagTemplatePromptDisplay').value.trim();

        if (!modelId) { alert('请选择生成模型'); return; }
        if (!prompt) { alert('请输入提示词'); return; }

        var genBtn = document.getElementById('sagGenerateBtn');
        var saveBtn = document.getElementById('sagSaveBtn');
        var closeBtn = document.getElementById('sagCloseBtn');
        var statusEl = document.getElementById('sagStatus');
        var previewGroup = document.getElementById('sagPreviewGroup');
        var previewImg = document.getElementById('sagPreviewImg');
        var previewPlaceholder = document.getElementById('sagPreviewPlaceholder');

        genBtn.disabled = true;
        genBtn.textContent = '生成中...';
        closeBtn.disabled = true;
        statusEl.style.display = '';
        statusEl.className = 'status-info';
        statusEl.innerHTML = '🔄 正在提交生成任务...';

        try {
            var model = _sagModels.find(function(m) { return m.id == modelId; });
            if (!model) throw new Error('未找到选中的模型');

            var fullPrompt = templateContent + '\n\n' + prompt;
            var isAudio = assetType === 'Bgm' || assetType === 'VoiceVoice';
            var endpoint = isAudio ? (assetType === 'Bgm' ? '/api/comfyui/stable-bgm/generate' : '/api/comfyui/ace-music/compose') : '/Assets/GenerateCharacterImage';
            var workflowType = isAudio ? (assetType === 'Bgm' ? 'stable-bgm-generate' : 'ace-music-compose') : 'zimage-character-profile';
            var payload = isAudio ? { prompt: fullPrompt } : { systemPrompt: templateContent, characterPrompt: prompt, negativePrompt: '', width: 1024, height: 768 };

            var res = await fetch(endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });
            var data = await res.json();

            var promptId;
            if (isAudio) {
                promptId = data && data.promptId;
                if (!promptId) throw new Error(data.message || '提交任务失败');
            } else {
                if (!data.success) throw new Error(data.message || '提交任务失败');
                promptId = data.promptId;
            }

            statusEl.innerHTML = '⏳ 任务已提交，正在生成中... (promptId: ' + promptId + ')';

            var resultUrl = await pollSingleAssetGenResult(promptId, workflowType, isAudio);
            if (!resultUrl) throw new Error('生成超时或失败');

            if (isAudio) {
                statusEl.className = 'status-success';
                statusEl.innerHTML = '✅ 音频生成成功！<br><audio controls src="' + resultUrl + '" style="width:100%;margin-top:8px;"></audio>';
                document.getElementById('sagGeneratedImageUrl').value = resultUrl;
            } else {
                previewGroup.style.display = '';
                previewImg.src = resultUrl;
                previewImg.style.display = '';
                previewPlaceholder.style.display = 'none';
                document.getElementById('sagGeneratedImageUrl').value = resultUrl;
                statusEl.className = 'status-success';
                statusEl.innerHTML = '✅ 图片生成成功！';
            }

            genBtn.style.display = 'none';
            saveBtn.style.display = '';
            closeBtn.textContent = '取消';
            closeBtn.disabled = false;
        } catch (err) {
            statusEl.className = 'status-error';
            statusEl.innerHTML = '❌ 生成失败: ' + err.message;
            genBtn.disabled = false;
            genBtn.textContent = '重新生成';
            closeBtn.disabled = false;
        }
    }

    function pollSingleAssetGenResult(promptId, workflowType, isAudio) {
        return new Promise(function(resolve, reject) {
            var pollCount = 0;
            var maxPolls = 120;
            var pollInterval = 5000;
            var timer = setInterval(async function() {
                pollCount++;
                if (pollCount >= maxPolls) { clearInterval(timer); reject(new Error('生成超时')); return; }
                try {
                    var resultType = isAudio ? 'audio' : 'image';
                    var res = await fetch('/api/v1/comfyui/result/' + promptId + '/' + resultType + '?workflowType=' + encodeURIComponent(workflowType));
                    var data = await res.json();
                    if (data.success && data.data) {
                        var urls = [];
                        var d = data.data;
                        if (isAudio) { if (d.audioUrls && Array.isArray(d.audioUrls)) urls = d.audioUrls; }
                        else { if (d.imageUrls && Array.isArray(d.imageUrls)) urls = d.imageUrls; }
                        if (urls.length > 0) { clearInterval(timer); resolve(urls[0]); return; }
                    } else if (data.pending === false) { clearInterval(timer); reject(new Error(data.message || '任务执行失败')); return; }
                } catch (e) {}
            }, pollInterval);
        });
    }

    async function saveSingleAssetImage() {
        var assetId = document.getElementById('sagAssetId').value;
        var assetType = document.getElementById('sagAssetType').value;
        var resourceUrl = document.getElementById('sagGeneratedImageUrl').value;
        var isAudio = assetType === 'Bgm' || assetType === 'VoiceVoice';
        var saveBtn = document.getElementById('sagSaveBtn');
        var closeBtn = document.getElementById('sagCloseBtn');
        var statusEl = document.getElementById('sagStatus');

        if (!resourceUrl) { alert('没有可保存的资源'); return; }

        saveBtn.disabled = true;
        saveBtn.textContent = '保存中...';
        closeBtn.disabled = true;
        statusEl.className = 'status-info';
        statusEl.innerHTML = '🔄 正在保存到资产...';

        try {
            var endpoint = isAudio ? '/Assets/ReplaceAudio' : '/Assets/ReplaceResource';
            var res = await fetch(endpoint, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ assetId: assetId, fileUrl: resourceUrl })
            });
            var text = await res.text();
            var result = text ? JSON.parse(text) : {};
            if (result.success) {
                statusEl.className = 'status-success';
                statusEl.innerHTML = '✅ ' + (isAudio ? '音频' : '图片') + '已保存到资产！';
                showToast('资源已更新', 'success');
                setTimeout(function() {
                    hideModal('singleAssetGenModal');
                    var idx = window.currentChapterIdx;
                    window.loadShotsForChapter(idx, true).then(function() { renderShotsTab(); });
                }, 1000);
            } else {
                throw new Error(result.message || '保存失败');
            }
        } catch (err) {
            statusEl.className = 'status-error';
            statusEl.innerHTML = '❌ 保存失败: ' + err.message;
            saveBtn.disabled = false;
            saveBtn.textContent = '保存到资产';
            closeBtn.disabled = false;
        }
    }

    // Expose
    window.renderShotsTab = renderShotsTab;
    window.renderFrameCard = renderFrameCard;
    window.renderFrameAssets = renderFrameAssets;
    window.playShotVideo = playShotVideo;
    window.regenerateShotVideo = regenerateShotVideo;
    window.renderContentTab = renderContentTab;
    window.toggleFrameDesc = toggleFrameDesc;
    window.toggleShotFrames = toggleShotFrames;
    window.toggleShotDesc = toggleShotDesc;
    window.removeFrameAssetFromShot = removeFrameAssetFromShot;
    window.showSingleAssetGenModalFromFrame = showSingleAssetGenModalFromFrame;
    window.doSingleAssetGenerate = doSingleAssetGenerate;
    window.saveSingleAssetImage = saveSingleAssetImage;

})();